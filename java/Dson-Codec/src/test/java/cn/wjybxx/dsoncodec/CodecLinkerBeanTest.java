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

import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerBean;
import cn.wjybxx.dsoncodec.annotations.DsonProperty;

/**
 * {@link DsonCodecLinkerBean}的使用示例
 *
 * @author wjybxx
 * date - 2024/4/16
 */
@DsonCodecLinkerBean(value = ThirdPartyBean2.class)
public class CodecLinkerBeanTest {

    @DsonProperty(wireType = WireType.UINT)
    private ThirdPartyBean2 age;

    @DsonProperty(stringStyle = StringStyle.AUTO_QUOTE)
    private ThirdPartyBean2 name;

    // 这些钩子方法，生成的代码会自动调用

    public static void beforeEncode(ThirdPartyBean2 inst, ConverterOptions options) {
    }

    public static void writeObject(ThirdPartyBean2 inst, DsonObjectWriter writer) {
    }

    public static ThirdPartyBean2 newInstance(DsonObjectReader reader, TypeInfo<?> typeInfo) {
        return new ThirdPartyBean2();
    }

    public static void readObject(ThirdPartyBean2 inst, DsonObjectReader reader) {
    }

    public static void afterDecode(ThirdPartyBean2 inst, ConverterOptions options) {
    }

}
