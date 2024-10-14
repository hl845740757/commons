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

import javax.annotation.Nullable;

/**
 * 该接口用于运行时补全类型的泛型参数
 *
 * @author wjybxx
 * date - 2024/9/27
 */
public interface GenericHelper {

    /**
     * 尝试继承声明类型的泛型参数（可以写入更完整的泛型信息）
     * 1.用户的接口不会收到数组类型。
     * 2.用户的实现主要处理runtimeType和declaredType具有不同泛型定义的情况，其它情况可由默认逻辑处理。
     * 3.底层会缓存查询结果，用户的实现通常不需要再进行缓存。
     * 4.用户的实现通常仅仅是测试两者的类型，然后转移泛型参数。
     *
     * @param runtimeType  运行时类型
     * @param declaredType 声明类型，可能和运行时类型一致，也可能毫无关系（投影）
     * @return 如果返回null，表示无法处理；返回{@link TypeInfo#OBJECT}表示中断处理；其它表示成功处理。
     */
    @Nullable
    TypeInfo inheritTypeArgs(Class<?> runtimeType, TypeInfo declaredType);
}
