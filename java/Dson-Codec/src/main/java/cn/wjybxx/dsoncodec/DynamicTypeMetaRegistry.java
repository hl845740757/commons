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

import cn.wjybxx.base.ArrayUtils;
import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 为更好的支持泛型，我们根据原型类型动态创建TypeMeta
 * PS：要做得更好的话，还可以缓存TypeInfo实例，进行常量化。
 *
 * @author wjybxx
 * date - 2024/5/16
 */
public final class DynamicTypeMetaRegistry implements TypeMetaRegistry {

    private final TypeMetaConfig basicRegistry;
    private final ConcurrentHashMap<String, ClassName> typeNamePool = new ConcurrentHashMap<>(1024);
    private final ConcurrentHashMap<TypeInfo, TypeMeta> type2MetaDic = new ConcurrentHashMap<>(1024);
    private final ConcurrentHashMap<String, TypeMeta> name2MetaDic = new ConcurrentHashMap<>(1024);

    public DynamicTypeMetaRegistry(TypeMetaConfig config) {
        this.basicRegistry = config.toImmutable();
    }

    // region ofType

    @Nullable
    @Override
    public TypeMeta ofType(TypeInfo type) {
        TypeMeta typeMeta = basicRegistry.ofType(type);
        if (typeMeta != null) {
            return typeMeta;
        }
        typeMeta = type2MetaDic.get(type);
        if (typeMeta != null) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型，或在基础注册表中不存在
        if (!type.isGenericType() && !type.isArrayType()) {
            return null;
        }

        ObjectStyle style;
        if (type.isArrayType()) {
            style = ObjectStyle.INDENT;
        } else {
            TypeMeta rawTypeMeta = basicRegistry.ofType(TypeInfo.of(type.rawType));
            if (rawTypeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            style = rawTypeMeta.style; // 保留泛型类的Style
        }
        ClassName className = classNameOfType(type); // 放前方可检测泛型
        String mainClsName = className.toString();

        // 动态生成TypeMeta并缓存下来
        typeMeta = TypeMeta.of(type, style, mainClsName);
        type2MetaDic.putIfAbsent(type, typeMeta);
        name2MetaDic.putIfAbsent(mainClsName, typeMeta);
        return typeMeta;
    }

    // endregion

    // region ofName

    @Override
    public TypeMeta ofName(String clsName) {
        TypeMeta typeMeta = basicRegistry.ofName(clsName);
        if (typeMeta != null) {
            return typeMeta;
        }
        typeMeta = name2MetaDic.get(clsName);
        if (typeMeta != null) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型 -- 别名可能导致断言失败
        ClassName className = parseName(clsName);
//        assert className.isArray() || className.isGeneric()
        TypeInfo type = typeOfClassName(className);

        // 通过Type初始化TypeMeta，我们尽量合并TypeMeta -- clsName包含空白时不缓存
        typeMeta = ofType(type);
        if (typeMeta == null) {
            throw new DsonCodecException("typeMeta absent, type: " + type);
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

    private ClassName parseName(String clsName) {
        Objects.requireNonNull(clsName);
        ClassName className = typeNamePool.get(clsName);
        if (className != null) {
            return className;
        }
        // 程序生成的clsName通常是紧凑的，不包含空白字符(缩进)的，因此可以安全缓存；
        // 如果clsName包含空白字符，通常是用户手写的，缓存有一定的风险性 —— 可能产生恶意缓存
        if (ObjectUtils.containsWhitespace(clsName)) {
            return ClassName.parse(clsName);
        }
        className = ClassName.parse(clsName);
        typeNamePool.put(clsName, className);
        return className;
    }

    /**
     * 根据Type查找对应的ClassName。
     * 1.由于类型存在别名，一个Type的ClassName可能有很多个，且泛型参数还会导致组合，导致更多的类型名，但动态生成时我们只生成确定的一种。
     * 2.解析的开销较大，需要缓存最终结果。
     * 3.Java禁止递归的泛型
     */
    private ClassName classNameOfType(TypeInfo type) {
        if (type.isArrayType()) {
            TypeInfo rootElementType = TypeInfo.of(ArrayUtils.getRootComponentType(type.rawType), type.genericArgs);
            int arrayRank = ArrayUtils.getArrayRank(type.rawType);
            String clsName = classNameOfType(rootElementType) + ArrayUtils.arrayRankSymbol(arrayRank);
            return new ClassName(clsName);
        }
        if (type.isGenericType()) {
            // 泛型原型类必须存在于用户的注册表中
            TypeMeta typeMeta = basicRegistry.ofType(TypeInfo.of(type.rawType));
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            List<TypeInfo> genericArguments = type.genericArgs;
            List<ClassName> typeArgClassNames = new ArrayList<>(genericArguments.size());
            for (TypeInfo genericArgument : genericArguments) {
                typeArgClassNames.add(classNameOfType(genericArgument));
            }
            return new ClassName(typeMeta.mainClsName(), typeArgClassNames);
        }
        // 非泛型非数组，必须存在于用户的注册表中
        {
            TypeMeta typeMeta = basicRegistry.ofType(type);
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
    private TypeInfo typeOfClassName(ClassName className) {
        // 先解析泛型类，再构建数组
        int arrayRank = className.getArrayRank();
        TypeInfo elementType;
        if (arrayRank > 0) {
            // 获取数组根元素的类型
            elementType = typeOfClassName(new ClassName(className.getRootElement(), className.typeArgs));
        } else {
            // 解析泛型原型 —— 泛型原型类必须存在于用户的注册表中
            TypeMeta typeMeta = basicRegistry.ofName(className.clsName);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, className: " + className);
            }
            elementType = typeMeta.typeInfo;
            // 解析泛型参数
            int typeArgsCount = className.typeArgs.size();
            if (typeArgsCount > 0) {
                TypeInfo[] typeParameters = new TypeInfo[typeArgsCount];
                for (int index = 0; index < typeArgsCount; index++) {
                    typeParameters[index] = typeOfClassName(className.typeArgs.get(index));
                }
                elementType = TypeInfo.of(elementType.rawType, typeParameters);
            }
        }
        // 构建多维数组
        if (arrayRank > 0) {
            elementType = elementType.makeArrayType(arrayRank);
        }
        return elementType;
    }

    // endregion
}