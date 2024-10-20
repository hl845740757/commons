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
package cn.wjybxx.btree.fsm;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.btree.Decorator;
import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskStatus;
import cn.wjybxx.btree.branch.Join;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 状态机节点
 * ps:以我的经验来看，状态机是最重要的节点，{@link Join}则是仅次于状态机的节点 -- 不能以使用数量而定。
 *
 * @author wjybxx
 * date - 2023/12/1
 */
public class StateMachineTask<T> extends Decorator<T> {

    /** 状态机名字 */
    private String name;
    /** 初始状态 */
    protected Task<T> initState;
    /** 初始状态的属性 */
    protected Object initStateProps;

    /** 待切换的状态，主要用于支持当前状态退出后再切换 */
    protected transient Task<T> tempNextState;
    /** 默认不序列化 */
    protected transient StateMachineHandler<T> handler = StateMachineHandlers.defaultHandler();

    // region api

    /** 获取当前状态 */
    public final Task<T> getCurState() {
        return child;
    }

    /** 获取临时的下一个状态 */
    public final Task<T> getNextState() {
        return tempNextState;
    }

    /** 丢弃未切换的临时状态 */
    public final Task<T> discardNextState() {
        Task<T> r = tempNextState;
        if (r != null) tempNextState = null;
        return r;
    }

    /**
     * 撤销到前一个状态
     *
     * @return 如果有前一个状态则返回true
     */
    public final boolean undoChangeState() {
        return undoChangeState(ChangeStateArgs.UNDO);
    }

    /**
     * 撤销到前一个状态
     *
     * @return 如果有前一个状态则返回true
     */
    public boolean undoChangeState(ChangeStateArgs changeStateArgs) {
        return false;
    }

    /**
     * 重新进入到下一个状态
     *
     * @return 如果有下一个状态则返回true
     */
    public final boolean redoChangeState() {
        return redoChangeState(ChangeStateArgs.REDO);
    }

    /**
     * 重新进入到下一个状态
     *
     * @return 如果有下一个状态则返回true
     */
    public boolean redoChangeState(ChangeStateArgs changeStateArgs) {
        return false;
    }

    /** 切换状态 -- 如果状态机处于运行中，则立即切换 */
    public final void changeState(Task<T> nextState) {
        changeState(nextState, ChangeStateArgs.PLAIN);
    }

    /**
     * 切换状态 -- 如果状态机处于运行中，则立即切换
     *
     * @param curStateResult 当前状态的结果
     */
    public final void changeState(Task<T> nextState, int curStateResult) {
        ChangeStateArgs changeStateArgs = switch (curStateResult) {
            case TaskStatus.SUCCESS -> ChangeStateArgs.PLAIN_SUCCESS;
            case TaskStatus.CANCELLED -> ChangeStateArgs.PLAIN_CANCELLED;
            case TaskStatus.ERROR -> ChangeStateArgs.PLAIN_ERROR;
            default -> ChangeStateArgs.PLAIN.withArg(curStateResult);
        };
        changeState(nextState, changeStateArgs);
    }

    /***
     * 切换状态
     * 1.如果当前有一个待切换的状态，则会被悄悄丢弃(todo 可以增加一个通知)
     * 2.无论何种模式，在当前状态进入完成状态时一定会触发
     * 3.如果状态机未运行，则仅仅保存在那里，等待下次运行的时候执行
     * 4.关于如何避免当前状态被取消，可参考{@link ChangeStateTask}
     *
     * @param nextState 要进入的下一个状态
     * @param changeStateArgs 状态切换参数
     */
    public final void changeState(Task<T> nextState, ChangeStateArgs changeStateArgs) {
        Objects.requireNonNull(nextState, "nextState");
        Objects.requireNonNull(changeStateArgs, "changeStateArgs");

        nextState.setControlData(changeStateArgs);
        tempNextState = nextState;

        if (isRunning() && handler.isReady(this, child, nextState)) {
            template_execute(false);
        }
    }

