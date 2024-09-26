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

import java.util.IdentityHashMap;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2024/9/25
 */
public class GenericCodecConfig implements IGenericCodecConfig {

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final Map<Class<?>, Class<?>> encoderTypeDic = new IdentityHashMap<>();
    private final Map<Class<?>, Class<?>> decoderTypeDic = new IdentityHashMap<>();

}
