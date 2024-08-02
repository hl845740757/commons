package cn.wjybxx.dson.ext;

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.Immutable;
import java.util.HashSet;
import java.util.Map;
import java.util.Objects;
import java.util.Set;

/**
 * 投影
 * <h2>多路径表达式</h2>
 * 多路径表达式需要使用标准的Dson文本表达。
 * <pre>{@code
 *  {
 *    name: 1,
 *    age: 0, // 不返回age字段
 *    pos: {
 *      $header: 1, // 返回pos的header
 *      $all: 1, // 返回pos的全部字段 -- 可表示这是一个object映射
 *      z: 0 // 排除z
 *   },
 *   arr1: {
 *      $slice: 1, // $slice 用于对数组进行切片投影
 *      $elem: {  // $elem 用于对数组元素进行投影
 *         name: 1,
 *         pos: 1
 *      }
 *   },
 *   arr2: {$slice: 1}, // 跳过1个元素，选择剩余所有元素
 *   arr3: {$slice: [0, 5]}, // 选择数组的前5个元素、
 *   arr4: 1, // 返回arr4整个数组
 *
 *   key1: {}, // 如果key1存在，则返回对应空Object/空Array。
 *   key2: {$header: 1}, // // 如果key2存在，返回的空Object或空Array将包含header。
 *  }
 * }</pre>
 *
 * <h2>规则</h2>
 * 1. $header 表示投影对象的header，header总是全量投影；header默认不返回，只有显式指定的情况下返回；
 * 2. value为1表示选择，为0表示排除；全为0时表示反选模式，否则只返回value为1的字段 -- header不计入。
 * 3. $all 用于选择object的所有字段，强制为反选字段模式；主要解决声明header的影响，也方便进入排除模式。
 * 4. 如果无法根据投影信息确定投影值的类型，将由真实的数据决定返回值类型 -- 可用于测试数据类型。
 * 5. $slice 表示数组范围投影，对数组进行细粒度投影时必须声明$slice，否则返回空数组。
 * 6. $slice skip 表示跳过skip个元素，截取剩余部分；兼容 $slice [skip]输入；
 * 7. $slice [skip, count] 表示跳过skip个元素，截取指定个数的元素部分；
 * 8. $elem 表示数组元素进行投影。
 * 9. Object的投影为Object，Array的投影为Array。
 * 10. 点号'.'默认不是路径分隔符，需要快捷语法时需要用户自行定义。
 * <p>
 * Q：为什么不支持反向索引？
 * A：我们不会在普通配置上存储数组的元素个数，因此反向索引必须解析所有的数组元素，用户直接获取所有元素即可。。。
 * 数据库通常会支持反向索引，这是因为数据库数据不是手工直接修改的，因此数据库可以在数据上存储一些元数据，实现快速截取。
 *
 * @author wjybxx
 * date - 2023/12/29
 */
@Immutable
public class Projection {

    /** 用于选择header */
    public static final String KEY_HEADER = "$header";
    /** 用于强调投影为object */
    public static final String KEY_OBJECT = "$object";
    /** 用于选择Object内的所有键 */
    public static final String KEY_ALL = "$all";

    /** 用于强调投影为数组 */
    public static final String KEY_ARRAY = "$array";
    /** 用于对数组切片 */
    public static final String KEY_SLICE = "$slice";
    /** 用于对数组元素进行投影 */
    public static final String KEY_ELEM = "$elem";

    /** object投影的特殊键 */
    public static final Set<String> OBJECT_KEYS = Set.of(KEY_OBJECT, KEY_ALL);
    /** 数组投影的特殊键 */
    public static final Set<String> ARRAY_KEYS = Set.of(KEY_ARRAY, KEY_SLICE, KEY_ELEM);
    /** 所有的特殊键 */
    public static final Set<String> ALL_SPECIAL_KEYS;

    static {
        HashSet<String> tempAllKeys = CollectionUtils.newHashSet(8);
        tempAllKeys.add(KEY_HEADER);
        tempAllKeys.addAll(OBJECT_KEYS);
        tempAllKeys.addAll(ARRAY_KEYS);
        ALL_SPECIAL_KEYS = Set.copyOf(tempAllKeys);
    }

    /** 表达式的根节点 */
    private final Node root;

