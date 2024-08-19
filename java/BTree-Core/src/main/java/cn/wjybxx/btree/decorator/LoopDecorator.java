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
 * <p>
 * 注意：该模板类默认支持了尾递归优化，如果子类没有重写{@link #execute()}方法，
 * 那么在{@link #onChildCompleted(Task)}方法中还需要判断是否启用了尾递归优化，
 * 如果启用了尾递归优化，也需要调用{@link #template_execute()}方法。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class LoopDecorator<T> extends Decorator<T> {

    /** 最大循环次数，超过次数直接失败；大于0有效 */
    protected int maxLoop = -1;
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
        if (isTailRecursion()) {
            // 尾递归优化--普通循环代替递归
            final int reentryId = getReentryId();
            while (true) {
                Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
                if (inlinedRunningChild != null) {
                    template_runInlinedChild(inlinedRunningChild, inlineHelper, child);
                } else if (child.isRunning()) {
                    if (child.isActiveInHierarchy()) {
                        child.template_execute();
                    }
                } else {
                    curLoop++;
                    template_runChild(child);
                }
                if (checkCancel(reentryId)) { // 得出结果或被取消
                    return;
                }
                if (child.isRunning()) { // 子节点未结束
                    return;
                }
            }
        } else {
            Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
            if (inlinedRunningChild != null) {
                template_runInlinedChild(inlinedRunningChild, inlineHelper, child);
            } else if (child.isRunning()) {
                if (child.isActiveInHierarchy()) {
                    child.template_execute();
                }
            } else {
                curLoop++;
                template_runChild(child);
            }
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