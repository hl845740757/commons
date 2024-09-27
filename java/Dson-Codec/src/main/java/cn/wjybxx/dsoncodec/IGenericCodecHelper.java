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

/**
 * 泛型编解码辅助类
 *
 * @author wjybxx
 * date - 2024/9/27
 */
@FunctionalInterface
public interface IGenericCodecHelper {

    /**
     * 测试运行时类型是否可继承声明类型的泛型参数
     * 如果可继承泛型参数，则会将声明类型的泛型参数传递给运行时类型，以查询对象的编解码器（可以写入更完整的泛型信息）。
     * <p>
     * 注意：
     * 1.该方法执行频率较高，应当考虑缓存。
     * 2.数组类不是泛型类，不直接包含泛型参数声明，要小心处理。
     *
     * @param runtimeType  运行时类型
     * @param declaredType 声明类型，可能和运行时类型一致，也可能毫无关系(投影)
     * @return 如果可以继承泛型参数则返回true
     */
    boolean canInheritTypeArgs(Class<?> runtimeType, Class<?> declaredType);

    /** 用于实现缓存 */
    class ClassPair {

        public final Class<?> first;
        public final Class<?> second;

        public ClassPair(Class<?> first, Class<?> second) {
            this.first = first;
            this.second = second;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            ClassPair classPair = (ClassPair) o;

            if (!first.equals(classPair.first)) return false;
            return second.equals(classPair.second);
        }

        @Override
        public int hashCode() {
            int result = first.hashCode();
            result = 31 * result + second.hashCode();
            return result;
        }
    }
}
