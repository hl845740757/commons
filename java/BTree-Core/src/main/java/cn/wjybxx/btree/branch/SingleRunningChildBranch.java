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
package cn.wjybxx.btree.branch;

import cn.wjybxx.btree.BranchTask;
import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskInlineHelper;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;

/**
 * 非并行分支节点抽象(最多只有一个运行中的子节点)
 * <p>
 * 注意：该模板类默认支持了尾递归优化，如果子类没有重写{@link #execute()}方法，
 * 那么在{@link #onChildCompleted(Task)}方法中还需要判断是否启用了尾递归优化，
 * 如果启用了尾递归优化，也需要调用{@link #template_execute(boolean)}方法。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class SingleRunningChildBranch<T> extends BranchTask<T> {

    /** 运行中的子节点索引 -- index信息总是准确的 */
    protected transient int runningIndex = -1;
    /** 运行中的子节点 */
    protected transient Task<T> runningChild = null;
    /**
     * 被内联运行的子节点
     * 1.该字段定义在这里是为了减少抽象层次，该类并不提供功能。
     * 2.子类要支持实现内联优化时，应当在{@link #onChildRunning(Task)}和{@link #onChildCompleted(Task)}维护字段引用。
     */
    protected final transient TaskInlineHelper<T> inlineHelper = new TaskInlineHelper<>();

    public SingleRunningChildBranch() {
    }

    public SingleRunningChildBranch(List<Task<T>> children) {
        super(children);
    }

    public SingleRunningChildBranch(Task<T> first, @Nullable Task<T> second) {
        super(first, second);
    }

    // region open

    /** 允许外部在结束后查询 */
    public final int getRunningIndex() {
        return runningIndex;
    }

    /** 获取运行中的子节点 */
    public final Task<T> getRunningChild() {
        return runningChild;
    }

    public final TaskInlineHelper<T> getInlineHelper() {
        return inlineHelper;
    }

    /** 是否所有子节点已进入完成状态 */
    public boolean isAllChildCompleted() {
        return runningIndex + 1 >= children.size();
    }

    /** 进入完成状态的子节点数量 */
    public int getCompletedCount() {
        return runningIndex + 1;
    }

    /** 成功的子节点数量 */
    public int getSucceededCount() {
        int r = 0;
        for (int i = 0; i < runningIndex; i++) {
            if (children.get(i).isSucceeded()) r++;
        }
        return r;
    }

    // endregion

    // region logic
    @Override
    public void resetForRestart() {
        super.resetForRestart();
        runningIndex = -1;
        runningChild = null;
        inlineHelper.stopInline();
    }

    /** 模板类不重写enter方法，只有数据初始化逻辑 */
    @Override
    protected void beforeEnter() {
        // 这里不调用super是安全的
        runningIndex = -1;
        runningChild = null;
//        inlineHelper.stopInline();
    }

    @Override
    protected void exit() {
        // index不立即重置，允许返回后查询
        runningChild = null;
        inlineHelper.stopInline();
    }

    @Override
    protected void stopRunningChildren() {
        // 停止需要从上层开始
        Task.stop(runningChild);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        Task<T> inlinedChild = inlineHelper.getInlinedRunningChild();
        if (inlinedChild != null) {
            inlinedChild.onEvent(event);
        } else if (runningChild != null) {
            runningChild.onEvent(event);
        }
    }

    @Override
    protected void execute() {
        if (isTailRecursion()) {
            // 尾递归优化--普通循环代替递归
            final int reentryId = getReentryId();
            while (true) {
                Task<T> runningChild = this.runningChild;
                if (runningChild == null) {
                    this.runningChild = runningChild = nextChild();
                    template_runChild(runningChild);
                } else {
                    Task<T> inlinedChild = inlineHelper.getInlinedRunningChild();
                    if (inlinedChild != null) {
                        template_runInlinedChild(inlinedChild, inlineHelper, runningChild);
                    } else if (runningChild.isRunning()) {
                        runningChild.template_execute(true);
                    } else {
                        template_runChild(runningChild);
                    }
                }
                if (checkCancel(reentryId)) { // 得出结果或被取消
                    return;
                }
                if (runningChild.isRunning()) { // 子节点未结束
                    return;
                }
            }
        } else {
            // 普通事件驱动模式
            Task<T> runningChild = this.runningChild;
            if (runningChild == null) {
                this.runningChild = runningChild = nextChild();
                template_runChild(runningChild);
            } else {
                Task<T> inlinedChild = inlineHelper.getInlinedRunningChild();
                if (inlinedChild != null) {
                    template_runInlinedChild(inlinedChild, inlineHelper, runningChild);
                } else if (runningChild.isRunning()) {
                    runningChild.template_execute(true);
                } else {
                    template_runChild(runningChild);
                }
            }
        }
    }

    protected Task<T> nextChild() {
        // 避免状态错误的情况下修改了index
        int nextIndex = runningIndex + 1;
        if (nextIndex < children.size()) {
            runningIndex = nextIndex;
            return children.get(nextIndex);
        }
        throw new IllegalStateException(illegalStateMsg());
    }

    protected final String illegalStateMsg() {
        return "numChildren: %d, currentIndex: %d".formatted(children.size(), runningIndex);
    }

    /** 子类如果支持内联，则重写该方法 */
    @Override
    protected void onChildRunning(Task<T> child) {
        runningChild = child; // 子类可能未赋值
    }

    /**
     * 子类的实现模板：
     * <pre>{@code
     *
     *  protected void onChildCompleted(Task child) {
     *      runningChild = null;
     *      inlinedHolder.reset();
     *      // 尝试计算结果（记得处理取消）
     *      ...
     *      // 如果未得出结果
     *      if (!isExecuting()) {
     *          template_execute();
     *      }
     *  }
     * }</pre>
     * ps: 推荐子类重复编码避免调用super
     */
    @Override
    protected void onChildCompleted(Task<T> child) {
        runningChild = null;
        inlineHelper.stopInline();
    }

    // endregion

}