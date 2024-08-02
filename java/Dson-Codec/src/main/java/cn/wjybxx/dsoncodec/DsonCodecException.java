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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.io.DsonIOException;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonCodecException extends DsonIOException {

    public DsonCodecException() {
    }

    public DsonCodecException(String message) {
        super(message);
    }

    public DsonCodecException(String message, Throwable cause) {
        super(message, cause);
    }

    public DsonCodecException(Throwable cause) {
        super(cause);
    }

    public DsonCodecException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }

    public static DsonCodecException wrap(Exception e) {
        if (e instanceof DsonCodecException) {
            return (DsonCodecException) e;
        }
        return new DsonCodecException(e);
    }

    //

    public static DsonCodecException unsupportedType(Class<?> type) {
        return new DsonCodecException("Can't find a codec for " + type);
    }

    public static DsonCodecException unsupportedKeyType(Class<?> type) {
        return new DsonCodecException("Can't find a codec for " + type + ", or key is not EnumLite");
    }

    public static DsonCodecException enumAbsent(Class<?> declared, String value) {
        return new DsonCodecException(String.format("EnumLite is absent, declared: %s, value: %s", declared, value));
    }

    public static DsonCodecException incompatible(Class<?> declared, DsonType dsonType) {
        return new DsonCodecException(String.format("Incompatible data format, declaredType %s, dsonType %s", declared, dsonType));
    }

    public static DsonCodecException incompatible(DsonType expected, DsonType dsonType) {
        return new DsonCodecException(String.format("Incompatible data format, expected %s, dsonType %s", expected, dsonType));
    }

    public static <T> DsonCodecException incompatible(Class<?> declared, T classId) {
        return new DsonCodecException(String.format("Incompatible data format, declaredType %s, classId %s", declared, classId));
    }
}