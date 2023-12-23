package cn.wjybxx.common.temp;

import cn.wjybxx.common.CollectionUtils;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

public class String2StringConst {
    public static final String ONE = "1";

    public static final String TWO = "2";

    public static final String THREE = "3";

    public static final Map<String, String> VALUE_MAP;

    public static final Set<String> VALUE_SET;

    static {
         {
            Map<String, String> tempMap = new LinkedHashMap<>();
            tempMap.put("ONE", "1");
            tempMap.put("TWO", "2");
            tempMap.put("THREE", "3");
            VALUE_MAP = CollectionUtils.toImmutableLinkedHashMap(tempMap);
        }
        VALUE_SET = Set.copyOf(VALUE_MAP.values());
    }

    public static boolean isEmpty() {
        return VALUE_SET.isEmpty();
    }

    public static int size() {
        return VALUE_SET.size();
    }

    public static boolean containsKey(String key) {
        return VALUE_MAP.containsKey(key);
    }

    public static boolean containsValue(String value) {
        return VALUE_SET.contains(value);
    }
}
