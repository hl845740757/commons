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
 * @author wjybxx
 * date - 2024/1/4
 */
public interface MutableNumber<T extends Number> extends Mutable<T> {

    int intValue();

    long longValue();

    float floatValue();

    double doubleValue();

    // region 四则运算

    /**
     * @param operand 操作数
     * @return this
     * @throws NullPointerException 如果操作数为null
     */
    MutableNumber<T> add(Number operand);

    /**
     * @param operand 操作数
     * @return this
     * @throws NullPointerException 如果操作数为null
     */
    MutableNumber<T> subtract(Number operand);

    /**
     * @param operand 操作数
     * @return this
     * @throws NullPointerException 如果操作数为null
     */
    MutableNumber<T> multiply(Number operand);

    /**
     * @param operand 操作数
     * @return this
     * @throws NullPointerException 如果操作数为null
     */
    MutableNumber<T> divide(Number operand);

    // endregion
}
