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

import cn.wjybxx.dson.DsonValue;
import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.annotations.DsonProperty;
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;
import it.unimi.dsi.fastutil.ints.Int2IntMap;
import it.unimi.dsi.fastutil.ints.Int2IntOpenHashMap;

import java.util.*;

/**
 * 编译之后，将 parent/common/target/generated-test-sources/test-annotations 设置为 test-resource 目录，
 * 就可以看见生成的代码是什么样的。
 * <p>
 * 这里为了减少代码，字段都定义为了public，避免getter/setter影响阅读
 *
 * @author wjybxx
 * date 2023/4/7
 */
@DsonSerializable
public class CodecBeanExample {

    @DsonProperty(wireType = WireType.UINT, name = "_age")
    public int age;
    public String name;

    public Map<Integer, String> age2NameMap;
    public Map<Sex, String> sex2NameMap1;
    public EnumMap<Sex, String> sex2NameMap2;
    @DsonProperty(impl = EnumMap.class)
    public Map<Sex, String> sex2NameMap3;

    public Set<Sex> sexSet1;
    public EnumSet<Sex> sexSet2;
    @DsonProperty(impl = EnumSet.class)
    public Set<Sex> sexSet3;

    @DsonProperty(objectStyle = ObjectStyle.FLOW)
    public List<String> stringList1;
    public ArrayList<String> stringList2;
    @DsonProperty(impl = LinkedList.class)
    public List<String> stringList3;

    public Int2IntOpenHashMap currencyMap1;
    @DsonProperty(impl = Int2IntOpenHashMap.class)
    public Int2IntMap currencyMap2;

    @DsonProperty(writeProxy = "writeCustom", readProxy = "readCustom")
    public Object custom;

    // 测试非标准的getter/seter
    private boolean boolValue;
    private String sV;

    // 测试最新apt 递归解析TypeInfo
    // 测试嵌套泛型apt解析
    public Map<String, Map<String, ObjectStyle>> nestedMap;
    // 测试泛型擦除，应当解析为 DsonValue
    public Map<String, ? extends DsonValue> nestedMap2;
    // 测试泛型擦除，应当解析为 Object
    public Map<String, ? super DsonValue> nestedMap3;
    public Map<String, ?> nestedMap4;
    // 测试数组Map -- 泛型参数应当被解析
    public Map<String, ObjectStyle>[] arrayMap;
    public Map<String, ObjectStyle>[][] arrayMap2;

    public CodecBeanExample() {
    }


    // region

    public void writeCustom(DsonObjectWriter writer, String name) {

    }

    public void readCustom(DsonObjectReader reader, String name) {

    }

    public void beforeEncode(ConverterOptions options) {

    }

    public void writeObject(DsonObjectWriter writer) {

    }

    /** 生成代码自动调用 */
    public static CodecBeanExample newInstance(DsonObjectReader reader, TypeInfo encoderType) {
        return new CodecBeanExample();
    }

    public void readObject(DsonObjectReader reader) {

    }

    public void afterDecode(ConverterOptions options) {
        if (age < 1) throw new IllegalStateException();
    }

    // endregion

    // REGION 非标准getter/setter

    /** 标准getter为:{@code isBoolValue} */
    public boolean getBoolValue() {
        return boolValue;
    }

    public void setBoolValue(boolean boolValue) {
        this.boolValue = boolValue;
    }

    /** 标准getter为：{@code getsV} */
    public String getSV() {
        return sV;
    }

    /** 标准setter为：{@code setsV} */
    public void setSV(String sV) {
        this.sV = sV;
    }
    // ENDREGION

}