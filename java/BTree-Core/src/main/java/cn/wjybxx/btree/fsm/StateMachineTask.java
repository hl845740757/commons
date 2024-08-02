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
import cn.wjybxx.base.collection.BoundedArrayDeque;
import cn.wjybxx.base.collection.DequeOverflowBehavior;
import cn.wjybxx.base.collection.EmptyDequeue;
import cn.wjybxx.btree.Decorator;
import cn.wjybxx.btree.ICancelToken;
import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskStatus;
import cn.wjybxx.btree.branch.Join;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Deque;
import java.util.Objects;

/**
 * 状态机节点
 * 1.redo和undo是很有用的特性，因此我们在顶层给予支持，但默认的队列不会保存状态。
 * 2.以我的经验来看，状态机是最重要的节点，{@link Join}则是是仅次于状态机的节点 -- 不能以使用数量而定。
 *
 * @author wjybxx
 * date - 2023/12/1
 */
public class StateMachineTask<T> extends Decorator<T> {

    /** 状态机名字 */
    private String name;
    /** 无可用状态时状态码 -- 默认成功退出更安全 */
    private int noneChildStatus = TaskStatus.SUCCESS;
    /** 初始状态 */
    private Task<T> initState;
    /** 初始状态的属性 */
    private Object initStateProps;

    private transient Task<T> tempNextState;
    private transient Deque<Task<T>> undoQueue = EmptyDequeue.getInstance();
    private transient Deque<Task<T>> redoQueue = EmptyDequeue.getInstance();
    private transient ICancelToken childCancelToken;

    private transient StateMachineListener<T> listener;
    private transient StateMachineHandler<T> stateMachineHandler;

    // region

    /** 获取当前状态 */
    public final Task<T> getCurState() {
        return child;
    }

    /** 获取临时的下一个状态 */
    public final Task<T> getTempNextState() {
        return tempNextState;
    }

    /** 丢弃未切换的临时状态 */
    public final Task<T> discardTempNextState() {
        Task<T> r = tempNextState;
        if (r != null) tempNextState = null;
        return r;
    }

    /** 对当前当前状态发出取消命令 */
    public final void cancelCurState(int cancelCode) {
        if (child != null && child.isRunning()) {
            child.getCancelToken().cancel(cancelCode);
        }
    }

    /** 查看undo对应的state */
    public final Task<T> peekUndoState() {
        return undoQueue.peekLast();
    }

    /** 查看redo对应的state */
    public final Task<T> peekRedoState() {
        return redoQueue.peekFirst();
    }

    /** 开放以允许填充 */
    public final Deque<Task<T>> getUndoQueue() {
        return undoQueue;
    }

    /** 开放以允许填充 */
    public final Deque<Task<T>> getRedoQueue() {
        return redoQueue;
    }

    /**
     * @param maxSize 最大大小；0表示禁用；大于0启用
     * @return 最新的queue
     */
    public final Deque<Task<T>> setUndoQueueSize(int maxSize) {
        if (maxSize < 0) throw new IllegalArgumentException("maxSize: " + maxSize);
        return undoQueue = setQueueMaxSize(undoQueue, maxSize, DequeOverflowBehavior.DISCARD_HEAD);
    }

    /**
     * @param maxSize 最大大小；0表示禁用；大于0启用
     * @return 最新的queue
     */
    public final Deque<Task<T>> setRedoQueueSize(int maxSize) {
        if (maxSize < 0) throw new IllegalArgumentException("maxSize: " + maxSize);
        return redoQueue = setQueueMaxSize(redoQueue, maxSize, DequeOverflowBehavior.DISCARD_TAIL);
    }

