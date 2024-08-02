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

package cn.wjybxx.dson.text;

/**
 * 数字的打印格式
 * 如果实现类是有状态的，应当小心多线程问题，如果可能被多线程使用。
 * <p>
 * Q：该接口产生的原因？
 * A：多数情况下我们都可以使用普通的{@link NumberStyle}，但存在一个常见但无法默认支持的需求：浮点数的打印精度控制。
 * 浮点数有个常见的弊端，toString的结果可能非常非常长 —— 因为浮点数的精度有限，不能准确表达某些数，因此是近似值，
 * 但这样长的字符串通常不是我们想要的，因此某些时候需要能设定打印精度。
 *
 * @author wjybxx
 * date - 2023/7/15
 */
public interface INumberStyle {

    void toString(int value, StyleOut styleOut);

    void toString(long value, StyleOut styleOut);

    void toString(float value, StyleOut styleOut);

    void toString(double value, StyleOut styleOut);

}