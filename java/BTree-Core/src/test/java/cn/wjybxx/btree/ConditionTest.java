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

package cn.wjybxx.btree;

import cn.wjybxx.btree.branch.Selector;
import cn.wjybxx.btree.branch.SelectorN;
import cn.wjybxx.btree.branch.Sequence;
import cn.wjybxx.btree.decorator.Inverter;
import cn.wjybxx.btree.leaf.Failure;
import cn.wjybxx.btree.leaf.Success;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * 由于我们对条件测试进行了专项优化，需要测试器正确性
 *
 * @author wjybxx
 * date - 2024/9/4
 */
public class ConditionTest {

    /** 测试需要覆盖成功的子节点数量 [0, 10] */
    private static final int childCount = 10;

    private static void initChildren(BranchTask<Blackboard> branch, int childCount, int successCount) {
        branch.removeAllChild();
        for (int i = 0; i < childCount; i++) {
            branch.addChild(new Success<>());
        }
        // 顺便测试inverter内联
        int failCount = childCount - successCount;
        for (int i = 0; i < failCount; i++) {
            switch (BtreeTestUtil.random.nextInt(3)) {
                case 0 -> branch.getChild(i).setFlags(Task.MASK_INVERTED_GUARD);
                case 1 -> branch.setChild(i, new Inverter<>(new Success<>()));
                case 2 -> branch.getChild(i).setGuard(new Failure<>());
                default -> throw new AssertionError();
            }
        }
        branch.shuffleChild(); // 打乱child
    }

    @Test
    void selectorTest() {
        Selector<Blackboard> branch = new Selector<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            initChildren(branch, childCount, expcted);
            taskEntry.test();

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
            initChildren(branch, childCount, expcted);
            taskEntry.test();

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
                initChildren(branch, childCount, real);
                taskEntry.test();

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
}
