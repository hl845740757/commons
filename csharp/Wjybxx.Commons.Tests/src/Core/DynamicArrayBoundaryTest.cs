#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using System.Collections.Generic;
using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

/// <summary>
/// DynamicArray的边界测试
/// (主要覆盖len为64整数倍时的word边界，以及nullFactor>1不主动压缩的场景)
///
/// 注意：探测mask损坏必须构造elementCount &lt; len的状态，
/// 否则ContainsNull/IndexOf(null)会因elementCount==len而短路返回，掩盖问题。
/// </summary>
public class DynamicArrayBoundaryTest
{
    #region null-index

    /// <summary>
    /// len为64整数倍时，LastIndexOf(null)应返回真实的null下标
    /// </summary>
    [TestCase(64)]
    [TestCase(128)]
    [TestCase(192)]
    public void TestLastNullIndexAtWordBoundary(int size) {
        DynamicArray<string> arr = new(size + 16, 2.0f); // nullFactor>1，不主动压缩
        for (int i = 0; i < size; i++) arr.Add("v" + i);
        Assert.AreEqual(size, arr.Length);

        arr.Set(10, null);
        Assert.AreEqual(size - 1, arr.ElementCount);
        Assert.AreEqual(10, arr.IndexOf(null));
        Assert.AreEqual(10, arr.LastIndexOf(null));
    }

    /// <summary>
    /// len为64整数倍时，删除元素触发的压缩不应越界
    /// </summary>
    [TestCase(64)]
    [TestCase(128)]
    public void TestCompressAtWordBoundary(int size) {
        DynamicArray<string> arr = new(size + 16, 2.0f);
        for (int i = 0; i < size; i++) arr.Add("v" + i);
        arr.Set(10, null);

        arr.Compress(true);
        Assert.AreEqual(size - 1, arr.Length);
        Assert.AreEqual(size - 1, arr.ElementCount);
        Assert.IsFalse(arr.ContainsNull);
        Assert.AreEqual("v11", arr[10]);
        Assert.AreEqual("v" + (size - 1), arr[size - 2]);
    }

    /// <summary>
    /// 默认nullFactor=0时，len恰为64整数倍时Remove会立即压缩
    /// </summary>
    [TestCase(64)]
    [TestCase(128)]
    public void TestRemoveAtWordBoundaryAutoCompress(int size) {
        DynamicArray<string> arr = new(); // nullFactor=0，总是压缩
        for (int i = 0; i < size; i++) arr.Add("v" + i);
        Assert.AreEqual(size, arr.Length);

        arr.Remove("v10");
        Assert.AreEqual(size - 1, arr.Length);
        Assert.AreEqual("v11", arr[10]);
    }

    #endregion

    #region insert-bit

    /// <summary>
    /// len为64整数倍时Insert，需将最高位进位到下一个word。
    /// 若进位丢失，mask会误报一个不存在的null（幽灵null）。
    /// </summary>
    [TestCase(64)]
    [TestCase(128)]
    [TestCase(192)]
    public void TestInsertCarryAcrossWordBoundary(int size) {
        DynamicArray<string> arr = new(size + 16, 2.0f);
        for (int i = 0; i < size; i++) arr.Add("v" + i);

        arr.Insert(0, "X"); // len: size -> size+1，原index(size-1)移到index(size)
        Assert.AreEqual(size + 1, arr.Length);
        Assert.AreEqual("X", arr[0]);
        Assert.AreEqual("v" + (size - 1), arr[size]);

        // 构造唯一的真实null，使elementCount<len，避免短路掩盖mask损坏
        arr.Set(10, null);
        Assert.AreEqual(size, arr.ElementCount);
        Assert.AreEqual(10, arr.IndexOf(null));
        Assert.AreEqual(10, arr.LastIndexOf(null));
    }

    /// <summary>
    /// Insert后压缩：mask损坏会导致真实元素被当作空洞覆盖，造成数据丢失
    /// </summary>
    [TestCase(64)]
    [TestCase(128)]
    public void TestCompressAfterInsertAtWordBoundary(int size) {
        DynamicArray<string> arr = new(size + 16, 2.0f);
        for (int i = 0; i < size; i++) arr.Add("v" + i);
        arr.Insert(0, "X");
        arr.Set(10, null); // 唯一null

        arr.Compress(true);
        Assert.AreEqual(size, arr.Length);
        Assert.AreEqual(size, arr.ElementCount);

        List<string> list = arr.ToList();
        Assert.AreEqual(size, list.Count);
        Assert.AreEqual("X", list[0]);
        Assert.AreEqual("v" + (size - 1), list[size - 1], "尾元素不应丢失");
        CollectionAssert.DoesNotContain(list, null);
    }

    /// <summary>
    /// 逐个size扫描Insert后的mask完整性
    /// </summary>
    [Test]
    public void TestInsertMaskIntegrityAcrossSizes() {
        for (int size = 4; size <= 200; size++) {
            DynamicArray<string> arr = new(size + 16, 2.0f);
            for (int i = 0; i < size; i++) arr.Add("v" + i);
            arr.Insert(0, "X");

            // 制造唯一真实null
            arr.Set(3, null);
            Assert.AreEqual(3, arr.IndexOf(null), $"size={size}");
            Assert.AreEqual(3, arr.LastIndexOf(null), $"size={size} 进位丢失");
            Assert.AreEqual("v" + (size - 1), arr[size], $"size={size} 尾元素");
        }
    }