    public Projection(String projectInfo) {
        this(Dsons.fromDson(projectInfo).asObject());
    }

    public Projection(DsonObject<String> projectInfo) {
        root = parseNode(projectInfo);
    }

    /** 将给定Dson字符串进行投影 */
    public DsonValue project(String dsonString) {
        return project(new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString));
    }

    /**
     * 1.如果投影为Array，则投可投影顶层的Header，返回值必定为{@link DsonArray}
     * 2.如果投影为Object，则只返回第一个对象的投影，顶层Header被当做普通对象投影。
     */
    public DsonValue project(DsonReader reader) {
        if (root instanceof DefaultNode node && node.arrayLike) {
            return new Matcher(reader, root).projectTopArray();
        } else {
            DsonType dsonType = reader.readDsonType();
            if (dsonType == DsonType.END_OF_OBJECT) {
                return null;
            }
            Matcher matcher = new Matcher(reader, root);
            return matcher.project();
        }
    }

    private static class Matcher {

        final DsonReader reader;
        final Node node;

        private Matcher(DsonReader reader, Node node) {
            this.reader = reader;
            this.node = node;
        }

        DsonValue project() {
            DsonType currentDsonType = reader.getCurrentDsonType();
            if (!node.testType(currentDsonType)) {
                reader.skipValue();
                // 上下文不匹配时返回期望的类型
                if (node instanceof DefaultNode defaultNode) {
                    return defaultNode.arrayLike ? new DsonArray<>(0) : new DsonObject<>(0);
                }
                // 语义不清楚的情况下返回真实的类型
                if (currentDsonType == DsonType.ARRAY) {
                    return new DsonArray<>(0);
                }
                if (currentDsonType == DsonType.HEADER) {
                    return new DsonHeader<>();
                }
                return new DsonObject<>(0);
            }
            if (currentDsonType == DsonType.ARRAY) {
                return projectArray();
            }
            if (currentDsonType == DsonType.HEADER) {
                return projectHeader();
            }
            return projectObject();
        }

        private static boolean needMatcher(Node fieldNode) {
            return fieldNode.isProjectNode() && !(fieldNode instanceof SelectNode);
        }

        private DsonHeader<String> projectHeader() {
            DsonHeader<String> dsonObject = new DsonHeader<>();
            DsonType dsonType;
            String name;
            DsonValue value;
            reader.readStartHeader();
            while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
                name = reader.readName();
                if (node.testField(name)) {
                    Node fieldNode = node.getFieldNode(name);
                    if (needMatcher(fieldNode)) {
                        value = new Matcher(reader, fieldNode).project();
                    } else {
                        value = Dsons.readDsonValue(reader);
                    }
                    dsonObject.put(name, value);
                } else {
                    reader.skipValue();
                }
            }
            reader.readEndHeader();
            return dsonObject;
        }

        DsonObject<String> projectObject() {
            DsonObject<String> dsonObject = new DsonObject<>();
            DsonType dsonType;
            String name;
            DsonValue value;
            reader.readStartObject();
            while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
                if (dsonType == DsonType.HEADER) {
                    if (node.testHeader()) {
                        Dsons.readHeader(reader, dsonObject.getHeader());
                    } else {
                        reader.skipValue();
                    }
                    if (node.remainCount(dsonObject.size()) == 0) {
                        reader.skipToEndOfObject(); // 不再继续读；header不在计数中，因此放header后
                        break;
                    }
                    continue;
                }
                name = reader.readName();
                if (node.testField(name)) {
                    Node fieldNode = node.getFieldNode(name);
                    if (needMatcher(fieldNode)) {
                        value = new Matcher(reader, fieldNode).project();
                    } else {
                        value = Dsons.readDsonValue(reader);
                    }
                    dsonObject.put(name, value);
                    if (node.remainCount(dsonObject.size()) == 0) {
                        reader.skipToEndOfObject();
                        break;
                    }
                } else {
                    reader.skipValue();
                }
            }
            reader.readEndObject();
            return dsonObject;
        }

        DsonArray<String> projectArray() {
            DsonArray<String> dsonArray = new DsonArray<>();
            DsonType dsonType;
            DsonValue value;
            int index = 0;
            reader.readStartArray();
            while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
                if (dsonType == DsonType.HEADER) {
                    if (node.testHeader()) {
                        Dsons.readHeader(reader, dsonArray.getHeader());
                    } else {
                        reader.skipValue();
                    }
                    if (node.remainCount(dsonArray.size()) == 0) {
                        reader.skipToEndOfObject(); // 不再继续读；header不在计数中，因此放header后
                        break;
                    }
                    continue;
                }

                if (node.testElement(index++)) {
                    Node elemNode = node.getElemNode();
                    if (needMatcher(elemNode)) {
                        value = new Matcher(reader, elemNode).project();
                    } else {
                        value = Dsons.readDsonValue(reader);
                    }
                    dsonArray.add(value);
                    if (node.remainCount(dsonArray.size()) == 0) {
                        reader.skipToEndOfObject();
                        break;
                    }
                } else {
                    reader.skipValue();
                }
            }
            reader.readEndArray();
            return dsonArray;
        }

        DsonArray<String> projectTopArray() {
            DsonArray<String> dsonArray = new DsonArray<>();
            DsonType dsonType;
            DsonValue value;
            int index = 0;
            while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
                if (dsonType == DsonType.HEADER) {
                    if (node.testHeader()) {
                        Dsons.readHeader(reader, dsonArray.getHeader());
                    } else {
                        reader.skipValue();
                    }
                    if (node.remainCount(dsonArray.size()) == 0) {
                        break; // 不再继续读；header不在计数中，因此放header后
                    }
                    continue;
                }
                if (node.testElement(index++)) {
                    Node elemNode = node.getElemNode();
                    if (needMatcher(elemNode)) {
                        value = new Matcher(reader, elemNode).project();
                    } else {
                        value = Dsons.readDsonValue(reader);
                    }
                    dsonArray.add(value);
                    if (node.remainCount(dsonArray.size()) == 0) {
                        break;
                    }
                } else {
                    reader.skipValue();
                }
            }
            return dsonArray;
        }

    }

    // region node

    private static final Node DISCARD_NODE = new DiscardNode();
    private static final Node SELECT_NODE = new SelectNode();

    /**
     * Node表示预编译的节点
     * 1.不能总是解析用户的文本，因此需要提前编译缓存
     * 2.Node仅仅是保存编译后的数据，即包含Object的投影信息，也包含Array的投影信息。
     * 3.只有在真正执行投影时才知道数据的类型，因此Node不能直接运行
     * 4.node无需记录父子关系，Matcher记录即可。
     */
    private static abstract class Node {

        /** 是否是需要投影的节点 */
        public final boolean isProjectNode() {
            return !(this instanceof DiscardNode);
        }

        /** 测试节点类型是否匹配 */
        public abstract boolean testType(DsonType dsonType);

        /** 测试对象的header是否需要投影 */
        public abstract boolean testHeader();

        /** 测试对象的特定字段是否需要返回 */
        public abstract boolean testField(String key);

        /** 测试数组的特点下标元素是否需要返回 */
        public abstract boolean testElement(int index);

        /** 剩余需要投影的成员数量，-1表示未知 */
        public abstract int remainCount(int current);

        /** 获取字段投影的Node信息 */
        @Nonnull
        public abstract Node getFieldNode(String key);

        /** 获取数组元素投影的Node信息 */
        @Nonnull
        public abstract Node getElemNode();
    }

    /** DefaultNode代表的是{k:v}构建的节点 */
    private static class DefaultNode extends Node {

        /** 匹配的上下文是否是数组类型 */
        final boolean arrayLike;
        /** 是否投影header */
        final boolean includeHeader;

        /** object的投影模式 */
        final SelectMode selectMode;
        /** 字段的投影信息 */
        final Map<String, Node> fieldNodes;
        /** 被选择的字段数 */
        final int selectCount;

        /** 数组切片信息 */
        final SliceSpec sliceSpec;
        /** 数组元素投影信息 */
        final Node elementNode;

        public DefaultNode(boolean arrayLike, DsonObject<String> projectInfo) {
            this.arrayLike = arrayLike;
            this.includeHeader = isTrue(projectInfo.get(KEY_HEADER));
            // object 字段映射
            {
                fieldNodes = CollectionUtils.newHashMap(projectInfo.size());
                int count = 0;
                for (Map.Entry<String, DsonValue> entry : projectInfo.entrySet()) {
                    String key = entry.getKey();
                    if (ALL_SPECIAL_KEYS.contains(key)) {
                        continue;
                    }
                    DsonValue value = entry.getValue();
                    Node childNode = parseNode(value);
                    fieldNodes.put(key, childNode);
                    if (childNode.isProjectNode()) {
                        count++;
                    }
                }
                this.selectCount = count;

                final DsonValue allValue = projectInfo.get(KEY_ALL);
                if (isTrue(allValue)) { // 指定$all的情况下直接进入反选模式
                    selectMode = SelectMode.INVERT;
                } else if (!fieldNodes.isEmpty() && count == 0) { // 指定了key，且所有key的value都是0
                    selectMode = SelectMode.INVERT;
                } else {
                    selectMode = SelectMode.NORMAL;
                }
            }
            // array 映射
            {
                final DsonValue sliceValue = projectInfo.get(KEY_SLICE);
                if (sliceValue == null) {
                    // 未声明slice的情况下，返回空数组
                    sliceSpec = SliceSpec.EMPTY;
                } else {
                    sliceSpec = parseSliceSpec(sliceValue);
                }
                DsonValue elemValue = projectInfo.get(KEY_ELEM);
                if (elemValue == null) {
                    // 未声明elem的情况下，返回原始对象
                    elementNode = SELECT_NODE;
                } else {
                    elementNode = parseNode(elemValue);
                }
            }
        }

        @Override
        public boolean testType(DsonType dsonType) {
            return arrayLike ? dsonType == DsonType.ARRAY : dsonType.isObjectLike();
        }

        public boolean testHeader() {
            return includeHeader;
        }

        public boolean testField(String key) {
            if (arrayLike) {
                return false;
            }
            Node node = fieldNodes.get(key);
            if (this.selectMode == SelectMode.NORMAL) {
                return node != null && node.isProjectNode();
            }
            return node == null || node.isProjectNode();
        }

        public boolean testElement(int index) {
            if (!arrayLike) {
                return false;
            }
            if (index < sliceSpec.skip) {
                return false;
            }
            if (sliceSpec.count == -1) { // 全投影
                return true;
            }
            return index < sliceSpec.skip + sliceSpec.count; // 有限投影
        }

        @Override
        public int remainCount(int current) {
            if (arrayLike) {
                if (sliceSpec.count == -1) {
                    return -1;
                }
                return Math.max(0, sliceSpec.count - current);
            }
            return selectMode == SelectMode.NORMAL ? Math.max(0, selectCount - current) : -1;
        }

        @Nonnull
        @Override
        public Node getFieldNode(String key) {
            if (arrayLike) {
                return DISCARD_NODE;
            }
            if (selectMode == SelectMode.NORMAL) {
                return fieldNodes.getOrDefault(key, DISCARD_NODE);
            }
            return fieldNodes.getOrDefault(key, SELECT_NODE);
        }

        @Nonnull
        @Override
        public Node getElemNode() {
            if (!arrayLike) {
                return DISCARD_NODE;
            }
            return elementNode;
        }
    }

    /** 无法识别上下文类型的Node -- 比如：{}, {$header: 1} */
    private static class UnknownContextNode extends Node {

        final boolean includeHeader;

        public UnknownContextNode(DsonObject<String> projectInfo) {
            includeHeader = isTrue(projectInfo.get(KEY_HEADER));
        }

        @Override
        public boolean testType(DsonType dsonType) {
            return true;
        }

        @Override
        public boolean testHeader() {
            return includeHeader;
        }

        @Override
        public boolean testField(String key) {
            return false;
        }

        @Override
        public boolean testElement(int index) {
            return false;
        }

        @Override
        public int remainCount(int current) {
            return 0;
        }

        @Nonnull
        @Override
        public Node getFieldNode(String key) {
            return DISCARD_NODE;
        }

        @Nonnull
        @Override
        public Node getElemNode() {
            return DISCARD_NODE;
        }
    }

    /** 简单丢弃节点 -- value为0 */
    private static class DiscardNode extends Node {

        @Override
        public boolean testType(DsonType dsonType) {
            return false;
        }

        @Override
        public boolean testHeader() {
            return false;
        }

        @Override
        public boolean testField(String key) {
            return false;
        }

        @Override
        public boolean testElement(int index) {
            return false;
        }

        @Override
        public int remainCount(int current) {
            return 0;
        }

        @Nonnull
        @Override
        public Node getFieldNode(String key) {
            return this;
        }

        @Nonnull
        @Override
        public Node getElemNode() {
            return this;
        }
    }

    /** 简单选择节点 -- value为1 */
    private static class SelectNode extends Node {

        @Override
        public boolean testType(DsonType dsonType) {
            return true;
        }

        @Override
        public boolean testHeader() {
            return false; // header默认被忽略
        }

        @Override
        public boolean testField(String key) {
            return true;
        }

        @Override
        public boolean testElement(int index) {
            return true;
        }

        @Override
        public int remainCount(int current) {
            return -1;
        }

        @Nonnull
        @Override
        public Node getFieldNode(String key) {
            return this;
        }

        @Nonnull
        @Override
        public Node getElemNode() {
            return this;
        }
    }

    private static Node parseNode(DsonValue childSpec) {
        Objects.requireNonNull(childSpec);
        if (childSpec instanceof DsonBool dsonBool) { // true or false
            return dsonBool.getValue() ? SELECT_NODE : DISCARD_NODE;
        }
        if (childSpec instanceof DsonNumber dsonNumber) { // 0 or 1
            return dsonNumber.intValue() == 1 ? SELECT_NODE : DISCARD_NODE;
        }
        DsonObject<String> childProjInfo = childSpec.asObject();
        if (childProjInfo.isEmpty()) { // {}
            return new UnknownContextNode(childProjInfo);
        }
        if (childProjInfo.size() == 1
                && childProjInfo.containsKey(KEY_HEADER)) { // {$header: 1}
            return new UnknownContextNode(childProjInfo);
        }
        for (String arrayKey : ARRAY_KEYS) {
            if (childProjInfo.containsKey(arrayKey)) { // {$slice: 1}
                return new DefaultNode(true, childProjInfo);
            }
        }
        // 默认为object上下文
        return new DefaultNode(false, childProjInfo);
    }

    private static boolean isTrue(DsonValue dsonValue) {
        if (dsonValue == null) return false;
        if (dsonValue.getDsonType() == DsonType.BOOL) {
            return dsonValue.asBool();
        }
        if (dsonValue.isNumber()) {
            return dsonValue.asDsonNumber().intValue() == 1;
        }
        return false;
    }

    private static SliceSpec parseSliceSpec(DsonValue rangeValue) {
        if (rangeValue.isNumber()) {
            int skip = rangeValue.asDsonNumber().intValue();
            return new SliceSpec(skip);
        }
        // todo 将slice看做字符串数组，就可以解析正负号
        DsonArray<String> array = rangeValue.asArray();
        return switch (array.size()) {
            case 0 -> SliceSpec.EMPTY;
            case 1 -> {
                int skip = array.get(0).asDsonNumber().intValue();
                yield new SliceSpec(skip);
            }
            case 2 -> {
                int skip = array.get(0).asDsonNumber().intValue();
                int count = array.get(1).asDsonNumber().intValue();
                yield new SliceSpec(skip, count);
            }
            default -> {
                throw new DsonIOException("invalid slice range: " + rangeValue);
            }
        };
    }

    /** object类型的投影模式 */
    private enum SelectMode {
        /** 普通选择，选择给定的键 -- 投影信息为空 或 value包含至少一个1 */
        NORMAL,
        /** 反选，排除给定的键 -- 投影信息value全部为0 */
        INVERT,
    }

    /** 数组切片范围 */
    private static class SliceSpec {

        static final SliceSpec EMPTY = new SliceSpec(0, 0);
        static final SliceSpec FIRST = new SliceSpec(0, 1);
        static final SliceSpec FULL = new SliceSpec(0, -1);

        final int skip;
        final int count;

        public SliceSpec(int skip) {
            this.skip = skip;
            this.count = -1;
        }

        public SliceSpec(int skip, int count) {
            this.skip = skip;
            this.count = count;
        }
    }

    // endregion
}