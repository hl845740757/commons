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

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.ex.InfiniteLoopException;
import cn.wjybxx.btree.decorator.Inverter;
import cn.wjybxx.btree.leaf.WaitFrame;

import java.util.Random;
import java.util.function.IntConsumer;
import java.util.random.RandomGenerator;

/**
 * @author wjybxx
 * date - 2023/12/3
 */
class BtreeTestUtil {

    static final RandomGenerator random = new Random();

    public static TaskEntry<Blackboard> newTaskEntry() {
        return new TaskEntry<>("Main", null, new Blackboard(), null, TreeLoader.nullLoader());
    }

    public static TaskEntry<Blackboard> newTaskEntry(Task<Blackboard> root) {
        return new TaskEntry<>("Main", root, new Blackboard(), null, TreeLoader.nullLoader());
    }

    public static void untilCompleted(TaskEntry<?> entry) {
        for (int idx = 0; idx < 200; idx++) { // 避免死循环
            if (MathCommon.isEven(idx)) {
                entry.update(idx);
            } else {
                entry.updateInlined(idx);
            }
            if (entry.isCompleted()) return;
        }
        throw new InfiniteLoopException();
    }

    /**
     * @param entry       任务入口
     * @param frameAction 帧回调，初始帧号0；在task执行后调用
     */
    public static void untilCompleted(TaskEntry<?> entry, IntConsumer frameAction) {
        for (int idx = 0; idx < 200; idx++) { // 避免死循环
            entry.update(idx);
            frameAction.accept(idx);
            if (entry.isCompleted()) return;
        }
        throw new InfiniteLoopException();
    }

    /** 需要注意！直接遍历子节点，可能统计到上次的执行结果 */
    public static int completedCount(Task<?> ctrl) {
        int count = 0;
        int childCount = ctrl.getChildCount();
        for (int i = 0; i < childCount; i++) {
            if (ctrl.getChild(i).isCompleted()) count++;
        }
        return count;
    }

    public static int succeededCount(Task<?> ctrl) {
        int count = 0;
        int childCount = ctrl.getChildCount();
        for (int i = 0; i < childCount; i++) {
            if (ctrl.getChild(i).isSucceeded()) count++;
        }
        return count;
    }

    public static int failedCount(Task<?> ctrl) {
        int count = 0;
        int childCount = ctrl.getChildCount();
        for (int i = 0; i < childCount; i++) {
            if (ctrl.getChild(i).isFailed()) count++;
        }
        return count;
    }

    /**
     * @param childCount   子节点数量
     * @param successCount 期望成功的子节点数量
     */
    public static void initChildren(BranchTask<Blackboard> branch, int childCount, int successCount) {
        branch.removeAllChild();
        // 不能过于简单的成功或失败，否则可能无法覆盖所有情况
        for (int i = 0; i < successCount; i++) {
            branch.addChild(new WaitFrame<>(random.nextInt(0, 3)));
        }
        for (int i = successCount; i < childCount; i++) {
            branch.addChild(new Inverter<>(new WaitFrame<>(random.nextInt(0, 3))));
        }
        branch.shuffleChild(); // 打乱child
    }

}