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

package cn.wjybxx.dson;

import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public abstract class DsonValue {

    @Nonnull
    public abstract DsonType getDsonType();

    // region 拆箱类型
    public int asInt32() {
        return ((DsonInt32) this).intValue();
    }

    public long asInt64() {
        return ((DsonInt64) this).longValue();
    }

    public float asFloat() {
        return ((DsonFloat) this).floatValue();
    }

    public double asDouble() {
        return ((DsonDouble) this).doubleValue();
    }

    public boolean asBool() {
        return ((DsonBool) this).getValue();
    }

    public String asString() {
        return ((DsonString) this).getValue();
    }

    public Binary asBinary() {
        return ((DsonBinary) this).binary();
    }

    public ObjectPtr asPointer() {
        return ((DsonPointer) this).getValue();
    }

    public ObjectLitePtr asLitePointer() {
        return ((DsonLitePointer) this).getValue();
    }

    public ExtDateTime asDateTime() {
        return ((DsonDateTime) this).getValue();
    }

    public Timestamp asTimestamp() {
        return ((DsonTimestamp) this).getValue();
    }

    // endregion

    // region number

    public boolean isNumber() {
        return getDsonType().isNumber();
    }

    public DsonNumber asDsonNumber() {
        return (DsonNumber) this;
    }

    // endregion

    // region Dson特定类型

    @SuppressWarnings("unchecked")
    public DsonHeader<String> asHeader() {
        return (DsonHeader<String>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonArray<String> asArray() {
        return (DsonArray<String>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonObject<String> asObject() {
        return (DsonObject<String>) this;
    }

    //

    @SuppressWarnings("unchecked")
    public DsonHeader<FieldNumber> asHeaderLite() {
        return (DsonHeader<FieldNumber>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonArray<FieldNumber> asArrayLite() {
        return (DsonArray<FieldNumber>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonObject<FieldNumber> asObjectLite() {
        return (DsonObject<FieldNumber>) this;
    }

    // endregion

}