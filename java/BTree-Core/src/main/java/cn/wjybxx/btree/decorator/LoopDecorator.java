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
package cn.wjybxx.btree.decorator;

import cn.wjybxx.btree.Decorator;
import cn.wjybxx.btree.Task;

/**
 * 循环节点抽象
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class LoopDecorator<T> extends Decorator<T> {

    /** 最大循环次数，超过次数直接失败；大于0有效 */
    protected int maxLoop = -1;
    /** 执行前+1，因此从1开始 */
    protected transient int curLoop = 0;

    public LoopDecorator() {
    }

    public LoopDecorator(Task<T> child) {
        super(child);
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        curLoop = 0;
    }

    @Override
    protected void execute() {
        Task<T> inlinedChild = inlineHelper.getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.template_executeInlined(inlineHelper, child);
        } else if (child.isRunning()) {
            child.template_execute(true);
        } else {
            curLoop++;
            template_startChild(child, true);
        }
    }

    /** 是否还有下一次循环 */
    protected boolean hasNextLoop() {
        return maxLoop <= 0 || curLoop < maxLoop;
    }

    public int getMaxLoop() {
        return maxLoop;
    }

    public void setMaxLoop(int maxLoop) {
        this.maxLoop = maxLoop;
    }
}