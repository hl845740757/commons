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
using NUnit.Framework;
using Wjybxx.BTree;
using Wjybxx.BTree.Decorator;
using Wjybxx.BTree.Leaf;
using Wjybxx.Commons.Ex;

namespace BTree.Tests;

public class DecoratorTest
{
    private static readonly Random random = new Random();
    private static int failedCount;
    private static int successCount;

    [SetUp]
    public void setUp() {
        failedCount = 0;
        successCount = 0;
    }

    private class CountRandom<T> : LeafTask<T> where T : class
    {
        private readonly bool isGuard;

        public CountRandom() {
            isGuard = false;
        }

        public CountRandom(bool isGuard) {
            this.isGuard = isGuard;
        }

        protected override void Execute() {
            if (!isGuard && RunFrames < 3 && random.Next(2) == 1) { // 随机等待
                return;
            }
            if (random.Next(2) == 1) {
                successCount++;
                SetSuccess();
            } else {
                failedCount++;
                SetFailed(TaskStatus.ERROR);
            }
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    #region repeat

    private const int REPEAT_COUNT = 10;

    private static Repeat<Blackboard> newRandomRepeat(int mode) {
        Repeat<Blackboard> repeat = new Repeat<Blackboard>();
        repeat.Required = REPEAT_COUNT;
        repeat.CountMode = mode;
        repeat.Child = new CountRandom<Blackboard>();
        return repeat;
    }

    [Test]
    public void repeatAlwaysTest() {
        Repeat<Blackboard> repeat = newRandomRepeat(RepeatMode.MODE_ALWAYS);
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(repeat);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(REPEAT_COUNT, successCount + failedCount);
    }

    [Test]
    public void repeatSuccessTest() {
        Repeat<Blackboard> repeat = newRandomRepeat(RepeatMode.MODE_ONLY_SUCCESS);
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(repeat);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(REPEAT_COUNT, successCount);
    }

    [Test]
    public void repeatFailTest() {
        Repeat<Blackboard> repeat = newRandomRepeat(RepeatMode.MODE_ONLY_FAILED);
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(repeat);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(REPEAT_COUNT, failedCount);
    }

    #endregion

    #region util

    [Repeat(5)]
    [Test]
    public void untilSuccessTest() {
        UntilSuccess<Blackboard> decorator = new UntilSuccess<Blackboard>();
        decorator.Child = new CountRandom<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(1, successCount);
    }

    [Repeat(5)]
    [Test]
    public void untilFailedTest() {
        UntilFail<Blackboard> decorator = new UntilFail<Blackboard>();
        decorator.Child = new CountRandom<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(1, failedCount);
    }

    [Test]
    public void untilCondTest() {
        UntilCond<Blackboard> decorator = new UntilCond<Blackboard>();
        decorator.Child = new Failure<Blackboard>(); // 子节点忽略
        decorator.Cond = new CountRandom<Blackboard>(true); // 条件成功则成功

        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(1, successCount);
    }

    #endregion

    /** OnlyOnce不重置的情况下，每次都返回之前的状态 */
    [Test]
    public void onlyOnceTest() {
        OnlyOnce<Blackboard> decorator = new OnlyOnce<Blackboard>();
        decorator.Child = new CountRandom<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);

        int status = taskEntry.Status;
        for (int i = 0; i < 10; i++) {
            BtreeTestUtil.untilCompleted(taskEntry);
            Assert.AreEqual(status, taskEntry.Status);
        }
        Assert.AreEqual(1, successCount + failedCount);
    }

    [Test]
    public void alwaysRunningTest() {
        AlwaysRunning<Blackboard> decorator = new AlwaysRunning<Blackboard>();
        decorator.Child = new CountRandom<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        Assert.Throws<InfiniteLoopException>(() => BtreeTestUtil.untilCompleted(taskEntry));

        Assert.IsTrue(taskEntry.IsRunning);
        Assert.AreEqual(1, successCount + failedCount);
    }
}