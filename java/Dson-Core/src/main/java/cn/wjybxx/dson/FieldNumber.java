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

import javax.annotation.Nonnull;

/**
 * 二进制编码下字段编号表示法
 * 注意：
 * 1. 不能直接使用{@code fullNumber}比较两个字段在类继承体系中的顺序，应当使用{@link #compareTo(FieldNumber)}进行比较
 * 2. 由于{@link FieldNumber}包含了类的继承信息和字段定义顺序，因此对于处理序列化数据的兼容十分有用。
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class FieldNumber implements Comparable<FieldNumber> {

    public static final FieldNumber ZERO = new FieldNumber((byte) 0, 0);

    /** 字段完整编号 */
    private final int fullNumber;

    public FieldNumber(byte idep, int lnumber) {
        if (idep < 0 || idep > DsonLites.IDEP_MAX_VALUE) {
            throw invalidArgs(idep, lnumber);
        }
        if (lnumber < 0) {
            throw invalidArgs(idep, lnumber);
        }
        this.fullNumber = DsonLites.makeFullNumber(idep, lnumber);
    }

    private FieldNumber(int fullNumber) {
        this.fullNumber = fullNumber;
    }

    private static IllegalArgumentException invalidArgs(byte idep, int lnumber) {
        return new IllegalArgumentException(String.format("invalid idep:%d or lnumber:%d", lnumber, idep));
    }

    public static FieldNumber ofLnumber(int lnumber) {
        return new FieldNumber((byte) 0, lnumber);
    }

    public static FieldNumber ofFullNumber(int fullNumber) {
        return new FieldNumber(DsonLites.idepOfFullNumber(fullNumber),
                DsonLites.lnumberOfFullNumber(fullNumber));
    }

    /** 获取字段的完整编号 */
    public int getFullNumber() {
        return fullNumber;
    }

    /** 类的继承深度 - Depth of Inheritance */
    public byte getIdep() {
        return DsonLites.idepOfFullNumber(fullNumber);
    }

    /** 字段正在类本地的编号 - localNumber */
    public int getLnumber() {
        return DsonLites.lnumberOfFullNumber(fullNumber);
    }

    // region 比较

    /** 比较两个fullNumber的大小 */
    public static int compare(int fullNumber1, int fullNumber2) {
        if (fullNumber1 == fullNumber2) {
            return 0;
        }
        int r = Byte.compare(DsonLites.idepOfFullNumber(fullNumber1),
                DsonLites.idepOfFullNumber(fullNumber2));
        if (r != 0) {
            return r;
        }
        return Integer.compare(DsonLites.lnumberOfFullNumber(fullNumber1),
                DsonLites.lnumberOfFullNumber(fullNumber2));
    }

    @Override
    public int compareTo(@Nonnull FieldNumber that) {
        return compare(fullNumber, that.fullNumber);
    }

    // endregion

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        FieldNumber that = (FieldNumber) o;
        return fullNumber == that.fullNumber;
    }

    @Override
    public int hashCode() {
        return fullNumber;
    }

    // endregion

    @Override
    public String toString() {
        return "FieldNumber{" +
                "idep=" + getIdep() +
                ", lnumber=" + getLnumber() +
                '}';
    }
}