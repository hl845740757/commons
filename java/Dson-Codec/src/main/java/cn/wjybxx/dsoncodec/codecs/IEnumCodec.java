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

package cn.wjybxx.dsoncodec.codecs;

import cn.wjybxx.dsoncodec.DsonCodec;

/**
 * 接口不添加Enum限制，该接口用于避免拆装箱等问题
 *
 * @author wjybxx
 * date - 2024/5/11
 */
public interface IEnumCodec<T> extends DsonCodec<T> {

    T forNumber(int number);

    T forName(String name);

    String getName(T val);

    int getNumber(T val);
}