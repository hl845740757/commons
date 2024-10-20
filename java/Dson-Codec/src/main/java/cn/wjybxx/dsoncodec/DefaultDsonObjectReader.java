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

import cn.wjybxx.dson.DsonReader;
import cn.wjybxx.dson.DsonType;

import java.util.Objects;

/**
 * 顺序解码没有额外的开销，但数据兼容性会变差。
 * 如果觉得{@link BufferedDsonObjectReader}的开销有点大，可以选择顺序解码模式
 *
 * @author wjybxx
 * date - 2023/4/23
 */
final class DefaultDsonObjectReader extends AbstractObjectReader implements DsonObjectReader {

    public DefaultDsonObjectReader(DsonConverter converter, DsonReader reader) {
        super(converter, reader);
    }

    @Override
    public boolean readName(String name) {
        DsonReader reader = this.reader;
        // array
        if (reader.getContextType().isArrayLike()) {
            if (reader.isAtValue()) {
                return true;
            }
            if (reader.isAtType()) {
                return reader.readDsonType() != DsonType.END_OF_OBJECT;
            }
            return reader.getCurrentDsonType() != DsonType.END_OF_OBJECT;
        }
        // object
        if (reader.isAtValue()) {
            return name == null || reader.getCurrentName().equals(name);
        }
        Objects.requireNonNull(name, "name");
        if (reader.isAtType()) {
            if (reader.readDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
        }
        reader.readName(name);
        return true;
    }

    @Override
    public void setEncoderType(TypeInfo encoderType) {
        reader.attach(encoderType);
    }

    @Override
    public TypeInfo getEncoderType() {
        return (TypeInfo) reader.attachment();
    }
}