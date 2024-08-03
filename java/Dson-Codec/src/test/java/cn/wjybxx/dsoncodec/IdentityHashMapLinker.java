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

import cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerBean;
import cn.wjybxx.dsoncodec.annotations.DsonProperty;

import java.util.IdentityHashMap;

/**
 * 测试静态代理的字段读写代理和钩子方法
 *
 * @author houlei
 * date - 2024/4/16
 */
@DsonCodecLinkerBean(value = IdentityHashMap.class)
public class IdentityHashMapLinker {

    // 新版本中，不包含public getter/setter的字段已默认跳过
    @DsonProperty(readProxy = "readSize", writeProxy = "writeSize")
    private IdentityHashMap<?, ?> size;

    public static void writeSize(IdentityHashMap<?, ?> inst, DsonObjectWriter writer, String name) {

    }

    public static void readSize(IdentityHashMap<?, ?> inst, DsonObjectReader reader, String name) {

    }

    public static IdentityHashMap<?, ?> newInstance(DsonObjectReader reader, TypeInfo<?> typeInfo) {
        return new IdentityHashMap<>();
    }

    public static void beforeEncode(IdentityHashMap<?, ?> inst, ConverterOptions options) {

    }

    public static void afterDecode(IdentityHashMap<?, ?> inst, ConverterOptions options) {

    }
}
