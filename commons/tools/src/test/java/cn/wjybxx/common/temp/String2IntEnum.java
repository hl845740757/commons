package cn.wjybxx.common.temp;

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.base.EnumLiteMap;
import cn.wjybxx.base.EnumUtils;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import java.util.List;

@BinarySerializable
public enum String2IntEnum implements EnumLite {
    ONE(1),

    TWO(2),

    THREE(3);

    public static final EnumLiteMap<String2IntEnum> MAPPER = EnumUtils.mapping(values());

    public static final List<String2IntEnum> VALUES = MAPPER.values();

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
        return MAPPER.forNumber(number);
    }

    public static String2IntEnum forNumber(int number, String2IntEnum def) {
        return MAPPER.forNumber(number, def);
    }

    public static String2IntEnum checkedForNumber(final int number) {
        return MAPPER.checkedForNumber(number);
    }
}
