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
 * 编解码器注册表
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface DsonCodecRegistry {

    /**
     * 查找编码器（encoder）。
     * 编码器可以接收子类实例，将子类实例按照超类编码，子类特殊数据丢弃。
     *
     * @param rootRegistry 用于比如想转换为查询超类的Encoder
     */
    @Nullable
    <T> DsonCodecImpl<? super T> getEncoder(Class<T> clazz, DsonCodecRegistry rootRegistry);

    /**
     * 查找解码器（decoder）。
     * 解码器必须目标类型一致，子类Codec不能安全解码超类数据，超类Codec返回的实例不能向下转型。
     * <p>
     * ps:在Java端其实可以有所变通，因为Java是伪泛型，因此{@code Codec<BaseType> }可以赋值给{@code Codec<SubType>}，
     * 因此如果超类的Codec创建的实例可以向下转型为参数目标类型，那么是可以返回超类的Codec的。
     *
     * @param rootRegistry 用于转换为查询子类的Decoder
     */
    @Nullable
    <T> DsonCodecImpl<T> getDecoder(Class<T> clazz, DsonCodecRegistry rootRegistry);

}