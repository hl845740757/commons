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

import cn.wjybxx.dson.DsonReaderSettings;
import cn.wjybxx.dson.DsonType;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/10/14
 */
public class DsonTextReaderSettings extends DsonReaderSettings {

    public static final DsonTextReaderSettings DEFAULT = newBuilder().build();

    public final DsonType localIdType;

    protected DsonTextReaderSettings(Builder builder) {
        super(builder);
        localIdType = Objects.requireNonNull(builder.localIdType);

        if (localIdType != DsonType.INT32
                && localIdType != DsonType.INT64
                && localIdType != DsonType.STRING) {
            throw new IllegalArgumentException("invalid localIdType: " + localIdType);
        }
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder extends DsonReaderSettings.Builder {
        /** localId的类型 -- 限制int32、int64、string */
        private DsonType localIdType = DsonType.STRING;

        protected Builder() {
        }

        public DsonType getLocalIdType() {
            return localIdType;
        }

        public Builder setLocalIdType(DsonType localIdType) {
            this.localIdType = localIdType;
            return this;
        }

        @Override
        public DsonTextReaderSettings build() {
            return new DsonTextReaderSettings(this);
        }
    }
}