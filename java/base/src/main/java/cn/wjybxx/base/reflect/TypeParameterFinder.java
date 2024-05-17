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

package cn.wjybxx.base.reflect;

import cn.wjybxx.base.annotation.Beta;

import java.lang.reflect.*;
import java.util.Objects;

/**
 * 泛型参数具体类型查找器。
 * 哪些泛型参数可以被找到？
 * 需要满足下面两个条件：
 * 1.指定泛型参数是在该对象的类所在的类层次的上层定义的。
 * 2.在定义该泛型参数的类/接口的下层被具体化了。
 * <p>
 * 举个栗子：
 * 在{@code io.netty.handler.codec.MessageToMessageDecoder}中定义了泛型参数I，
 * 而其子类 {@code io.netty.handler.codec.http.HttpContentDecoder}将其指定为{@code HttpObject},
 * 那么可以通过HttpContentDecoder的实例查找到MessageToMessageDecoder上的泛型参数I为HttpObject类型。
 * <p>
 * 反面栗子：
 * 在{@link java.util.List}中定义了泛型参数E，
 * 在{@link java.util.ArrayList}中声明了新的泛型参数E，并将List中的E指定为新声明的E(这是两个泛型参数)。
 * 那么无法通过ArrayList的实例查找到List上泛型参数E的具体类型的。
 * <p>
 * 该类对Netty的泛型参数查找进行了增强，Netty自带的查找只支持超类中查找，这里进行适配增强，以支持查找接口中声明的泛型参数。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Beta
public class TypeParameterFinder {
    
    /**
     * 获取包含泛型信息的超类(或接口)
     *
     * @param thisClass             当前类
     * @param superClazzOrInterface 目标超类后接口
     */
    public static <T> Type getGenericSuperType(final Class<T> thisClass, final Class<? super T> superClazzOrInterface) {
        assert superClazzOrInterface.isAssignableFrom(thisClass);
        if (superClazzOrInterface.isInterface()) {
            Class<? super T> directChildClass = findInterfaceDirectChildClass(thisClass, superClazzOrInterface);
            return getGenericInterface(directChildClass, superClazzOrInterface);
        }
        Class<?> currentClass = thisClass;
        while (currentClass.getSuperclass() != superClazzOrInterface) {
            currentClass = currentClass.getSuperclass();
        }
        return currentClass.getGenericSuperclass();
    }

    /**
     * 从instance所属的类开始，查找在superClazzOrInterfaced定义的泛型参数typeParamName的具体类型
     * (该方法更安全)
     *
     * @param instance              实例对象
     * @param superClazzOrInterface 声明泛型参数typeParamName的类,class或interface
     * @param typeParamName         泛型参数名字
     * @param <T>                   约束必须有继承关系或实现关系
     * @return 如果定义的泛型存在，则返回对应的泛型clazz
     */
    public static <T> Class<?> findTypeParameter(T instance, Class<? super T> superClazzOrInterface, String typeParamName) {
        Objects.requireNonNull(instance, "instance");
        @SuppressWarnings("unchecked") final Class<? extends T> thisClass = (Class<? extends T>) instance.getClass();
        return findTypeParameterUnsafe(thisClass, superClazzOrInterface, typeParamName);
    }

    /**
     * 从指定类开始查找在superClazzOrInterfaced定义的泛型参数typeParamName的具体类型。
     * 请优先使用{@link #findTypeParameter(Object, Class, String)}。
     *
     * @param thisClass             查找起始类，注意最好是{@code this.getClass()}获取到的class对象。
     * @param superClazzOrInterface 声明泛型参数typeParamName的类,class或interface
     * @param typeParamName         泛型参数名字
     * @param <T>                   约束必须有继承关系或实现关系
     * @return 如果定义的泛型存在，则返回对应的泛型clazz
     */
    public static <T> Class<?> findTypeParameterUnsafe(Class<T> thisClass, Class<? super T> superClazzOrInterface, String typeParamName) {
        Objects.requireNonNull(thisClass, "thisClass");
        Objects.requireNonNull(superClazzOrInterface, "superClazzOrInterface");
        Objects.requireNonNull(typeParamName, "typeParamName");

        if (thisClass == superClazzOrInterface) {
            // 仅仅支持查找父类/父接口定义的泛型且被子类声明为具体类型的泛型参数
            throw new IllegalArgumentException("typeParam " + typeParamName + " is declared in self class: " + thisClass.getSimpleName()
                    + ", only support find superClassOrInterface typeParam.");
        }
        ensureTypeParameterExist(superClazzOrInterface, typeParamName);

        if (superClazzOrInterface.isInterface()) {
            // 自己实现的在接口中查找泛型参数的具体类型
            return findInterfaceTypeParameter(thisClass, superClazzOrInterface, typeParamName);
        } else {
            // netty实现了在超类中进行查找
            return find0(thisClass, superClazzOrInterface, typeParamName);
        }
    }

    /**
     * 确保泛型参数在该类型中进行了定义
     *
     * @param parametrizedSuperInterface 超类/接口对应的Class对象
     * @param typeVarName                泛型参数名
     */
    private static void ensureTypeParameterExist(Class<?> parametrizedSuperInterface, String typeVarName) {
        if (indexTypeParam(parametrizedSuperInterface, typeVarName) < 0) {
            throw new IllegalArgumentException("typeVarName " + typeVarName +
                    " is not declared in superClazz/interface " + parametrizedSuperInterface.getSimpleName());
        }
    }

    /**
     * 从当前类开始，寻找最近通向泛型接口的通路。
     *
     * @param thisClass             起始查找类
     * @param parametrizedInterface 定义泛型参数的超类或接口
     * @param typeParamName         泛型参数的名字
     * @return 泛型参数的距离类型
     */
    private static <T> Class<?> findInterfaceTypeParameter(final Class<T> thisClass, Class<? super T> parametrizedInterface, String typeParamName) {
        final Class<? super T> directChildClass = findInterfaceDirectChildClass(thisClass, parametrizedInterface);
        return parseTypeParameter(thisClass, directChildClass, parametrizedInterface, typeParamName);
    }

    /**
     * 查找接口的任意直接子节点
     * <p>
     * 为什么任意子节点都是正确的呢？
     * 当子类和父类实现相同的接口时，或实现多个接口时，对同一个泛型变量进行约束时，子类的泛型参数约束必定是所有约束的子集。
     * 当指定具体类型以后，任意一条通路结果都是正确的。
     *
     * @param currentClazzOrInterface 递归到的当前类或接口
     * @param parametrizedInterface   起始class继承的接口或实现的接口
     * @return actualType
     */
    private static <T> Class<? super T> findInterfaceDirectChildClass(Class<? super T> currentClazzOrInterface, Class<? super T> parametrizedInterface) {
        if (!parametrizedInterface.isAssignableFrom(currentClazzOrInterface)) {
            throw new IllegalArgumentException("currentClazzOrInterface=" + currentClazzOrInterface.getSimpleName()
                    + " ,parametrizedInterface=" + parametrizedInterface.getSimpleName());
        }

        // 查询直接实现/继承的接口
        Class<?>[] implementationInterfaces = currentClazzOrInterface.getInterfaces();
        for (Class<?> clazz : implementationInterfaces) {
            if (clazz == parametrizedInterface) {
                // 找到了直接实现类/直接子接口
                return currentClazzOrInterface;
            }
        }

        // 因为指定了泛型的具体类型，那么任意一个路径达到目标类都能获取到相同结果
        // 如果超类 是 目标类的子类或实现类，就在超类体系中查找，更快更简单(因为超类只有一个)
        Class<? super T> superclass = currentClazzOrInterface.getSuperclass();
        if (null != superclass && parametrizedInterface.isAssignableFrom(superclass)) {
            return findInterfaceDirectChildClass(superclass, parametrizedInterface);
        }

        // 这里，currentClazzOrInterface继承或实现的接口中必定存在目标接口的子接口
        assert parametrizedInterface.isInterface() : "currentClazzOrInterface " + currentClazzOrInterface.getSimpleName() + " i";
        for (Class<?> oneSuperInterface : implementationInterfaces) {
            if (parametrizedInterface.isAssignableFrom(oneSuperInterface)) {
                // 任意一个通路上去
                @SuppressWarnings({"unchecked"})
                Class<? super T> superInterface = (Class<? super T>) oneSuperInterface;
                return findInterfaceDirectChildClass(superInterface, parametrizedInterface);
            }
        }

        // 这里走不到
        throw new AssertionError();
    }

    /**
     * 通过找到的类/接口的声明信息和类对象解析出具体的泛型类型,使用了netty的解析代码，保持尽量少的改动
     * {@code TypeParameterMatcher#find0(Object, Class, String)}
     *
     * @param thisClass                  起始查找类，可能需要递归重新查找
     * @param directChildClass           直接孩子(子接口或实现类)
     * @param parametrizedSuperInterface 显示声明指定泛型参数typeParamName的接口
     * @param typeParamName              泛型名字
     * @return actualType
     */
    private static <T> Class<?> parseTypeParameter(final Class<? extends T> thisClass, final Class<? super T> directChildClass,
                                                   final Class<? super T> parametrizedSuperInterface, final String typeParamName) {
        // 获取的是声明的泛型变量 类名/接口名之后的<>
        int typeParamIndex = indexTypeParam(parametrizedSuperInterface, typeParamName);
        assert typeParamIndex >= 0;

        // 这里的实现是在接口中查找，而netty的实现是在超类中查找
        Type genericSuperInterface = getGenericInterface(directChildClass, parametrizedSuperInterface);
        if (!(genericSuperInterface instanceof ParameterizedType)) {
            // 1. 直接子类忽略了该接口中所有泛型参数，会导致获取到不是 ParameterizedType，而是一个普通的class对象
            return Object.class;
        }

        // 2. 直接子类对父接口中至少一个泛型参数进行了保留或指定了具体类型，会导致获取到的是ParameterizedType
        Type[] actualTypeParams = ((ParameterizedType) genericSuperInterface).getActualTypeArguments();
        Type actualTypeParam = actualTypeParams[typeParamIndex];
        if (actualTypeParam instanceof ParameterizedType parameterizedType) {
            actualTypeParam = parameterizedType.getRawType();
        }
        if (actualTypeParam instanceof Class<?> clazz) {
            return clazz;
        }
        if (actualTypeParam instanceof GenericArrayType arrayType) {
            // 数组的元素只能是普通类型和参数化类型
            Type componentType = arrayType.getGenericComponentType();
            if (componentType instanceof ParameterizedType parameterizedType) {
                componentType = parameterizedType.getRawType();
            }
            if (componentType instanceof Class<?>) {
                return Array.newInstance((Class<?>) componentType, 0).getClass();
            }
        }

        if (actualTypeParam instanceof TypeVariable<?> typeVar) {
            // 3.真实类型是另一个泛型参数，即子接口仍然用泛型参数表示父接口中的泛型参数，可能原封不动的保留了，也可能添加了边界，也可能换了个名
            // Resolved type parameter points to another type parameter.
            if (!(typeVar.getGenericDeclaration() instanceof Class<?> genericDeclarationClass)) {
                // 4.新泛型参数(换名后的参数名)的类型如果不是class/interface，则返回Object。
                return Object.class;
            }

            if (parametrizedSuperInterface.isAssignableFrom(thisClass)) {
                // 5.实例对象的某个超类或接口仍然用泛型参数表示目标泛型参数，则需要重新查找被重新定义的泛型参数
                @SuppressWarnings("unchecked")
                Class<? super T> newSuperClazzOrInterface = (Class<? super T>) genericDeclarationClass;
                return findTypeParameterUnsafe(thisClass, newSuperClazzOrInterface, typeVar.getName());
            } else {
                // 5.泛型参数来自另一个继承体系,停止查找
                return Object.class;
            }
        }

        return fail(thisClass, typeParamName);
    }

    private static <T> Type getGenericInterface(Class<? super T> directChildClass, Class<? super T> superInterface) {
        Class<?>[] extendsOrImpInterfaces = directChildClass.getInterfaces();
        for (int index = 0; index < extendsOrImpInterfaces.length; index++) {
            if (extendsOrImpInterfaces[index] == superInterface) {
                return directChildClass.getGenericInterfaces()[index];
            }
        }
        throw new AssertionError("genericSuperInterface");
    }

    private static int indexTypeParam(Class<?> clazz, String typeVarName) {
        final TypeVariable<?>[] typeParams = clazz.getTypeParameters();
        for (int idx = 0; idx < typeParams.length; idx++) {
            if (typeVarName.equals(typeParams[idx].getName())) {
                return idx;
            }
        }
        return -1;
    }

    private static Class<?> fail(Class<?> type, String typeParamName) {
        throw new IllegalStateException(
                "cannot determine the type of the type parameter '" + typeParamName + "': " + type);
    }

    /**
     * 拷贝自netty，需要进行一定修改
     */
    private static Class<?> find0(final Class<?> thisClass, Class<?> parametrizedSuperclass, String typeParamName) {
        Class<?> currentClass = thisClass;
        for (; ; ) {
            if (currentClass.getSuperclass() == parametrizedSuperclass) {
                int typeParamIndex = indexTypeParam(parametrizedSuperclass, typeParamName);
                if (typeParamIndex < 0) {
                    throw new IllegalStateException(
                            "unknown type parameter '" + typeParamName + "': " + parametrizedSuperclass);
                }

                Type genericSuperType = currentClass.getGenericSuperclass();
                if (!(genericSuperType instanceof ParameterizedType)) {
                    return Object.class;
                }

                Type[] actualTypeParams = ((ParameterizedType) genericSuperType).getActualTypeArguments();
                Type actualTypeParam = actualTypeParams[typeParamIndex];
                if (actualTypeParam instanceof ParameterizedType parameterizedType) {
                    actualTypeParam = parameterizedType.getRawType();
                }
                if (actualTypeParam instanceof Class<?> clazz) {
                    return clazz;
                }
                if (actualTypeParam instanceof GenericArrayType arrayType) {
                    Type componentType = arrayType.getGenericComponentType();
                    if (componentType instanceof ParameterizedType) {
                        componentType = ((ParameterizedType) componentType).getRawType();
                    }
                    if (componentType instanceof Class) {
                        return Array.newInstance((Class<?>) componentType, 0).getClass();
                    }
                }
                if (actualTypeParam instanceof TypeVariable<?> typeVar) {
                    // Resolved type parameter points to another type parameter.
                    currentClass = thisClass;
                    if (!(typeVar.getGenericDeclaration() instanceof Class)) {
                        return Object.class;
                    }

                    parametrizedSuperclass = (Class<?>) typeVar.getGenericDeclaration();
                    typeParamName = typeVar.getName();
                    if (parametrizedSuperclass.isAssignableFrom(thisClass)) {
                        continue;
                    } else {
                        return Object.class;
                    }
                }

                return fail(thisClass, typeParamName);
            }
            currentClass = currentClass.getSuperclass();
            if (currentClass == null) {
                return fail(thisClass, typeParamName);
            }
        }
    }

}
