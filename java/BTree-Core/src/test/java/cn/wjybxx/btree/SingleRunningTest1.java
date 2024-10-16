/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree;

import cn.wjybxx.btree.branch.*;
import cn.wjybxx.btree.leaf.Failure;
import cn.wjybxx.btree.leaf.SimpleRandom;
import cn.wjybxx.btree.leaf.Success;
import cn.wjybxx.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
public class SingleRunningTest1 {

    /** 测试需要覆盖成功的子节点数量 [0, 5] */
    private static final int childCount = 5;

    @Test
    void selectorTest() {
        Selector<Blackboard> branch = new Selector<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            BtreeTestUtil.initChildren(branch, childCount, expcted);
            BtreeTestUtil.untilCompleted(taskEntry);

            if (expcted > 0) {
                Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
            } else {
                Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
            }
        }
    }

    @Test
    void sequenceTest() {
        Sequence<Blackboard> branch = new Sequence<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            BtreeTestUtil.initChildren(branch, childCount, expcted);
            BtreeTestUtil.untilCompleted(taskEntry);

            if (expcted < childCount) {
                Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
            } else {
                Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
            }
        }
    }

    @Test
    void selectorNTest() {
        SelectorN<Blackboard> branch = new SelectorN<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount + 1; expcted++) { // 期望成功的数量，需要包含边界外
            branch.setRequired(expcted);
            for (int real = 0; real <= childCount; real++) { // 真正成功的数量
                BtreeTestUtil.initChildren(branch, childCount, real);
                BtreeTestUtil.untilCompleted(taskEntry);

                if (real >= expcted) {
                    Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
                } else {
                    Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
                }

                if (expcted >= childCount) { // 所有子节点完成
                    Assertions.assertEquals(childCount, BtreeTestUtil.completedCount(branch));
                }
            }
        }
    }

    /** 由于可能未命中分支，因此需要循环多次 */
    @RepeatedTest(5)
    void switchTest() {
        Switch<Blackboard> branch = new Switch<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.addChild(new WaitFrame<Blackboard>().setGuard(new SimpleRandom<>(0.3f)));
        branch.addChild(new Success<Blackboard>().setGuard(new SimpleRandom<>(0.4f)));
        branch.addChild(new Failure<Blackboard>().setGuard(new SimpleRandom<>(0.5f)));
        BtreeTestUtil.untilCompleted(taskEntry);

        int runningIndex = branch.getRunningIndex();
        if (runningIndex < 0) {
            Assertions.assertTrue(taskEntry.isFailed());
        } else {
            Task<Blackboard> runChild = branch.getChild(runningIndex);
            Assertions.assertEquals(taskEntry.getStatus(), runChild.getStatus());
        }
    }

    @Test
    void foreachTest() {
        Foreach<Blackboard> branch = new Foreach<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.addChild(new WaitFrame<Blackboard>().setGuard(new SimpleRandom<>(0.3f)));
        branch.addChild(new Success<Blackboard>().setGuard(new SimpleRandom<>(0.4f)));
        branch.addChild(new Failure<Blackboard>().setGuard(new SimpleRandom<>(0.5f)));
        BtreeTestUtil.untilCompleted(taskEntry);

        Assertions.assertTrue(taskEntry.isSucceeded());
    }

    @Test
    void activeSelectorTest() {
        ActiveSelector<Blackboard> branch = new ActiveSelector<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.addChild(new WaitFrame<Blackboard>(10).setGuard(new FailAtFrame<>(1)));
        branch.addChild(new WaitFrame<Blackboard>(10).setGuard(new FailAtFrame<>(2)));
        branch.addChild(new WaitFrame<Blackboard>(10).setGuard(new FailAtFrame<>(100))); // 成功

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded());

        Assertions.assertTrue(branch.getChild(0).isCancelled());
        Assertions.assertTrue(branch.getChild(1).isCancelled());
        Assertions.assertTrue(branch.getChild(2).isSucceeded());
    }

    /** 在给定帧返回失败 */
    private static class FailAtFrame<T> extends LeafTask<T> {

        final int frame;

        private FailAtFrame(int frame) {
            this.frame = frame;
        }

        @Override
        protected void execute() {
            if (taskEntry.getCurFrame() >= frame) {
                setFailed(TaskStatus.ERROR);
            } else {
                setSuccess();
            }
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }

}