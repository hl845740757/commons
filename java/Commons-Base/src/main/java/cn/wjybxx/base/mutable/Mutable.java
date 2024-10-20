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

package cn.wjybxx.base.mutable;

/**
 * 主要为基础值类型提供可变性
 *
 * @author wjybxx
 * date - 2024/1/4
 */
public interface Mutable<T> {

    /** 获取当前值 */
    T getValue();

    /**
     * @throws NullPointerException 如果实现类禁止参数为null
     * @throws ClassCastException   如果value的类型与实际类型不符
     */
    void setValue(T value);
}
