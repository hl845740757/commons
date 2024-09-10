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

import cn.wjybxx.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2024/8/20
 */
public class ActiveTest {

    /**
     * waitframe本应该在第5帧完成，但我们暂停了其心跳，在第9帧后启用心跳，第10帧就完成
     */
    @Test
    void testWaitFrame() {
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(new WaitFrame<>(5));

        int expectedFrames = 10;
        BtreeTestUtil.untilCompleted(taskEntry, frame -> {
            if (frame == 0) {
                taskEntry.setActive(false);
            }
            if (frame == expectedFrames - 1) {
                taskEntry.setActive(true);
            }
        });
        Assertions.assertEquals(10, taskEntry.getRootTask().getRunFrames());
    }

    /** 测试active为false的情况下在第9帧取消 */
    @Test
    void testCancel() {
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(new WaitFrame<>(5));

        int expectedFrames = 10;
        BtreeTestUtil.untilCompleted(taskEntry, frame -> {
            if (frame == 0) {
                taskEntry.setActive(false);
            }
            if (frame == expectedFrames - 1) {
                taskEntry.getCancelToken().cancel(1);
                taskEntry.setActive(true);
            }
        });
        Assertions.assertTrue(taskEntry.isCancelled());
        Assertions.assertEquals(10, taskEntry.getRootTask().getRunFrames());
    }
}
