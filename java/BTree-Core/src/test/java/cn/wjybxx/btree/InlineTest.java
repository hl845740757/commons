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
import cn.wjybxx.btree.branch.Sequence;
import cn.wjybxx.btree.decorator.Repeat;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 内敛测试
 *
 * @author wjybxx
 * date - 2024/9/9
 */
public class InlineTest {

    private static final String successMessage = "success";

    @Test
    public void fireEventTest() {
        Task<Blackboard> branch = new Selector<>();
        EventAcceptor eventAcceptor = new EventAcceptor();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        // 顶层插入一个repeat，查看内联的修复是否正确
        branch.addChild(new Repeat<>(3));

        branch = branch.getChild(0);
        branch.addChild(new Selector<>());

        branch = branch.getChild(0);
        branch.addChild(new Selector<>());

        branch = branch.getChild(0);
        branch.addChild(new Sequence<>());

        branch = branch.getChild(0);
        branch.addChild(eventAcceptor);

        taskEntry.update(0); // 先启动

        // 测试事件是否直接到达末端 -- 这个似乎只能debug看调用栈
        String message = "message";
        taskEntry.onEvent(message);
        Assertions.assertEquals(message, eventAcceptor.eventObj);

        taskEntry.update(1); // debug查看心跳调用栈

        taskEntry.onEvent(successMessage);
        taskEntry.update(2); // debug查看心跳调用栈--查看内联修复过程
    }

    private static class EventAcceptor extends LeafTask<Blackboard> {

        public Object eventObj;

        @Override
        protected void execute() {
            if (getRunFrames() >= 10) {
                setSuccess();
            }
        }

        protected void onEventImpl(@Nonnull Object eventObj) {
            this.eventObj = eventObj;
            if (Objects.equals(eventObj, successMessage)) {
                setSuccess();
            }
        }
    }
}