    // endregion

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        handler.resetForRestart(this);
        if (initState != null) {
            initState.resetForRestart();
        }
        tempNextState = null;
        if (child != null) {
            removeChild(0);
        }
    }

    @Override
    protected void beforeEnter() {
//        super.beforeEnter();
        handler.beforeEnter(this);
        if (initState != null) {
            initState.setSharedProps(initStateProps);
        }
        if (tempNextState == null && initState != null) {
            tempNextState = initState;
        }
        if (tempNextState != null && tempNextState.getControlData() == null) {
            tempNextState.setControlData(ChangeStateArgs.PLAIN);
        }
        // 不清理child是因为允许用户提前指定初始状态
    }

    @Override
    protected void exit() {
        tempNextState = null;
        if (child != null) {
            removeChild(0);
        }
        super.exit();
    }

    @Override
    protected void execute() {
        Task<T> curState = this.child;
        Task<T> nextState = this.tempNextState;
        if (nextState != null && handler.isReady(this, curState, nextState)) {
            stopCurState(curState, (ChangeStateArgs) nextState.getControlData());

            this.tempNextState = null;
            if (curState != null) {
                setChild(0, nextState);
            } else {
                addChild(nextState);
            }

            beforeChangeState(curState, nextState);
            nextState.setControlData(null); // 用户需要提前将数据填充到黑板
            template_startChild(nextState, true); // 启动新状态
            return;
        }
        if (curState == null) {
            return;
        }

        // 继续运行或新状态enter；在尾部才能保证安全
        Task<T> inlinedChild = inlineHelper.getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.template_executeInlined(inlineHelper, curState);
        } else if (curState.isRunning()) {
            curState.template_execute(true);
        } else {
            template_startChild(curState, true);
        }
    }

    private void stopCurState(Task<T> curState, ChangeStateArgs changeStateArgs) {
        if (curState == null) return;
        if (changeStateArgs.delayMode == 0 && changeStateArgs.delayArg > 0) {
            curState.stop(changeStateArgs.delayArg);
        } else {
            curState.stop();
        }
        inlineHelper.stopInline(); // help gc
    }

    protected void beforeChangeState(Task<T> curState, Task<T> nextState) {
        assert curState != null || nextState != null;
        handler.beforeChangeState(this, curState, nextState);
    }

    @Override
    protected void onChildRunning(Task<T> child, boolean starting) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        assert this.child == child;
        inlineHelper.stopInline();

        // 先判断是否有下一个状态，保持和changeState调用相同的逻辑
        if (tempNextState != null) {
            template_execute(false);
            return;
        }
        if (handler.onNextStateAbsent(this, child)) {
            return;
        }
        removeChild(0);
        beforeChangeState(child, null);
    }

    // region find

    /**
     * 查找task最近的状态机节点
     * 1.仅递归查询父节点和长兄节点
     * 2.优先查找附近的，然后测试长兄节点 - 状态机作为第一个节点的情况比较常见
     */
    public static <T> StateMachineTask<T> findStateMachine(Task<T> task) {
        Task<T> control;
        while ((control = task.getControl()) != null) {
            // 父节点
            if (control instanceof StateMachineTask<T> stateMachineTask) {
                return stateMachineTask;
            }
            // 长兄节点
            Task<T> eldestBrother = control.getChild(0);
            if (eldestBrother instanceof StateMachineTask<T> stateMachineTask) {
                return stateMachineTask;
            }
            task = control;
        }
        throw new IllegalStateException("cant find stateMachine from controls");
    }

    /**
     * 查找task最近的状态机节点
     * 1.名字不为空的情况下，支持从兄弟节点中查询
     * 2.优先测试父节点，然后测试兄弟节点
     */
    @Nonnull
    public static <T> StateMachineTask<T> findStateMachine(Task<T> task, String name) {
        if (ObjectUtils.isBlank(name)) {
            return findStateMachine(task);
        }
        Task<T> control;
        StateMachineTask<T> stateMachine;
        while ((control = task.getControl()) != null) {
            // 父节点
            if ((stateMachine = castAsStateMachine(control, name)) != null) {
                return stateMachine;
            }
            // 兄弟节点
            for (int i = 0, n = control.getChildCount(); i < n; i++) {
                final Task<T> brother = control.getChild(i);
                if ((stateMachine = castAsStateMachine(brother, name)) != null) {
                    return stateMachine;
                }
            }
            task = control;
        }
        throw new IllegalStateException("cant find stateMachine from controls and brothers");
    }

    private static <T> StateMachineTask<T> castAsStateMachine(Task<T> task, String name) {
        if (task instanceof StateMachineTask<T> stateMachineTask
                && Objects.equals(name, stateMachineTask.getName())) {
            return stateMachineTask;
        }
        return null;
    }

    // endregion

    // region 序列化

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public Task<T> getInitState() {
        return initState;
    }

    public void setInitState(Task<T> initState) {
        this.initState = initState;
    }

    public Object getInitStateProps() {
        return initStateProps;
    }

    public void setInitStateProps(Object initStateProps) {
        this.initStateProps = initStateProps;
    }

    public StateMachineHandler<T> getHandler() {
        return handler;
    }

    public void setHandler(StateMachineHandler<T> handler) {
        this.handler = handler == null ? StateMachineHandlers.defaultHandler() : handler;
    }
    // endregion
}
