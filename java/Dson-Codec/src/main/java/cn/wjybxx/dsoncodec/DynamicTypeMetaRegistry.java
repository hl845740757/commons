/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dsoncodec;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 为更好的支持泛型，我们根据原型类型动态创建TypeMeta
 *
 * @author wjybxx
 * date - 2024/5/16
 */
public class DynamicTypeMetaRegistry implements TypeMetaRegistry {

    private final TypeMetaRegistry basicRegistry;

    private final ClassNamePool classNamePool = new ClassNamePool();
    private final ConcurrentHashMap<TypeInfo<?>, TypeMeta> type2MetaDic = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<String, TypeMeta> name2MetaDic = new ConcurrentHashMap<>();

    public DynamicTypeMetaRegistry(TypeMetaRegistry basicRegistry) {
        this.basicRegistry = Objects.requireNonNull(basicRegistry);
    }

    // region ofType

    @Nullable
    @Override
    public TypeMeta ofClass(Class<?> clazz) {
        TypeMeta typeMeta = basicRegistry.ofClass(clazz);
        if (typeMeta != null) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型，或在基础注册表中不存在
        // 对于数组，我们可以生成原始类型数组的元数据，其它情况下则证明不存在对应元数据
        if (clazz.isArray()) {
            return ofType(TypeInfo.of(clazz));
        }
        return null;
    }

    @Nullable
    @Override
    public TypeMeta ofType(TypeInfo<?> type) {
        return ofType(type, false);
    }

    private TypeMeta ofType(TypeInfo<?> type, boolean basicType) {
        TypeMeta typeMeta = basicRegistry.ofType(type);
        if (typeMeta != null || basicType) {
            return typeMeta;
        }
        typeMeta = type2MetaDic.get(type);
        if (typeMeta != null) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型，或在基础注册表中不存在
        if (!type.isGenericType() && !type.isArray()) {
            return null;
        }

        ObjectStyle style;
        if (type.isArray()) {
            style = ObjectStyle.INDENT;
        } else {
            TypeMeta rawTypeMeta = ofType(type);
            if (rawTypeMeta == null) { // 通常这里不应该为null
                throw new AssertionError("type: " + type);
            }
            style = rawTypeMeta.style;
        }
        ClassName className = classNameOfType(type);
        String mainClsName = className.toString();

        // 动态生成TypeMeta并缓存下来
        typeMeta = TypeMeta.of(type, style, mainClsName);
        type2MetaDic.put(type, typeMeta);
        name2MetaDic.put(mainClsName, typeMeta);
        return typeMeta;
    }

    // endregion

    // region ofName

    @Override
    public TypeMeta ofName(String clsName) {
        return ofName(clsName, false);
    }

    private TypeMeta ofName(String clsName, boolean basicType) {
        TypeMeta typeMeta = basicRegistry.ofName(clsName);
        if (typeMeta != null || basicType) {
            return typeMeta;
        }
        typeMeta = name2MetaDic.get(clsName);
        if (typeMeta != null) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型 -- 别名可能导致泛型断言失败
        ClassName className = classNamePool.parse(clsName);
//        assert className.isArray() || className.isGeneric()
        TypeInfo<?> type = typeOfClassName(className);

        // 通过Type初始化TypeMeta，我们尽量合并TypeMeta -- clsName包含空白时不缓存
        typeMeta = ofType(type);
        if (typeMeta == null) {
            throw new AssertionError(type.toString());
        }
        if (typeMeta.clsNames.contains(clsName) || ObjectUtils.containsWhitespace(clsName)) {
            return typeMeta;
        }
        // 覆盖数据
        {
            List<String> clsNames = new ArrayList<>(typeMeta.clsNames.size() + 1);
            clsNames.addAll(typeMeta.clsNames);
            clsNames.add(clsName);

            typeMeta = TypeMeta.of(type, typeMeta.style, clsNames);
            type2MetaDic.put(type, typeMeta);
            for (String name : clsNames) {
                name2MetaDic.put(name, typeMeta);
            }
        }
        return typeMeta;
    }

    // endregion

    // region internal

    /**
     * 根据Type查找对应的ClassName。
     * 1.由于类型存在别名，一个Type的ClassName可能有很多个，且泛型参数还会导致组合，导致更多的类型名，但动态生成时我们只生成确定的一种。
     * 2.解析的开销较大，需要缓存最终结果。
     * 3.Java禁止递归的泛型
     */
    private ClassName classNameOfType(TypeInfo<?> type) {
        if (type.isArray()) {
            TypeInfo<?> rootElementType = TypeInfo.of(DsonConverterUtils.getRootComponentType(type.rawType), type.typeArgs);
            int arrayRank = DsonConverterUtils.getArrayRank(type.rawType);
            String clsName = classNameOfType(rootElementType) + DsonConverterUtils.arrayRankSymbol(arrayRank);
            return new ClassName(clsName);
        }
        if (type.isGenericType()) {
            // 泛型原型类必须存在于用户的注册表中
            TypeInfo<?> genericTypeDefinition = type.getGenericTypeDefinition();
            TypeMeta typeMeta = ofType(genericTypeDefinition, true);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            List<Class<?>> genericArguments = type.typeArgs;
            List<ClassName> typeArgClassNames = new ArrayList<>(genericArguments.size());
            for (Class<?> genericArgument : genericArguments) {
                TypeInfo<?> genericArgType = TypeInfo.of(genericArgument);
                typeArgClassNames.add(classNameOfType(genericArgType));
            }
            return new ClassName(typeMeta.mainClsName(), typeArgClassNames);
        }
        // 非泛型非数组，必须存在于用户的注册表中
        {
            TypeMeta typeMeta = ofType(type, true);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            return new ClassName(typeMeta.mainClsName());
        }
    }

    /**
     * 根据ClassName查找对应的Type。
     * 1.ClassName到Type之间是多对一关系。
     * 2.解析的开销较大，需要缓存最终结果。
     * 3.Java禁止递归的泛型
     */
    private TypeInfo<?> typeOfClassName(ClassName className) {
        // 先解析泛型类，再构建数组
        int arrayRank = className.getArrayRank();
        TypeInfo<?> elementType;
        if (arrayRank > 0) {
            // 获取数组根元素的类型
            elementType = typeOfClassName(new ClassName(className.getRootElement(), className.typeArgs));
        } else {
            // 解析泛型原型 —— 这个clsName必须存在于用户的注册表中
            TypeMeta typeMeta = ofName(className.clsName, true);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, className: " + className);
            }
            elementType = typeMeta.typeInfo;
            // 解析泛型参数
            int typeArgsCount = className.typeArgs.size();
            if (typeArgsCount > 0) {
                Class<?>[] typeParameters = new Class<?>[typeArgsCount];
                for (int index = 0; index < typeArgsCount; index++) {
                    ClassName genericArgClassName = className.typeArgs.get(index);
                    typeParameters[index] = typeOfClassName(genericArgClassName).rawType;
                }
                elementType = TypeInfo.of(elementType.rawType, typeParameters);
            }
        }
        // 构建多维数组
        while (arrayRank-- > 0) {
            elementType = elementType.makeArrayType();
        }
        return elementType;
    }

    // endregion

    @Override
    public List<TypeMeta> export() {
        List<TypeMeta> result = new ArrayList<>(basicRegistry.export());
        result.addAll(type2MetaDic.values());
        return result;
    }

}