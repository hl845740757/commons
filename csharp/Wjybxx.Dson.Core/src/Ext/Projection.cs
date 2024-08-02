#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Ext
{
/// <summary>
/// 投影
/// <h2>多路径表达式</h2>
/// 多路径表达式需要使用标准的Dson文本表达。
/// <code>
///  {
///    name: 1,
///    age: 0, // 不返回age字段
///    pos: {
///      $header: 1, // 返回pos的header
///      $all: 1, // 返回pos的全部字段 -- 可表示这是一个object映射
///      z: 0 // 排除z
///   },
///   arr1: {
///      $slice: 1, // $slice 用于对数组进行切片投影
///      $elem: {  // $elem 用于对数组元素进行投影
///         name: 1,
///         pos: 1
///      }
///   },
///   arr2: {$slice: 1}, // 跳过1个元素，选择剩余所有元素
///   arr3: {$slice: [0, 5]}, // 选择数组的前5个元素、
///   arr4: 1, // 返回arr4整个数组
///
///   key1: {}, // 如果key1存在，则返回对应空Object/空Array。
///   key2: {$header: 1}, // // 如果key2存在，返回的空Object或空Array将包含header。
///  }
/// </code>
///
/// <h2>规则</h2>
/// 1. $header 表示投影对象的header，header总是全量投影；header默认不返回，只有显式指定的情况下返回；
/// 2. value为1表示选择，为0表示排除；全为0时表示反选模式，否则只返回value为1的字段 -- header不计入。
/// 3. $all 用于选择object的所有字段，强制为反选字段模式；主要解决声明header的影响，也方便进入排除模式。
/// 4. 如果无法根据投影信息确定投影值的类型，将由真实的数据决定返回值类型 -- 可用于测试数据类型。
/// 5. $slice 表示数组范围投影，对数组进行细粒度投影时必须声明$slice，否则返回空数组。
/// 6. $slice skip 表示跳过skip个元素，截取剩余部分；兼容 $slice [skip]输入；
/// 7. $slice [skip, count] 表示跳过skip个元素，截取指定个数的元素部分；
/// 8. $elem 表示数组元素进行投影。
/// 9. Object的投影为Object，Array的投影为Array。
/// 10. 点号'.'默认不是路径分隔符，需要快捷语法时需要用户自行定义。
///
/// Q：为什么不支持反向索引？
/// A：我们不会在普通配置上存储数组的元素个数，因此反向索引必须解析所有的数组元素，用户直接获取所有元素即可。。。
/// 数据库通常会支持反向索引，这是因为数据库数据不是手工直接修改的，因此数据库可以在数据上存储一些元数据，实现快速截取。
/// 
/// </summary>
public class Projection
{
    /** 用于选择header */
    public const string KEY_HEADER = "$header";
    /** 用于强调投影为object */
    public const string KEY_OBJECT = "$object";
    /** 用于选择Object内的所有键 */
    public const string KEY_ALL = "$all";

    /** 用于强调投影为数组 */
    public const string KEY_ARRAY = "$array";
    /** 用于对数组切片 */
    public const string KEY_SLICE = "$slice";
    /** 用于对数组元素进行投影 */
    public const string KEY_ELEM = "$elem";

    /** object投影的特殊键 */
    private static readonly IGenericSet<string> OBJECT_KEYS = new[] { KEY_OBJECT, KEY_ALL }.ToImmutableLinkedHashSet();
    /** 数组投影的特殊键 */
    public static readonly IGenericSet<string> ARRAY_KEYS = new[] { KEY_ARRAY, KEY_SLICE, KEY_ELEM }.ToImmutableLinkedHashSet();
    /** 所有的特殊键 */
    public static readonly IGenericSet<string> ALL_SPECIAL_KEYS;

    static Projection() {
        HashSet<string> tempKeys = new HashSet<string>();
        tempKeys.Add(KEY_HEADER);
        tempKeys.AddAll(OBJECT_KEYS);
        tempKeys.AddAll(ARRAY_KEYS);
        ALL_SPECIAL_KEYS = tempKeys.ToImmutableLinkedHashSet();
    }

    /** 表达式的根节点 */
    private readonly Node root;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectInfo">Dson文本格式的投影信息</param>
    public Projection(String projectInfo)
        : this(Dsons.FromDson(projectInfo).AsObject()) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectInfo">结构化的投影信息</param>
    public Projection(DsonObject<String> projectInfo) {
        root = ParseNode(projectInfo);
    }


    /// <summary>
    /// 将指定Dson文本进行投影
    /// </summary>
    /// <param name="dsonString"></param>
    /// <returns></returns>
    public DsonValue? Project(string dsonString) {
        return Project(new DsonTextReader(DsonTextReaderSettings.Default, dsonString));
    }

    /**
     * 1.如果投影为Array，则投可投影顶层的Header，返回值必定为{@link DsonArray}
     * 2.如果投影为Object，则只返回第一个对象的投影，顶层Header被当做普通对象投影。
     */
    public DsonValue? Project(IDsonReader<string> reader) {
        if (root is DefaultNode defaultNode && defaultNode.arrayLike) {
            return new Matcher(reader, root).ProjectTopArray();
        } else {
            DsonType dsonType = reader.ReadDsonType();
            if (dsonType == DsonType.EndOfObject) {
                return null;
            }
            Matcher matcher = new Matcher(reader, root);
            return matcher.Project();
        }
    }

    readonly struct Matcher
    {
        readonly IDsonReader<string> reader;
        readonly Node node;

        internal Matcher(IDsonReader<string> reader, Node node) {
            this.reader = reader;
            this.node = node;
        }

        internal DsonValue Project() {
            DsonType currentDsonType = reader.CurrentDsonType;
            if (!node.TestType(currentDsonType)) {
                reader.SkipValue();
                // 上下文不匹配时返回期望的类型
                if (node is DefaultNode defaultNode) {
                    return defaultNode.arrayLike ? new DsonArray<string>(0) : new DsonObject<string>(0);
                }
                // 语义不清楚的情况下返回真实的类型
                if (currentDsonType == DsonType.Array) {
                    return new DsonArray<string>(0);
                }
                if (currentDsonType == DsonType.Header) {
                    return new DsonHeader<string>();
                }
                return new DsonObject<string>(0);
            }
            if (currentDsonType == DsonType.Array) {
                return ProjectArray();
            }
            if (currentDsonType == DsonType.Header) {
                return ProjectHeader();
            }
            return ProjectObject();
        }

        private static bool NeedMatcher(Node fieldNode) {
            return fieldNode.IsProjectNode() && !(fieldNode is SelectNode);
        }

        private DsonHeader<string> ProjectHeader() {
            DsonHeader<string> dsonObject = new DsonHeader<string>();
            DsonType dsonType;
            string name;
            DsonValue value;
            reader.ReadStartHeader();
            while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
                name = reader.ReadName();
                if (node.TestField(name)) {
                    Node fieldNode = node.GetFieldNode(name);
                    if (NeedMatcher(fieldNode)) {
                        value = new Matcher(reader, fieldNode).Project();
                    } else {
                        value = Dsons.ReadDsonValue(reader);
                    }
                    dsonObject[name] = value;
                } else {
                    reader.SkipValue();
                }
            }
            reader.ReadEndHeader();
            return dsonObject;
        }

        internal DsonObject<string> ProjectObject() {
            DsonObject<string> dsonObject = new DsonObject<string>();
            DsonType dsonType;
            string name;
            DsonValue value;
            reader.ReadStartObject();
            while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
                if (dsonType == DsonType.Header) {
                    if (node.TestHeader()) {
                        Dsons.ReadHeader(reader, dsonObject.Header);
                    } else {
                        reader.SkipValue();
                    }
                    if (node.RemainCount(dsonObject.Count) == 0) {
                        reader.SkipToEndOfObject(); // 不再继续读；header不在计数中，因此放header后
                        break;
                    }
                    continue;
                }

                name = reader.ReadName();
                if (node.TestField(name)) {
                    Node fieldNode = node.GetFieldNode(name);
                    if (NeedMatcher(fieldNode)) {
                        value = new Matcher(reader, fieldNode).Project();
                    } else {
                        value = Dsons.ReadDsonValue(reader);
                    }
                    dsonObject[name] = value;
                    if (node.RemainCount(dsonObject.Count) == 0) {
                        reader.SkipToEndOfObject();
                        break;
                    }
                } else {
                    reader.SkipValue();
                }
            }
            reader.ReadEndObject();
            return dsonObject;
        }

        internal DsonArray<string> ProjectArray() {
            DsonArray<string> dsonArray = new DsonArray<string>();
            DsonType dsonType;
            DsonValue value;
            int index = 0;
            reader.ReadStartArray();
            while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
                if (dsonType == DsonType.Header) {
                    if (node.TestHeader()) {
                        Dsons.ReadHeader(reader, dsonArray.Header);
                    } else {
                        reader.SkipValue();
                    }
                    if (node.RemainCount(dsonArray.Count) == 0) {
                        reader.SkipToEndOfObject(); // 不再继续读；header不在计数中，因此放header后
                        break;
                    }
                    continue;
                }

                if (node.TestElement(index++)) {
                    Node elemNode = node.GetElemNode();
                    if (NeedMatcher(elemNode)) {
                        value = new Matcher(reader, elemNode).Project();
                    } else {
                        value = Dsons.ReadDsonValue(reader);
                    }
                    dsonArray.Add(value);
                    if (node.RemainCount(dsonArray.Count) == 0) {
                        reader.SkipToEndOfObject();
                        break;
                    }
                } else {
                    reader.SkipValue();
                }
            }
            reader.ReadEndArray();
            return dsonArray;
        }

        internal DsonArray<string> ProjectTopArray() {
            DsonArray<string> dsonArray = new DsonArray<string>();
            DsonType dsonType;
            DsonValue value;
            int index = 0;
            while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
                if (dsonType == DsonType.Header) {
                    if (node.TestHeader()) {
                        Dsons.ReadHeader(reader, dsonArray.Header);
                    } else {
                        reader.SkipValue();
                    }
                    if (node.RemainCount(dsonArray.Count) == 0) {
                        break; // 不再继续读；header不在计数中，因此放header后
                    }
                    continue;
                }

                if (node.TestElement(index++)) {
                    Node elemNode = node.GetElemNode();
                    if (NeedMatcher(elemNode)) {
                        value = new Matcher(reader, elemNode).Project();
                    } else {
                        value = Dsons.ReadDsonValue(reader);
                    }
                    dsonArray.Add(value);
                    if (node.RemainCount(dsonArray.Count) == 0) {
                        break;
                    }
                } else {
                    reader.SkipValue();
                }
            }
            return dsonArray;
        }
    }

    // region node

    private static readonly Node DISCARD_NODE = new DiscardNode();
    private static readonly Node SELECT_NODE = new SelectNode();

    /**
     * Node表示预编译的节点
     * 1.不能总是解析用户的文本，因此需要提前编译缓存
     * 2.Node仅仅是保存编译后的数据，即包含Object的投影信息，也包含Array的投影信息。
     * 3.只有在真正执行投影时才知道数据的类型，因此Node不能直接运行
     * 4.node无需记录父子关系，Matcher记录即可。
     */
    private abstract class Node
    {
        /** 是否是需要投影的节点 */
        public bool IsProjectNode() {
            return this is not DiscardNode;
        }

        /** 测试节点类型是否匹配 */
        public abstract bool TestType(DsonType dsonType);

        /** 测试对象的header是否需要投影 */
        public abstract bool TestHeader();

        /** 测试对象的特定字段是否需要返回 */
        public abstract bool TestField(string key);

        /** 测试数组的特点下标元素是否需要返回 */
        public abstract bool TestElement(int index);

        /** 剩余需要投影的成员数量，-1表示未知 */
        public abstract int RemainCount(int current);

        /** 获取字段投影的Node信息 */
        public abstract Node GetFieldNode(string key);

        /** 获取数组元素投影的Node信息 */
        public abstract Node GetElemNode();
    }

    /** DefaultNode代表的是{k:v}构建的节点 */
    private class DefaultNode : Node
    {
        /** 匹配的上下文是否是数组类型 */
        internal readonly bool arrayLike;
        /** 是否投影header */
        readonly bool includeHeader;

        /** object的投影模式 */
        readonly SelectMode selectMode;
        /** 字段的投影信息 */
        readonly Dictionary<string, Node> fieldNodes;
        /** 投影的字段数 */
        readonly int selectCount;

        /** 数组切片信息 */
        readonly SliceSpec sliceSpec;
        /** 数组元素投影信息 */
        readonly Node elementNode;

        public DefaultNode(bool arrayLike, DsonObject<string> projectInfo) {
            this.arrayLike = arrayLike;
            projectInfo.TryGetValue(KEY_HEADER, out var header);
            this.includeHeader = IsTrue(header);
            // object 字段映射
            {
                fieldNodes = new Dictionary<string, Node>(projectInfo.Count);
                int count = 0;
                foreach (KeyValuePair<string, DsonValue> entry in projectInfo) {
                    string key = entry.Key;
                    if (ALL_SPECIAL_KEYS.Contains(key)) {
                        continue;
                    }
                    DsonValue value = entry.Value;
                    Node childNode = ParseNode(value);
                    fieldNodes[key] = childNode;
                    if (childNode.IsProjectNode()) {
                        count++;
                    }
                }
                this.selectCount = count;

                projectInfo.TryGetValue(KEY_ALL, out DsonValue allValue);
                if (IsTrue(allValue)) { // 指定$all的情况下直接进入反选模式
                    selectMode = SelectMode.Invert;
                } else if (fieldNodes.Count > 0 && count == 0) { // 指定了key，且所有key的value都是0
                    selectMode = SelectMode.Invert;
                } else {
                    selectMode = SelectMode.Normal;
                }
            }
            // array 映射
            {
                projectInfo.TryGetValue(KEY_SLICE, out DsonValue sliceValue);
                if (sliceValue == null) {
                    // 未声明slice的情况下，返回空数组
                    sliceSpec = SliceSpec.Empty;
                } else {
                    sliceSpec = ParseSliceSpec(sliceValue);
                }
                projectInfo.TryGetValue(KEY_ELEM, out DsonValue elemValue);
                if (elemValue == null) {
                    // 未声明elem的情况下，返回原始对象
                    elementNode = SELECT_NODE;
                } else {
                    elementNode = ParseNode(elemValue);
                }
            }
        }

        public override bool TestType(DsonType dsonType) {
            return arrayLike ? dsonType == DsonType.Array : dsonType.IsObjectLike();
        }

        public override bool TestHeader() {
            return includeHeader;
        }

        public override bool TestField(string key) {
            if (arrayLike) {
                return false;
            }
            fieldNodes.TryGetValue(key, out Node node);
            if (this.selectMode == SelectMode.Normal) {
                return node != null && node.IsProjectNode();
            }
            return node == null || node.IsProjectNode();
        }

        public override bool TestElement(int index) {
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

        public override int RemainCount(int current) {
            if (arrayLike) {
                if (sliceSpec.count == -1) {
                    return -1;
                }
                return Math.Max(0, sliceSpec.count - current);
            }
            return selectMode == SelectMode.Normal ? Math.Max(0, selectCount - current) : -1;
        }

        public override Node GetFieldNode(string key) {
            if (arrayLike) {
                return DISCARD_NODE;
            }
            if (fieldNodes.TryGetValue(key, out Node childNode)) {
                return childNode;
            }
            return selectMode == SelectMode.Normal ? DISCARD_NODE : SELECT_NODE;
        }

        public override Node GetElemNode() {
            if (!arrayLike) {
                return DISCARD_NODE;
            }
            return elementNode;
        }
    }

    /** 无法识别上下文类型的Node -- 比如：{}, {$header: 1} */
    private class UnknownContextNode : Node
    {
        readonly bool includeHeader;

        public UnknownContextNode(DsonObject<string> projectInfo) {
            projectInfo.TryGetValue(KEY_HEADER, out var header);
            includeHeader = IsTrue(header);
        }

        public override bool TestType(DsonType dsonType) {
            return true;
        }

        public override bool TestHeader() {
            return includeHeader;
        }

        public override bool TestField(string key) {
            return false;
        }

        public override bool TestElement(int index) {
            return false;
        }

        public override int RemainCount(int current) {
            return 0;
        }

        public override Node GetFieldNode(string key) {
            return DISCARD_NODE;
        }

        public override Node GetElemNode() {
            return DISCARD_NODE;
        }
    }

    /** 简单丢弃节点 -- value为0 */
    private class DiscardNode : Node
    {
        public override bool TestType(DsonType dsonType) {
            return false;
        }

        public override bool TestHeader() {
            return false;
        }

        public override bool TestField(string key) {
            return false;
        }

        public override bool TestElement(int index) {
            return false;
        }

        public override int RemainCount(int current) {
            return 0;
        }

        public override Node GetFieldNode(string key) {
            return this;
        }

        public override Node GetElemNode() {
            return this;
        }
    }

    /** 简单选择节点 -- value为1 */
    private class SelectNode : Node
    {
        public override bool TestType(DsonType dsonType) {
            return true;
        }

        public override bool TestHeader() {
            return false; // header默认被忽略
        }

        public override bool TestField(string key) {
            return true;
        }

        public override bool TestElement(int index) {
            return true;
        }

        public override int RemainCount(int current) {
            return -1;
        }

        public override Node GetFieldNode(string key) {
            return this;
        }

        public override Node GetElemNode() {
            return this;
        }
    }

    private static Node ParseNode(DsonValue childSpec) {
        if (childSpec == null) throw new ArgumentNullException(nameof(childSpec));
        if (childSpec is DsonBool dsonBool) { // true or false
            return dsonBool.Value ? SELECT_NODE : DISCARD_NODE;
        }
        if (childSpec is DsonNumber dsonNumber) { // 0 or 1
            return dsonNumber.IntValue == 1 ? SELECT_NODE : DISCARD_NODE;
        }
        DsonObject<string> childProjInfo = childSpec.AsObject();
        if (childProjInfo.Count == 0) { // {}
            return new UnknownContextNode(childProjInfo);
        }
        if (childProjInfo.Count == 1
            && childProjInfo.ContainsKey(KEY_HEADER)) { // {$header: 1}
            return new UnknownContextNode(childProjInfo);
        }
        foreach (string arrayKey in ARRAY_KEYS) {
            if (childProjInfo.ContainsKey(arrayKey)) { // {$slice: 1}
                return new DefaultNode(true, childProjInfo);
            }
        }
        // 默认为object上下文
        return new DefaultNode(false, childProjInfo);
    }

    private static bool IsTrue(DsonValue? dsonValue) {
        if (dsonValue == null) return false;
        if (dsonValue.DsonType == DsonType.Bool) {
            return dsonValue.AsBool();
        }
        if (dsonValue.IsNumber) {
            return dsonValue.AsDsonNumber().IntValue == 1;
        }
        return false;
    }

    private static SliceSpec ParseSliceSpec(DsonValue rangeValue) {
        if (rangeValue.IsNumber) {
            int skip = rangeValue.AsDsonNumber().IntValue;
            return new SliceSpec(skip);
        }
        DsonArray<string> array = rangeValue.AsArray();
        switch (array.Count) {
            case 0: {
                return SliceSpec.Empty;
            }
            case 1: {
                int skip = array[0].AsDsonNumber().IntValue;
                return new SliceSpec(skip);
            }
            case 2: {
                int skip = array[0].AsDsonNumber().IntValue;
                int count = array[1].AsDsonNumber().IntValue;
                return new SliceSpec(skip, count);
            }
            default: {
                throw new DsonIOException("invalid slice range: " + rangeValue);
            }
        }
    }

    /** object类型的投影模式 */
    enum SelectMode
    {
        /** 普通选择，选择给定的键 -- 投影信息为空 或 value包含至少一个1 */
        Normal,
        /** 反选，排除给定的键 -- 投影信息value全部为0 */
        Invert,
    }

    /** 数组切片范围 */
    readonly struct SliceSpec
    {
        internal static readonly SliceSpec Empty = new SliceSpec(0, 0);
        internal static readonly SliceSpec First = new SliceSpec(0, 1);
        internal static readonly SliceSpec Full = new SliceSpec(0, -1);

        internal readonly int skip;
        internal readonly int count;

        public SliceSpec(int skip) {
            this.skip = skip;
            this.count = -1;
        }

        public SliceSpec(int startIndex, int count) {
            this.skip = startIndex;
            this.count = count;
        }
    }

    // endregion
}
}