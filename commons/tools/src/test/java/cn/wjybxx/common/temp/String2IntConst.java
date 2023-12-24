package cn.wjybxx.common.temp;

import cn.wjybxx.base.CollectionUtils;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;
import it.unimi.dsi.fastutil.ints.IntSets;
import java.util.LinkedHashMap;
import java.util.Map;

public class String2IntConst {
    public static final int ONE = 1;

    public static final int TWO = 2;

    public static final int THREE = 3;

    public static final Map<String, Integer> VALUE_MAP;

    public static final IntSet VALUE_SET;

    public static final int MIN_VALUE = 1;

    public static final int MAX_VALUE = 3;

    static {
         {
            Map<String, Integer> tempMap = new LinkedHashMap<>();
            tempMap.put("ONE", 1);
            tempMap.put("TWO", 2);
            tempMap.put("THREE", 3);
            VALUE_MAP = CollectionUtils.toImmutableLinkedHashMap(tempMap);
        }
         {
            IntSet tempSet = new IntOpenHashSet(VALUE_MAP.values());
            VALUE_SET = IntSets.unmodifiable(tempSet);
        }
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

    public static boolean containsValue(int value) {
        return VALUE_SET.contains(value);
    }

    public static boolean isBetween(int value) {
        if (VALUE_SET.isEmpty()) return false;
        return value >= MIN_VALUE && value <= MAX_VALUE;
    }
}
