package cn.wjybxx.dson;

import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.StyleOut;

import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/7/2
 */
public class TestUtils {

    @SuppressWarnings("unchecked")
    public static DsonValue toDsonValue(final Object object) {
        if (object instanceof Map<?, ?>) {
            Map<String, Object> map = (Map<String, Object>) object;
            DsonObject<String> dsonObject = new DsonObject<>();
            map.forEach((k, v) -> dsonObject.put(k, toDsonValue(v)));
            return dsonObject;
        } else if (object instanceof List<?>) {
            List<Object> array = (List<Object>) object;
            DsonArray<String> dsonArray = new DsonArray<>(array.size());
            for (Object v : array) {
                dsonArray.add(toDsonValue(v));
            }
            return dsonArray;
        } else if (object instanceof Integer i) {
            return new DsonInt32(i);
        } else if (object instanceof Long l) {
            return new DsonInt64(l);
        } else if (object instanceof Float f) {
            return new DsonFloat(f);
        } else if (object instanceof Double d) {
            return new DsonDouble(d);
        } else if (object instanceof Boolean b) {
            return DsonBool.valueOf(b);
        } else if (object instanceof String s) {
            return new DsonString(s);
        } else {
            throw new IllegalArgumentException("unsupported type " + object.getClass());
        }
    }

    public static String toRawBinaryString(float v) {
        StyleOut styleOut = new StyleOut();
        NumberStyle.FIXED_BINARY.toString(Float.floatToRawIntBits(v), styleOut);
        StringBuilder sb = new StringBuilder(styleOut.getValue().substring(2));
        sb.insert(24, '_');
        sb.insert(16, '_');
        sb.insert(8, '_');
        return sb.toString();
    }

    public static String toRawBinaryString(double v) {
        StyleOut styleOut = new StyleOut();
        NumberStyle.FIXED_BINARY.toString(Double.doubleToRawLongBits(v), styleOut);
        StringBuilder sb = new StringBuilder(styleOut.getValue().substring(2));
        sb.insert(56, '_');
        sb.insert(48, '_');
        sb.insert(40, '_');
        sb.insert(32, '_');
        sb.insert(24, '_');
        sb.insert(16, '_');
        sb.insert(8, '_');
        return sb.toString();
    }

}