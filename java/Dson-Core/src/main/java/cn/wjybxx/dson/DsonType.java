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

/**
 * Dson数据类型
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public enum DsonType {

    /** 对象的结束标识 */
    END_OF_OBJECT(0),

    INT32(1),
    INT64(2),
    FLOAT(3),
    DOUBLE(4),
    BOOL(5),
    STRING(6),
    NULL(7),
    BINARY(8),

    /**
     * 对象指针
     */
    POINTER(11),
    /**
     * 轻量对象指针
     */
    LITE_POINTER(12),
    /**
     * 日期时间
     */
    DATETIME(13),
    /**
     * 时间戳
     */
    TIMESTAMP(14),

    /**
     * 对象头信息，与Object类型编码格式类似
     * 但header不可以再直接嵌入header
     */
    HEADER(29),
    /**
     * 数组(v,v,v...)
     */
    ARRAY(30),
    /**
     * 普通对象(k,v,k,v...)
     */
    OBJECT(31),
    ;
    private final int number;

    DsonType(int number) {
        assert number >= 0 && number <= Dsons.DSON_TYPE_MAX_VALUE;
        this.number = number;
    }

    private static final DsonType[] LOOK_UP = new DsonType[Dsons.DSON_TYPE_MAX_VALUE + 1];

    static {
        for (DsonType type : values()) {
            LOOK_UP[type.getNumber()] = type;
        }
    }

    public int getNumber() {
        return number;
    }

    public boolean isPrimitive() {
        return number >= 1 && number <= 6;
    }

    public boolean isNumber() {
        return switch (this) {
            case INT32, INT64, FLOAT, DOUBLE -> true;
            default -> false;
        };
    }

    /** {@link WireType} */
    public boolean hasWireType() {
        return switch (this) {
            case INT32, INT64 -> true;
            default -> false;
        };
    }

    /** header不属于普通意义上的容器 */
    public boolean isContainer() {
        return this == OBJECT || this == ARRAY;
    }

    /** DsonType是否是容器类型或Header */
    public boolean isContainerOrHeader() {
        return this == OBJECT || this == ARRAY || this == HEADER;
    }

    /** Dson是否是KV结构 */
    public boolean isObjectLike() {
        return this == OBJECT || this == HEADER;
    }

    public static DsonType forNumber(int number) {
        return LOOK_UP[number];
    }

}