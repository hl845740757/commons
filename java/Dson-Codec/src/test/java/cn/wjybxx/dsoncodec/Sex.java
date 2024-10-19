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

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.base.EnumLiteMap;
import cn.wjybxx.base.EnumUtils;
import cn.wjybxx.dsoncodec.annotations.DsonProperty;
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date - 2024/5/11
 */
@DsonSerializable
public enum Sex implements EnumLite {

    @DsonProperty(name = "Z")
    UNKNOWN(0),

    @DsonProperty(name = "M")
    MALE(1),

    @DsonProperty(name = "F")
    FEMALE(2);

    public final int number;

    Sex(int number) {
        this.number = number;
    }

    @Override
    public int getNumber() {
        return number;
    }

    public static final EnumLiteMap<Sex> MAPPER = EnumUtils.mapping(values());

    @Nullable
    public static Sex forNumber(int number) {
        return MAPPER.forNumber(number);
    }

    public static Sex checkedForNumber(int number) {
        return MAPPER.checkedForNumber(number);
    }
}