    /// <summary>
    /// Insert到中间位置，元素顺序与mask均需正确
    /// </summary>
    [Test]
    public void TestInsertMiddleAcrossBoundary() {
        for (int size = 60; size <= 132; size++) {
            DynamicArray<string> arr = new(size + 16, 2.0f);
            for (int i = 0; i < size; i++) arr.Add("v" + i);

            arr.Insert(30, "X");
            Assert.AreEqual(size + 1, arr.ElementCount, $"size={size}");
            Assert.IsFalse(arr.ContainsNull, $"size={size} 不应有幽灵null");
            Assert.AreEqual("X", arr[30], $"size={size}");
            Assert.AreEqual("v30", arr[31], $"size={size}");
            Assert.AreEqual("v" + (size - 1), arr[size], $"size={size}");
        }
    }

    #endregion

    #region clear

    /// <summary>
    /// 所有元素均为null时（未压缩），Clear仍应重置Length
    /// </summary>
    [Test]
    public void TestClearWhenAllNullResetsLength() {
        DynamicArray<string> arr = new(8, 2.0f);
        for (int i = 0; i < 5; i++) arr.Add("v" + i);
        for (int i = 0; i < 5; i++) arr.Set(i, null);
        Assert.AreEqual(0, arr.ElementCount);
        Assert.AreEqual(5, arr.Length);

        arr.Clear();
        Assert.AreEqual(0, arr.Length);
        Assert.AreEqual(0, arr.ElementCount);
    }

    /// <summary>
    /// IndexedDynamicArray：全null时Clear应重置Length，且新元素落在下标0
    /// </summary>
    [Test]
    public void TestIndexedClearWhenAllNull() {
        IndexedDynamicArray<Idx> arr = new(H.Inst, 8, 2.0f);
        for (int i = 0; i < 5; i++) arr.Add(new Idx());
        for (int i = 0; i < 5; i++) arr.Set(i, null);

        arr.Clear();
        Assert.AreEqual(0, arr.Length);

        Idx fresh = new Idx();
        arr.Add(fresh);
        Assert.AreEqual(0, fresh.qIndex, "Clear后首个元素应落在下标0");
        Assert.IsFalse(arr.ContainsNull, "Clear后不应残留null空洞");
    }

    /// <summary>
    /// SmallDynamicArray：全null时Clear应重置Length
    /// </summary>
    [Test]
    public void TestSmallClearWhenAllNull() {
        SmallDynamicArray<string> arr = new(8, 2.0f);
        for (int i = 0; i < 5; i++) arr.Add("v" + i);
        for (int i = 0; i < 5; i++) arr.Set(i, null);

        arr.Clear();
        Assert.AreEqual(0, arr.Length);
    }

    /// <summary>
    /// 反复Clear复用不应导致Length单调增长
    /// </summary>
    [Test]
    public void TestRepeatedClearNoLeak() {
        DynamicArray<string> arr = new(8, 2.0f);
        IndexedDynamicArray<Idx> iarr = new(H.Inst, 8, 2.0f);
        for (int round = 0; round < 8; round++) {
            for (int i = 0; i < 5; i++) {
                arr.Add("v" + i);
                iarr.Add(new Idx());
            }
            for (int i = 0; i < arr.Length; i++) arr.Set(i, null);
            for (int i = 0; i < iarr.Length; i++) iarr.Set(i, null);
            arr.Clear();
            iarr.Clear();
            Assert.AreEqual(0, arr.Length, $"round={round}");
            Assert.AreEqual(0, iarr.Length, $"round={round}");
        }
    }

    /// <summary>
    /// Clear后复用：mask需彻底清零
    /// </summary>
    [Test]
    public void TestClearThenReuseAtWordBoundary() {
        DynamicArray<string> arr = new(64, 2.0f);
        for (int i = 0; i < 64; i++) arr.Add("v" + i);
        arr.Clear();
        Assert.AreEqual(0, arr.Length);

        for (int i = 0; i < 64; i++) arr.Add("x" + i);
        Assert.AreEqual(64, arr.ElementCount);
        Assert.IsFalse(arr.ContainsNull);
        Assert.AreEqual(-1, arr.IndexOf(null));

        arr.Set(10, null);
        Assert.AreEqual(10, arr.LastIndexOf(null));
    }

    #endregion

    #region ctor

    /// <summary>
    /// SmallDynamicArray的容量上限为64，构造时即应校验
    /// (否则elementsMask只有64位，越界位会回绕污染低位)
    /// </summary>
    [Test]
    public void TestSmallCtorCapacityValidation() {
        Assert.Catch(() => new SmallDynamicArray<string>(65));
        Assert.Catch(() => new SmallDynamicArray<string>(100));
        Assert.Catch(() => new SmallDynamicArray<string>(-1));
        Assert.DoesNotThrow(() => new SmallDynamicArray<string>(0));
        Assert.DoesNotThrow(() => new SmallDynamicArray<string>(64));
    }

    /// <summary>
    /// initCapacity为0时，首次Add不应越界
    /// </summary>
    [Test]
    public void TestZeroInitCapacity() {
        DynamicArray<string> arr = new(0);
        arr.Add("a");
        Assert.AreEqual(1, arr.Length);
        Assert.AreEqual("a", arr[0]);

        IndexedDynamicArray<Idx> iarr = new(H.Inst, 0);
        Idx e = new Idx();
        iarr.Add(e);
        Assert.AreEqual(1, iarr.Length);
        Assert.AreEqual(0, e.qIndex);

        SmallDynamicArray<string> sarr = new(0);
        sarr.Add("a");
        Assert.AreEqual(1, sarr.Length);
    }

    #endregion

    #region helper

    private class H : IIndexedElementHelper<Idx>
    {
        internal static readonly H Inst = new H();
        public int CollectionIndex(object collection, Idx element) => element.qIndex;
        public void CollectionIndex(object collection, Idx element, int index) => element.qIndex = index;
    }

    private class Idx
    {
        internal int qIndex = -1;
    }

    #endregion
}
