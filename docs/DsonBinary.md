# Dson二进制流

Dson提供了两个版本的二进制格式，从整体上看他们是一样的，区别在于**一个使用number映射字段，一个使用string映射字段**。
使用number映射字段可以使编码后的包体更小，编解码性能也更好。

我们以object的编码为例介绍流的构成。

## number映射字段方案

  <pre>
   length1  [dsonType + wireType] +  [lnumber  +  idep] + [length2] + [subType] + [hasValue] + [data] ...
    4Bytes    5 bits     3 bits        n bits  3 bits     4 Bytes     1~5 Byte      1Byte      0~n Bytes
    总长度      1 Byte(unit8)             1~n Byte          int32      unit32
  </pre>

### length1区域

1. length1区域记录动态容器类结构\(Object、Array、Header)的数据总长度。
2. 为以方便截取数据，Length1区域固定Fixed32编码。

### 类型区域(DsonType+WireType)

1. 每一个Value都会写入其类型信息`DsonType`，数字还会记录其编码信息`WireType`。
2. DsonType 和 WireType 合并编码，共1个字节。
3. WireType分为：VARINT(0)、UINT(1)、SINT(2)、FIXED(3) -可参考谷歌protobuf。
4. int32和int64数字的编码类型会随着数字序列化，以确保对方正确的解码。
5. **WireType的比特位用于非数字类型时可以表达其它信息** -- 比如标记null字段。

### 字段编号区域(lnumber + idep)

1. Dson最初是为序列化而创建的，因此考虑过继承问题，Dson是支持继承的。
2. 字段的fullNumber由两部分构成，localNumber(本地编号)  + idep(继承深度)。
3. idep的取值范围\[0,7]，localNumber不建议超过8192，不可以为负
4. fullNumber为uint32类型。

### length2区域

1. Length2区域用于记录变长内置结构的长度；固定长度的属性类型没有length字段。
2. 数字没有length字段，数字通过wireType确定如何解码。
3. string的length是uint32变长编码。
4. Binary的length也是uint32变长编码。

### 子类型区域(subType)

1. subType用于记录Binary、ExtInt32、ExtInt64、ExtDouble、ExtString的Type。
2. subType统一使用`uint32`编码。

### 值存在标记(hasValue)

1. Ext扩展类型允许Value部分不存在；为保持编码简单，我们不记录在WireType的比特上。
2. hasValue用于记录Binary、ExtInt32、ExtInt64、ExtDouble、ExtString是否包含数据。

### wireType比特位的特殊使用

1. bool使用wireType记录了其值；wireType为1表示true，0表示false
2. Float和Double使用wireType记录了**后导全0的字节数**
    1. 浮点数的前16位总是写入
    2. 对于Float，前16位包含了7个有效数据位，就涵盖了大量的常用数；WireType的取值范围为\[0, 2]
    3. 对于Double，前16位包含了4个数据位，也包含了少许常用数据；WireType的取值范围为\[0, 6]
    4. 浮点数压缩算法的实际收益不理想，不过聊胜于无。
3. ptr/lptr使用wireType标记了namespace、type、policy是否存在，只有存在时才写入；localId总是写入。
    1. 001 用于标记namespace
    2. 010 用于标记type
    3. 100 用于标记policy
    4. 编码顺序为 localId、namespace、type、policy (可选字段后序列化)
    5. 其中 type 和 policy 使用unit32编码
4. datetime使用wireType存储了enables

### 编码详情

#### Binary编码

```
   output.writeUint32(binary.length);
   output.writeRawBytes(binary.data);
```

#### 指针(ptr)

1. 由于指针的使用量可能较大，我们在wireType上记录了namespace、type、policy是否有值的信息。

```
   output.writeString(dsonValue.localId);
   if (dsonValue.hasNamespcae) {
      output.writeString(dsonValue.namespace);
   }
   if (dsonValue.type != 0) {
      output.writeRawByte(dsonValue.type);
   }
   if (dsonValue.policy != 0) {
      output.writeRawByte(dsonValue.policy);
   }
```

#### 轻量指针(lptr)

1. 由于指针的使用量可能较大，我们在wireType上记录了namespace、type、policy是否有值的信息
2. localId 使用`Uint`编码，localId通常应该为正值。

```
   output.writeUInt64(dsonValue.localId);
   if (dsonValue.hasNamespcae) {
      output.writeString(dsonValue.namespace);
   }
   if (dsonValue.type != 0) {
      output.writeRawByte(dsonValue.type);
   }
   if (dsonValue.policy != 0) {
      output.writeRawByte(dsonValue.policy);
   }
```

#### DateTime

1. 我未对DateTime的编码做优化，简单按序写入。
2. offset使用`sint`编码，时区容易出现负数。
3. enables使用wireType比特位存储

```
   output.writeUint64(dateTime.getSeconds());
   output.writeUint32(dateTime.getNanos());
   output.writeSint32(dateTime.getOffset());
```

#### Timestamp

1. 我未对Timestamp的编码做优化，简单按序写入。

```
   output.writeUint64(dateTime.getSeconds());
   output.writeUint32(dateTime.getNanos());
```

### 其它

1. string采用utf8编码
2. header是object/array的一个匿名属性，*在object中是没有字段id但有类型的值*。

## string映射字段方案

  <p>
  文档型编码格式：
  <pre>
   length  [dsonType + wireType] +  [length + name] +  [length] + [subType] + [data] ...
   4Bytes    5 bits     3 bits          nBytes         4 Bytes    1~5 Byte   0~n Bytes
   数据长度     1 Byte(unit8)            string          int32      unit32
  </pre>

String映射字段其实和number映射字段的差别只是 fullNumber 变为了字符串类型的名字，而name按照普通的String值编码，
即：length采用uint32编码，data采用utf8编码。