package cn.wjybxx.common.temp.cfg2;

import cn.wjybxx.common.EnumLite;
import cn.wjybxx.common.EnumLiteMap;
import cn.wjybxx.common.EnumUtils;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import java.util.List;

@BinarySerializable
public enum String2IntEnum implements EnumLite {
    ONE(1),

    TWO(2),

    THREE(3);

    public static final EnumLiteMap<String2IntEnum> VALUE_MAP = EnumUtils.mapping(values());

    public static final List<String2IntEnum> VALUES = VALUE_MAP.values();

    public static final int MIN_VALUE = 1;

    public static final int MAX_VALUE = 3;

    public final int number;

    String2IntEnum(int number) {
        this.number = number;
    }

    @Override
    public final int getNumber() {
        return number;
    }

    public static String2IntEnum forNumber(int number) {
        return VALUE_MAP.forNumber(number);
    }

    public static String2IntEnum forNumber(int number, String2IntEnum def) {
        return VALUE_MAP.forNumber(number, def);
    }

    public static String2IntEnum checkedForNumber(final int number) {
        return VALUE_MAP.checkedForNumber(number);
    }
}