    private static <T> Deque<T> setQueueMaxSize(Deque<T> queue, int maxSize, DequeOverflowBehavior overflowBehavior) {
        if (maxSize == 0) {
            queue.clear();
            return EmptyDequeue.getInstance();
        }
        if (queue == EmptyDequeue.INSTANCE) {
            return new BoundedArrayDeque<>(maxSize, overflowBehavior);
        } else {
            BoundedArrayDeque<T> boundedArrayDeque = (BoundedArrayDeque<T>) queue;
            boundedArrayDeque.setCapacity(maxSize, overflowBehavior);
            return queue;
        }
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
     * @param changeStateArgs 状态切换参数
     * @return 如果有前一个状态则返回true
     */
    public final boolean undoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.isUndo()) {
            throw new IllegalArgumentException();
        }
        Task<T> prevState = undoQueue.peekLast(); // 真正切换以后再删除
        if (prevState == null) {
            return false;
        }
        changeState(prevState, changeStateArgs);
        return true;
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
     * @param changeStateArgs 状态切换参数
     * @return 如果有下一个状态则返回true
     */
    public final boolean redoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.isRedo()) {
            throw new IllegalArgumentException();
        }
        Task<T> nextState = redoQueue.peekFirst();  // 真正切换以后再删除
        if (nextState == null) {
            return false;
        }
        changeState(nextState, changeStateArgs);
        return true;
    }

    /** 切换状态 -- 如果状态机处于运行中，则立即切换 */
    public final void changeState(Task<T> nextState) {
        changeState(nextState, ChangeStateArgs.PLAIN);
    }

    /***
     * 切换状态
     * 1.如果当前有一个待切换的状态，则会被悄悄丢弃(todo 可以增加一个通知)
     * 2.无论何种模式，在当前状态进入完成状态时一定会触发
     * 3.如果状态机未运行，则仅仅保存在那里，等待下次运行的时候执行
     * 4.当前状态可先正常完成，然后再切换状态，就可以避免进入被取消状态；可参考{@link ChangeStateTask}
     * <pre>{@code
     *      Task<T> nextState = nextState();
     *      setSuccess();
     *      stateMachine.changeState(nextState)
     * }
     * </pre>
     *
     * @param nextState 要进入的下一个状态
     * @param changeStateArgs 状态切换参数
     */
    public final void changeState(Task<T> nextState, ChangeStateArgs changeStateArgs) {
        Objects.requireNonNull(nextState, "nextState");
        Objects.requireNonNull(changeStateArgs, "changeStateArgs");

        changeStateArgs = checkArgs(changeStateArgs);
        nextState.setControlData(changeStateArgs);
        tempNextState = nextState;

        if (isRunning() && isReady(child, nextState)) {
            template_execute();
        }
    }

    /** 检测正确性和自动初始化；不可修改掉cmd */
    protected ChangeStateArgs checkArgs(ChangeStateArgs changeStateArgs) {
        // 当前未运行，不能指定延迟帧号
        if (!isRunning()) {
            if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_NEXT_FRAME) {
                throw new IllegalArgumentException("invalid args");
            }
            return changeStateArgs.withDelayMode(ChangeStateArgs.DELAY_NONE);
        }
        // 运行中一定可以拿到帧号
        if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_NEXT_FRAME) {
            if (changeStateArgs.frame < 0) {
                return changeStateArgs.withFrame(getTaskEntry().getCurFrame() + 1);
            }
        }
        return changeStateArgs;
    }
    // endregion

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        if (stateMachineHandler != null) {
            stateMachineHandler.resetForRestart(this);
        }
        if (initState != null) {
            initState.resetForRestart();
        }
        if (child != null) {
            removeChild(0);
        }
        tempNextState = null;
        undoQueue.clear(); // 保留用户的设置
        redoQueue.clear();
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (stateMachineHandler != null) {
            stateMachineHandler.beforeEnter(this);
        }
        if (childCancelToken == null) {
            childCancelToken = cancelToken.newInstance();
        }
        if (noneChildStatus == 0) {  // 兼容编辑器忘记赋值，默认成功退出更安全
            noneChildStatus = TaskStatus.SUCCESS;
        }
        if (initState != null && initStateProps != null) {
            initState.setSharedProps(initStateProps);
        }
        if (tempNextState == null && initState != null) { // 允许运行前调用changeState
            tempNextState = initState;
        }
        if (tempNextState != null && tempNextState.getControlData() == null) {
            tempNextState.setControlData(ChangeStateArgs.PLAIN);
        }
        if (child != null) {
            child.setCancelToken(childCancelToken);
        }
    }

    @Override
    protected void exit() {
        if (child != null) {
            removeChild(0);
        }
        tempNextState = null;
        undoQueue.clear();
        redoQueue.clear();
        super.exit();
    }

    @Override
    protected void execute() {
        Task<T> curState = this.child;
        Task<T> nextState = this.tempNextState;
        if (nextState != null && isReady(curState, nextState)) {
            this.tempNextState = null;
            if (!template_checkGuard(nextState.getGuard())) { // 下个状态无效
                nextState.setGuardFailed(null);
                if (stateMachineHandler != null) { // 通知特殊情况
                    stateMachineHandler.onNextStateGuardFailed(this, nextState);
                }
            } else {
                if (curState != null) {
                    curState.stop();
                    inlineHelper.stopInline(); // help gc
                }
                ChangeStateArgs changeStateArgs = (ChangeStateArgs) nextState.getControlData();
                switch (changeStateArgs.cmd) {
                    case ChangeStateArgs.CMD_UNDO -> {
                        undoQueue.pollLast();
                        if (curState != null) {
                            redoQueue.offerFirst(curState);
                        }
                    }
                    case ChangeStateArgs.CMD_REDO -> {
                        redoQueue.pollFirst();
                        if (curState != null) {
                            undoQueue.offerLast(curState);
                        }
                    }
                    default -> {
                        // 进入新状态，需要清理redo队列
                        redoQueue.clear();
                        if (curState != null) {
                            undoQueue.offerLast(curState);
                        }
                    }
                }
                notifyChangeState(curState, nextState);

                curState = nextState;
                cancelToken.addListener(childCancelToken); // 需要每次添加
                curState.setCancelToken(cancelToken); // state可独立取消 -- 可复用cancelToken
                curState.setControlData(null);
                if (child != null) {
                    setChild(0, curState);
                } else {
                    addChild(curState);
                }
            }
        }
        if (curState == null) { // 当前无可用状态
            onNoChildRunning();
            return;
        }

        // 继续运行或新状态enter；在尾部才能保证安全
        Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
        if (inlinedRunningChild != null) {
            template_runInlinedChild(inlinedRunningChild, inlineHelper, curState);
        } else if (curState.isRunning()) {
            curState.template_execute();
        } else {
            template_runChildDirectly(curState);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        assert this.child == child;
        inlineHelper.stopInline();
        cancelToken.remListener(childCancelToken);
        childCancelToken.reset();
        child.setCancelToken(null);

        if (tempNextState == null) {
            if (stateMachineHandler != null && stateMachineHandler.onNextStateAbsent(this, child)) {
                return;
            }
            undoQueue.offerLast(child);
            removeChild(0);
            notifyChangeState(child, null);
            onNoChildRunning();
        } else {
            ChangeStateArgs changeStateArgs = (ChangeStateArgs) tempNextState.getControlData();
            if (changeStateArgs == null) {
                tempNextState.setControlData(ChangeStateArgs.PLAIN);
            } else {
                // 必须立即切换，保留其它参数(extraInfo - 用户需要)
                tempNextState.setControlData(changeStateArgs.withDelayMode(ChangeStateArgs.DELAY_NONE));
            }
            template_execute();
        }
    }

    protected final void onNoChildRunning() {
        if (noneChildStatus != TaskStatus.RUNNING) {
            setCompleted(noneChildStatus, false);
        }
    }

    protected boolean isReady(@Nullable Task<T> curState, Task<?> nextState) {
        if (curState == null) {
            return true;
        }
        ChangeStateArgs changeStateArgs = (ChangeStateArgs) nextState.getControlData();
        if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_CURRENT_COMPLETED) {
            return curState.isCompleted();
        }
        if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_NEXT_FRAME) {
            return getTaskEntry().getCurFrame() >= changeStateArgs.frame;
        }
        return true;
    }

    protected final void notifyChangeState(@Nullable Task<T> curState, @Nullable Task<T> nextState) {
        assert curState != null || nextState != null;
        if (listener != null) listener.beforeChangeState(this, curState, nextState);
    }

    // region

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

    //

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public int getNoneChildStatus() {
        return noneChildStatus;
    }

    public void setNoneChildStatus(int noneChildStatus) {
        this.noneChildStatus = noneChildStatus;
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

    public StateMachineListener<T> getListener() {
        return listener;
    }

    public void setListener(StateMachineListener<T> listener) {
        this.listener = listener;
    }

    public StateMachineHandler<T> getStateMachineHandler() {
        return stateMachineHandler;
    }

    public void setStateMachineHandler(StateMachineHandler<T> stateMachineHandler) {
        this.stateMachineHandler = stateMachineHandler;
    }
}
