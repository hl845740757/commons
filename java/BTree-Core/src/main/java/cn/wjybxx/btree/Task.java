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
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * <h3>取消</h3>
 * 1.取消默认是协作式的，依赖于任务自身检查；如果期望更及时的响应取消信号，则需要注册注册监听器。
 * 2.通常在执行外部代码后都应该检测.
 * 3.一般而言，不管理上下文的节点在子节点取消时都应该取消自己（因为是同一个CancelToken）
 * 4.Task类默认只在心跳方法中检测取消信号，任何的回调和事件方法中都由用户自身检测。
 * 5.Task在运行期间，最多只应该添加一次监听。
 *
 * <h3>心跳+事件驱动</h3>
 * 1.心跳为主，事件为辅。
 * 2.心跳不是事件！心跳自顶向下驱动，事件则无规律。
 *
 * @param <T> 黑板的类型
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class Task<T> implements ICancelTokenListener {

    public static final Logger logger = LoggerFactory.getLogger(Task.class);

    /** 低5位记录Task重写了哪些方法 */
    private static final int MASK_OVERRIDES = 31;
    /** 低[6~10]位记录前一次的运行结果，范围 [0, 31] */
    private static final int MASK_PREV_STATUS = 31 << 5;
    /** 前一次运行结果的存储偏移量 */
    private static final int OFFSET_PREV_STATUS = 5;

    private static final int MASK_INHERITED_BLACKBOARD = 1 << 10;
    private static final int MASK_INHERITED_PROPS = 1 << 11;
    private static final int MASK_INHERITED_CANCEL_TOKEN = 1 << 12;
    private static final int MASK_STOP_EXIT = 1 << 13;
    private static final int MASK_STILLBORN = 1 << 14;
    private final int MASK_DISABLE_NOTIFY = 1 << 15;
    static final int MASK_CHECKING_GUARD = 1 << 16;
    private static final int MASK_NOT_ACTIVE_SELF = 1 << 17;
    private static final int MASK_NOT_ACTIVE_IN_HIERARCHY = 1 << 18;
    private static final int MASK_REGISTERED_LISTENER = 1 << 19;

    public static final int MASK_SLOW_START = 1 << 24;
    public static final int MASK_AUTO_RESET_CHILDREN = 1 << 25;
    public static final int MASK_MANUAL_CHECK_CANCEL = 1 << 26;
    public static final int MASK_AUTO_LISTEN_CANCEL = 1 << 27;
    public static final int MASK_CANCEL_TOKEN_PER_CHILD = 1 << 28;
    public static final int MASK_BLACKBOARD_PER_CHILD = 1 << 29;
    public static final int MASK_INVERTED_GUARD = 1 << 30;
    public static final int MASK_BREAK_INLINE = 1 << 31;
    /** 高8位为流程控制特征值（对外开放） */
    public static final int MASK_CONTROL_FLOW_OPTIONS = (-1) << 24;

    /** 条件节点的基础选项 */
    private static final int MASK_GUARD_BASE_OPTIONS = MASK_CHECKING_GUARD | MASK_MANUAL_CHECK_CANCEL;
    /** enter前相关options */
    private static final int MASK_BEFORE_ENTER_OPTIONS = MASK_AUTO_LISTEN_CANCEL | MASK_AUTO_RESET_CHILDREN;

    /** 任务树的入口(缓存以避免递归查找) */
    transient TaskEntry<T> taskEntry;
    /** 任务的控制节点，通常是Task的Parent节点 */
    transient Task<T> control;
    /**
     * 任务运行时依赖的黑板（主要上下文）
     * 1.每个任务可有独立的黑板（数据）；
     * 2.运行时不能为null；
     * 3.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
     */
    protected transient T blackboard;
    /**
     * 共享属性（配置上下文）
     * 1.用于解决【数据和行为分离】架构下的配置需求，主要解决策划的配置问题，减少维护工作量。
     * 2.共享属性应该在运行前赋值，不应该也不能被序列化。
     * 3.共享属性应该是只读的、可共享的，因为它是配置。
     * 4.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
     */
    protected transient Object sharedProps;
    /**
     * 取消令牌（取消上下文）
     * 1.每个任务可有独立的取消信号；
     * 2.运行时不能为null；
     * 3.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
     */
    protected transient CancelToken cancelToken;
    /**
     * Control为管理子节点存储在子节点上的数据
     * 1.避免额外映射，提高性能和易用性
     * 2.entry的逻辑control是用户，因此也可以存储用户的数据
     * 3.该属性不自动继承，不属于运行上下文。
     */
    private transient Object controlData;

    /** 任务的状态 -- {@link TaskStatus}，使用int以支持用户返回更详细的错误码 */
    private transient int status;
    /** 任务运行时的控制信息(bits) -- 每次运行时会重置为0 */
    private transient int ctl;
    /** 启动时的帧号 -- 每次运行时会重置，仅保留override信息 */
    private transient int enterFrame;
    /** 结束时的帧号 -- 每次运行时重置为0 */
    private transient int exitFrame;
    /** 重入Id，只增不减 -- 用于事件驱动下检测冲突（递归）；reset时不重置，甚至也增加 */
    private transient short reentryId;

    /**
     * 任务绑定的前置条件(precondition太长...)
     * 编程经验告诉我们：前置条件和行为是由控制节点组合的，而不是行为的属性。
     * 但由于Task只能有一个Control，因此将前置条件存储在Task可避免额外的映射，从而可提高查询性能和易用性；
     * 另外，将前置条件存储在Task上，可以让行为树的结构更加清晰。
     */
    private Task<T> guard;
    /**
     * 任务的自定义标识
     * 1.对任务进行标记是一个常见的需求，我们将其定义在顶层以简化使用
     * 2.在运行期间不应该变动
     * 3.高8位为流程控制特征值，会在任务运行前拷贝到ctl -- 以支持在编辑器导中指定Task的运行特征。
     * 4.用户可以通过flags来识别child，以判断是否处理child的结果
     */
    protected int flags;

    public Task() {
        ctl = TaskOverrides.maskOfTask(getClass());
    }

    // region getter/setter

    public final TaskEntry<T> getTaskEntry() {
        return taskEntry;
    }

    public final Task<T> getControl() {
        return control;
    }

    public final T getBlackboard() {
        return blackboard;
    }

    public final void setBlackboard(T blackboard) {
        this.blackboard = blackboard;
    }

    public final CancelToken getCancelToken() {
        return cancelToken;
    }

    public final void setCancelToken(CancelToken cancelToken) {
        this.cancelToken = cancelToken;
    }

    public final Object getControlData() {
        return controlData;
    }

    public final void setControlData(Object controlData) {
        this.controlData = controlData;
    }

    public final Object getSharedProps() {
        return sharedProps;
    }

    public final void setSharedProps(Object sharedProps) {
        this.sharedProps = sharedProps;
    }

    public final int getEnterFrame() {
        return enterFrame;
    }

    public final int getExitFrame() {
        return exitFrame;
    }

    /** 慎重调用 */
    public final void setEnterFrame(int enterFrame) {
        this.enterFrame = enterFrame;
    }

    /** 慎重调用 */
    public final void setExitFrame(int exitFrame) {
        this.exitFrame = exitFrame;
    }
    // endregion

    // region status

    /** 获取原始的状态码 */
    public final int getStatus() {
        return status;
    }

    /** 获取归一化后的状态码，所有的失败码都转换为{@link TaskStatus#ERROR} */
    public final int getNormalizedStatus() {
        return Math.min(status, TaskStatus.ERROR);
    }

    /** 任务是否正在运行 */
    public final boolean isRunning() {
        return status == TaskStatus.RUNNING;
    }

    /** 任务是否已完成(成功、失败、取消) */
    public final boolean isCompleted() {
        return status >= TaskStatus.SUCCESS;
    }

    /** 任务是否已成功 */
    public final boolean isSucceeded() {
        return status == TaskStatus.SUCCESS;
    }

    /** 任务是否已被取消 */
    public final boolean isCancelled() {
        return status == TaskStatus.CANCELLED;
    }

    /** 任务是否已失败 */
    public final boolean isFailed() {
        return status > TaskStatus.CANCELLED;
    }

    /** 任务是否已失败或被取消 */
    public final boolean isFailedOrCancelled() {
        return status >= TaskStatus.CANCELLED;
    }

    // endregion

    // region context

    /** 获取行为树绑定的实体 -- 最好让Entity也在黑板中 */
    public Object getEntity() {
        if (taskEntry == null) {
            throw new IllegalStateException("This task has never run");
        }
        return taskEntry.getEntity();
    }

    /**
     * 运行的帧数
     * 1.任务如果在首次{@link #execute()}的时候就进入完成状态，那么运行帧数0
     * 2.运行帧数是非常重要的统计属性，值得我们定义在顶层.
     */
    public final int getRunFrames() {
        if (status == TaskStatus.RUNNING) {
            return taskEntry.getCurFrame() - enterFrame;
        }
        // 不测试taskEntry，是因为child可能在运行后被删除
        return exitFrame - enterFrame;
    }

    /**
     * 获取任务前一次的执行结果
     * 1.取值范围[0,31] -- 其实只要能区分成功失败就够；
     * 2.这并不是一个运行时必须的属性，而是为Debug和UI视图服务的；
     */
    public final int getPrevStatus() {
        return (ctl & MASK_PREV_STATUS) >> OFFSET_PREV_STATUS;
    }

    public final void setPrevStatus(int prevStatus) {
        prevStatus = MathCommon.clamp(prevStatus, 0, TaskStatus.MAX_PREV_STATUS);
        ctl |= (prevStatus << OFFSET_PREV_STATUS);
    }

    // endregion

    // region core

    /**
     * 该方法用于初始化对象。
     * 1.不命名为init，是因为init通常让人觉得只调用一次。
     * 2.该方法不可以使自身进入完成状态。
     */
    protected void beforeEnter() {

    }

    /**
     * 该方法在Task进入运行状态时执行
     * 1.数据初始化需要放在{@link #beforeEnter()}中，避免执行逻辑时对象未初始化完成。
     * 2.如果要初始化子节点，也放到{@link #beforeEnter()}方法;
     * 3.允许更新自己为完成状态
     *
     * @param reentryId 用于判断父类是否使任务进入了完成状态；虽然也可先捕获再调用超类方法，但传入会方便许多。
     */
    protected void enter(int reentryId) {

    }

    /**
     * Task的心跳方法，在Task进入完成状态之前会反复执行。
     * 1.运行中可通过{@link #setSuccess()}、{@link #setFailed(int)}、{@link #setCancelled()}将自己更新为完成状态。
     * 2.如果不想和{@link #enter(int)}同步执行，可通过{@link #setSlowStart(boolean)}实现。
     * 3.不建议直接调用该方法，而是通过模板方法{@link #template_execute(boolean)}运行。
     */
    protected abstract void execute();

    /**
     * 该方法在Task进入完成状态时执行
     * 1.该方法与{@link #enter(int)}对应，通常用于停止{@link #enter(int)}中启动的逻辑。
     * 2.该方法也用于清理部分运行时产生的临时数据。
     * 3.一定记得取消注册的各种监听器。
     */
    protected void exit() {

    }

    /** 设置为运行成功 */
    public final void setSuccess() {
        setCompleted(TaskStatus.SUCCESS, false);
    }

    /** 设置为取消 */
    public final void setCancelled() {
        setCompleted(TaskStatus.CANCELLED, false);
    }

    /** 设置为执行失败 */
    public final void setFailed(int status) {
        if (status < TaskStatus.ERROR) {
            throw new IllegalArgumentException("status " + status);
        }
        setCompleted(status, false);
    }

    /**
     * 设置为前置条件测试失败
     * 1.该方法仅适用于control测试child的guard失败，令child在未运行的情况下直接失败的情况。
     * 2.对于运行中的child，如果发现child的guard失败，不能继续运行，应当取消子节点的执行（stop）。
     *
     * @param control 由于task未运行，其control可能尚未赋值，因此要传入
     * @param isHook  是否是钩子任务，钩子任务则禁用回调
     */
    public final void setGuardFailed(Task<T> control, boolean isHook) {
        assert status != TaskStatus.RUNNING;
        setCtlBit(MASK_DISABLE_NOTIFY, isHook); // 需要覆盖旧值
        setControl(control);
        setCompleted(TaskStatus.GUARD_FAILED, false);
    }

    /** 设置为完成 -- 通常用于通过子节点的结果设置自己 */
    public final void setCompleted(int status, boolean fromChild) {
        if (status < TaskStatus.SUCCESS) throw new IllegalArgumentException();
        if (status == TaskStatus.GUARD_FAILED && fromChild) {
            status = TaskStatus.ERROR; // GUARD_FAILED 不能向上传播
        }
        final int prevStatus = this.status;
        if (prevStatus == TaskStatus.RUNNING) {
            if (status == TaskStatus.GUARD_FAILED) {
                throw new IllegalArgumentException("Running task cant fail with 'GUARD_FAILED'");
            }
            this.status = status;
            // template_exit
            if ((ctl & MASK_REGISTERED_LISTENER) != 0) {
                cancelToken.remListener(this);
            }
            exitFrame = taskEntry.getCurFrame();
            stopRunningChildren();
            if ((ctl & TaskOverrides.MASK_EXIT) != 0) {
                exit();
            }
            releaseContext();
            reentryId++;
        } else {
            // 未调用Enter和Exit，需要补偿 -- 保留当前的ctl会更好
            setPrevStatus(prevStatus);
            this.enterFrame = exitFrame;
            this.reentryId++;
            this.ctl |= MASK_STILLBORN;

            this.status = status;
        }
        if ((ctl & MASK_DISABLE_NOTIFY) == 0 && control != null) {
            control.onChildCompleted(this);
        }
    }

    /**
     * 子节点还需要继续运行
     * 1.该方法在运行期间可能被多次调用(非启动时调用表示修复内联)
     * 2.该方法不应该触发状态迁移，即不应该使自己进入完成状态
     *
     * @param starting 是否是启动时调用，即首次调用
     */
    protected abstract void onChildRunning(Task<T> child, boolean starting);

    /**
     * 子节点进入完成状态
     * 1.避免方法数太多，实现类测试task的status即可
     * 2.{@link #getNormalizedStatus()}有助于switch测试
     * 3.task可能是取消状态，甚至可能没运行过直接失败（前置条件失败）
     * 4.钩子任务和guard不会调用该方法
     * 5.同一子节点连续通知的情况下，completed的逻辑应当覆盖{@link #onChildRunning(Task, boolean)}的影响。
     * 6.任何的回调和事件方法中都由用户自身检测取消信号
     */
    protected abstract void onChildCompleted(Task<T> child);

    /**
     * Task收到外部事件
     *
     * @see #onEventImpl(Object)
     */
    public final void onEvent(@Nonnull Object event) {
        if ((ctl & TaskOverrides.MASK_CAN_HANDLE_EVENT) == 0) {
            if (status == TaskStatus.RUNNING) {
                onEventImpl(event);
            }
        } else if (canHandleEvent(event)) {
            onEventImpl(event);
        }
    }

    /**
     * 该方法用于测试自己的状态和事件数据
     * (任何事件处理中，都用用户自身检测取消信号)
     * <p>
     * 如果通过条件Task来实现事件过滤，那么通常的写法如下：
     * <pre>
     *     blackboard.set("event", event); // task通过黑板获取事件对象
     *     try {
     *         return template_checkGuard(eventFilter);
     *     } finally {
     *         blackboard.remove("event");
     *     }
     * </pre>
     * ps: 如果想支持编辑器中测试事件属性，event通常需要实现为KV结构。
     */
    public boolean canHandleEvent(@Nonnull Object event) {
        return status == TaskStatus.RUNNING;
    }

    /**
     * 对于控制节点，通常将事件派发给约定的子节点或钩子节点。
     * 对于叶子节点，通常自身处理事件。
     * 注意：
     * 1.转发事件时应该调用子节点的{@link #onEvent(Object)}方法
     * 2.在AI这样的领域中，建议将事件转化为信息存储在Task或黑板中，而不是尝试立即做出反应。
     */
    protected abstract void onEventImpl(@Nonnull Object event);

    /**
     * 取消令牌的回调方法
     * 注意：如果未启动自动监听，手动监听时也建议绑定到该方法
     */
    @Override
    public void onCancelRequested(CancelToken cancelToken) {
        if (isRunning()) setCompleted(TaskStatus.CANCELLED, false);
    }

    /** 强制停止任务 */
    public final void stop() {
        stop(TaskStatus.CANCELLED);
    }

    /**
     * 强制停止任务
     * 1.只应该由Control调用，因此不会通知Control
     * 2.未完成的任务默认会进入Cancelled状态
     * 3.不命名为cancel，否则容易误用；我们设计的cancel是协作式的，可通过{@link #cancelToken}发出请求请求。
     *
     * @param result 取消任务时的分配的结果，0默认为取消 -- 更好的支持FSM
     */
    public final void stop(int result) {
        if (result == 0) {
            result = TaskStatus.CANCELLED;
        }
        // 被显式调用stop的task不能通知父节点，只要任务执行过就需要标记
        if (status == TaskStatus.RUNNING) {
            ctl |= MASK_DISABLE_NOTIFY;
            setCompleted(result, false);
        }
    }

    /**
     * 停止所有运行中的子节点
     * 1.该方法在自身的exit之前调用
     * 2.如果有特殊的子节点（钩子任务），也需要在这里停止
     * 3.该方法不采用visitor实现，是因为停止任务可能有特殊的时序。
     * 4.如果子节点不是按原始序启动的，务必要重写该方法。
     */
    protected void stopRunningChildren() {
        // 停止child时默认逆序停止；一般而言都是后面的子节点依赖前面的子节点
        for (int idx = getChildCount() - 1; idx >= 0; idx--) {
            final Task<T> child = getChild(idx);
            if (child.status == TaskStatus.RUNNING) {
                child.stop(TaskStatus.CANCELLED);
            }
        }
    }

    /**
     * 重置任务以便重新启动(清理运行产生的所有临时数据)
     * <p>
     * 1. 和exit一样，清理的是运行时产生的临时数据，而不是所有数据；不过该方法是比exit更彻底的清理。
     * 2. 钩子任务也应当被重置。
     * 3. 与{@link #beforeEnter()}相同，重写方法时，应先执行父类逻辑，再重置自身属性。
     * 4. 有临时数据的Task都应该重写该方法，行为树通常是需要反复执行的。
     */
    public void resetForRestart() {
        if (status == TaskStatus.NEW) {
            return;
        }
        if (status == TaskStatus.RUNNING) {
            stop(TaskStatus.CANCELLED);
        }
        resetChildrenForRestart();
        if (guard != null) {
            guard.resetForRestart();
        }
        if (this != taskEntry) {
            unsetControl();
        }
        status = 0;
        ctl &= MASK_OVERRIDES; // 保留Overrides信息
        enterFrame = 0;
        exitFrame = 0;
        reentryId++; // 上下文变动，和之前的执行分开
    }

    /** 重置所有的子节点 */
    public final void resetChildrenForRestart() {
        // 由于所有的子节点都已停止，因此重置顺序无影响
        visitChildren(TaskVisitors.resetForRestart(), null);
    }

    /** 当前节点自身是否为active状态 */
    public final boolean isActiveSelf() {
        return (ctl & MASK_NOT_ACTIVE_SELF) == 0;
    }

    /** 当前节点及其所有父节点是否都为active状态 */
    public final boolean isActiveInHierarchy() {
        return (ctl & MASK_NOT_ACTIVE_IN_HIERARCHY) == 0;
    }

    /**
     * 修改节点的active状态
     * 注意：
     * 1.active为false表示可以不执行心跳逻辑{@link #execute()}
     * 2.只有停止Execute而不影响逻辑的场景，才可能需要该特性。比如：等待事件发生。
     * 3.如果等待条件或事件的过程中需要响应超时，通常需要通过定时任务唤醒。
     * 4.如果Task处于非运行状态，该属性在运行时被重置。
     * 5.该属性对条件检查无效。
     * 6.为控制复杂度，暂不打算支持activeChanged事件。
     */
    public final void setActive(boolean value) {
        if (isActiveSelf() == value) {
            return;
        }
        setCtlBit(MASK_NOT_ACTIVE_SELF, !value); // 取反
        refreshActiveInHierarchy();
    }

    /**
     * 当节点在层次结构中的Active状态发生变化时调用
     * 1.该方法只在Task处于运行状态下调用。
     * 2.该方法不应该产生状态迁移，即不应该使Task进入完成状态。
     * 3.该方法主要用于暂停关联的外部逻辑，如停止外部计时器。
     * 4.重写该方法通常应该重写enter方法，在enter方法中处理未激活的情况。
     */
    protected void onActiveInHierarchyChanged() {

    }

    /**
     * 刷新Task在层次结构中的active状态
     */
    public final void refreshActiveInHierarchy() {
        boolean newState = isActiveSelf() && (control == null || control.isActiveInHierarchy());
        if (newState == isActiveInHierarchy()) {
            return;
        }
        setCtlBit(MASK_NOT_ACTIVE_IN_HIERARCHY, !newState); // 取反
        if (status == TaskStatus.RUNNING) {
            onActiveInHierarchyChanged();
            visitChildren(TaskVisitors.refreshActive(), null);
        }
    }

    // endregion

    // region execute-util

    /** 注册取消信号监听器，任务在退出时将自动触发删除 */
    public final void registerCancelListener() {
        cancelToken.addListener(this);
        ctl |= MASK_REGISTERED_LISTENER;
    }

    /**
     * 检查取消
     *
     * @param rid 重入id；方法保存的局部变量
     * @return 任务是否已进入完成状态；如果返回true，调用者应立即退出
     * @see #isExited(int)
     */
    public final boolean checkCancel(int rid) {
        if (rid != this.reentryId) { // exit
            return true;
        }
        if (cancelToken.isCancelRequested()) { // 这里是手动检查
            setCompleted(TaskStatus.CANCELLED, false);
            return true;
        }
        return false;
    }

    /**
     * 获取重入id
     * 1.重入id用于解决事件（或外部逻辑）可能使当前Task进入完成状态的问题。
     * 2.如果执行的外部逻辑可能触发状态切换，在执行外部逻辑前最好捕获重入id，再执行外部逻辑后以检查是否可进行运行。
     */
    public final int getReentryId() {
        return reentryId; // 勿修改返回值类型，以便以后扩展
    }

    /**
     * 重入id对应的任务是否已退出，即：是否已执行{@link #exit()}方法。
     * 1.如果已退出，当前逻辑应该立即退出。
     * 2.通常在执行外部代码后都应该检测，eg：运行子节点，派发事件，执行用户钩子...
     * 3.通常循环体中的代码应该调用{@link #checkCancel(int)}
     * 4.也可以用于检测是否已重新启动
     *
     * @param rid 重入id；方法保存的局部变量
     * @return 重入id对应的任务是否已退出
     */
    public final boolean isExited(int rid) {
        return rid != this.reentryId;
    }

    /**
     * 任务是否未启动就失败了。常见原因：
     * 1. 前置条件失败
     * 2. 任务开始前检测到取消
     */
    public final boolean isStillborn() {
        return (ctl & MASK_STILLBORN) != 0;
    }

    /** 当前是否是条件检查上下文 */
    public final boolean isCheckingGuard() {
        return (ctl & MASK_CHECKING_GUARD) != 0;
    }

    /** 是否可以延迟启动 */
    private static boolean checkSlowStart(int ctl) {
        // 条件节点不可延迟启动；其它情况下只有用户请求延迟启动的Task才可延迟启动
        return (ctl & (MASK_CHECKING_GUARD | MASK_SLOW_START)) == MASK_SLOW_START;
    }

    // endregion

    // region options

    /**
     * 告知模板方法否将{@link #enter(int)}和{@link #execute()}方法分开执行。
     * 1.默认值由{@link #flags}中的信息指定，默认不分开执行
     * 2.要覆盖默认值应当在{@link #beforeEnter()}和{@link #enter(int)}方法中调用
     * 3.该属性运行期间不应该调整，调整也无效
     */
    public final void setSlowStart(boolean value) {
        setCtlBit(MASK_SLOW_START, value);
    }

    public final boolean isSlowStart() {
        return (ctl & MASK_SLOW_START) != 0;
    }

    /**
     * 告知模板方法是否在{@link #enter(int)}前自动调用{@link #resetChildrenForRestart()}
     * 1.默认值由{@link #flags}中的信息指定，默认false
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     * 3.部分任务可能在调用{@link #resetForRestart()}之前不会再次运行，因此需要该特性
     */
    public final void setAutoResetChildren(boolean value) {
        setCtlBit(MASK_AUTO_RESET_CHILDREN, value);
    }

    public final boolean isAutoResetChildren() {
        return (ctl & MASK_AUTO_RESET_CHILDREN) != 0;
    }

    /**
     * 告知模板方法是否手动检测取消
     * 1.默认值由{@link #flags}中的信息指定，默认false
     * 2.是否检测取消信号是一个动态的属性，可随时更改 -- 因此不要轻易缓存。
     */
    public final void setManualCheckCancel(boolean value) {
        setCtlBit(MASK_MANUAL_CHECK_CANCEL, value);
    }

    public final boolean isManualCheckCancel() {
        return (ctl & MASK_MANUAL_CHECK_CANCEL) != 0;
    }

    private boolean isAutoCheckCancel() {
        return (ctl & MASK_MANUAL_CHECK_CANCEL) == 0;
    }

    /**
     * 告知模板方法是否自动监听取消事件
     * 1.默认值由{@link #flags}中的信息指定，默认不自动监听！自动监听有较大的开销，绝大多数业务只需要在Entry监听。
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     */
    public final void setAutoListenCancel(boolean value) {
        setCtlBit(MASK_AUTO_LISTEN_CANCEL, value);
    }

    public final boolean isAutoListenCancel() {
        return (ctl & MASK_AUTO_LISTEN_CANCEL) != 0;
    }

    /**
     * 是否每个child一个独立的取消令牌
     * 1.默认值由{@link #flags}中的信息指定，默认false
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     * 3.该值是否生效取决于控制节点的实现，这里只是提供配置接口。
     */
    public final void setCancelTokenPerChild(boolean value) {
        setCtlBit(MASK_CANCEL_TOKEN_PER_CHILD, value);
    }

    public final boolean isCancelTokenPerChild() {
        return (ctl & MASK_CANCEL_TOKEN_PER_CHILD) != 0;
    }

    /**
     * 是否每个child一个独立的黑板（常见于栈式黑板）
     * 1.默认值由{@link #flags}中的信息指定，默认false
     * 2.该值是否生效取决于控制节点的实现，这里只是提供配置接口。
     */
    public final void setBlackboardPerChild(boolean value) {
        setCtlBit(MASK_BLACKBOARD_PER_CHILD, value);
    }

    public final boolean isBlackboardPerChild() {
        return (ctl & MASK_BLACKBOARD_PER_CHILD) != 0;
    }

    /**
     * 当task作为guard节点时，是否取反(减少栈深度) -- 避免套用{@link cn.wjybxx.btree.decorator.Inverter}节点
     * 1.默认值由{@link #flags}中的信息指定，默认false
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     */
    public final void setInvertedGuard(boolean value) {
        setCtlBit(MASK_INVERTED_GUARD, value);
    }

    public final boolean isInvertedGuard() {
        return (ctl & MASK_INVERTED_GUARD) != 0;
    }

    /**
     * 当Task可以被内联时是否打破内联
     * 1.默认值由{@link #flags}中的信息指定，默认false
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     * 3.它的作用是避免被内联子节点进入完成状态时产生【过长的恢复路径】
     */
    public final void setBreakInline(boolean value) {
        setCtlBit(MASK_BREAK_INLINE, value);
    }

    public final boolean isBreakInline() {
        return (ctl & MASK_BREAK_INLINE) != 0;
    }

    // endregion

    // region 模板方法

    /** start方法不暴露，否则以后难以改动 */
    final void template_start(final Task<T> control, int initMask) {
        assert status != TaskStatus.RUNNING;
        // 初始化基础上下文后才可以检测取消
        if (control != null) {
            initMask |= captureContext(control);
            initMask |= (control.ctl & MASK_NOT_ACTIVE_IN_HIERARCHY);
        }
        initMask |= (ctl & MASK_OVERRIDES); // 方法实现bits
        initMask |= (flags & MASK_CONTROL_FLOW_OPTIONS); // 控制流bits
        ctl = initMask;

        final CancelToken cancelToken = this.cancelToken;
        if (cancelToken.isCancelRequested()) { // 胎死腹中
            releaseContext();
            setCompleted(TaskStatus.CANCELLED, false);
            return;
        }

        final int prevStatus = Math.min(TaskStatus.MAX_PREV_STATUS, this.status);
        initMask |= (prevStatus << OFFSET_PREV_STATUS);
        ctl = initMask;
        status = TaskStatus.RUNNING; // 先更新为running状态，以避免执行过程中外部查询task的状态时仍处于上一次的结束status
        enterFrame = exitFrame = taskEntry.getCurFrame();
        final short reentryId = ++this.reentryId;  // 和上次执行的exit分开
        // beforeEnter
        if ((initMask & TaskOverrides.MASK_BEFORE_ENTER) != 0) {
            beforeEnter(); // 这里用户可能修改控制流标记
        }
        if ((ctl & MASK_BEFORE_ENTER_OPTIONS) != 0) {
            if (isAutoResetChildren()) {
                resetChildrenForRestart();
            }
            if (isAutoListenCancel()) {
                cancelToken.addListener(this);
                ctl |= MASK_REGISTERED_LISTENER;
            }
        }
        // enter
        if ((initMask & TaskOverrides.MASK_ENTER) != 0) {
            enter(reentryId); // enter可能导致结束和取消信号，也可能修改ctl
            if (reentryId != this.reentryId) {
                return;
            }
            if (cancelToken.isCancelRequested() && isAutoCheckCancel()) {
                setCompleted(TaskStatus.CANCELLED, false);
                return;
            }
        }
        if (checkSlowStart(ctl)) { // 需要使用最新的ctl
            if ((initMask & MASK_DISABLE_NOTIFY) == 0 && control != null) {
                control.onChildRunning(this, true);
            }
            return;
        }
        // execute
        execute();
        if (reentryId != this.reentryId) {
            return;
        }
        if (cancelToken.isCancelRequested() && isAutoCheckCancel()) {
            setCompleted(TaskStatus.CANCELLED, false);
            return;
        }
        if ((initMask & MASK_DISABLE_NOTIFY) == 0 && control != null) {
            control.onChildRunning(this, true);
        }
    }

    /**
     * execute模板方法
     * (通过参数的方式，有助于我们统一代码，也简化子类实现；同时避免遗漏)
     *
     * @param fromControl 是否由父节点调用(判断是否是心跳)
     */
    public final void template_execute(boolean fromControl) {
        assert status == TaskStatus.RUNNING;
        // 事件驱动下无法精确判断是否是心跳走到这里，因此只要处于非激活状态就拒绝父节点请求(装死)
        if (fromControl && (ctl & MASK_NOT_ACTIVE_IN_HIERARCHY) != 0) {
            return;
        }
        if (cancelToken.isCancelRequested() && isAutoCheckCancel()) {
            setCompleted(TaskStatus.CANCELLED, false);
            return;
        }
        final short reentryId = this.reentryId;
        execute();
        if (reentryId != this.reentryId) {
            return;
        }
        if (cancelToken.isCancelRequested() && isAutoCheckCancel()) {
            setCompleted(TaskStatus.CANCELLED, false);
            return;
        }
        // 如果可以被内联的子节点没有被内联执行，则尝试通知父节点修复内联
        if (fromControl && isInlinable() && (ctl & MASK_DISABLE_NOTIFY) == 0) {
            control.onChildRunning(this, false);
        }
    }

    /**
     * 被内联子节点的execute模板方法
     *
     * @param helper 存储被内联子节点的对象
     * @param source 被内联前的Task
     */
    public final void template_executeInlined(TaskInlineHelper<T> helper, Task<T> source) {
        assert status == TaskStatus.RUNNING;
        if ((ctl & MASK_NOT_ACTIVE_IN_HIERARCHY) != 0) {
            return;
        }
        // 理论上这里可以先检查一下source的取消令牌，但如果source收到取消信号，则被内联的节点的子节点也一定收到取消信号
        final short sourceReentryId = source.reentryId;
        final short reentryId = this.reentryId;
        // 内联template_execute逻辑
        outer:
        {
            if (cancelToken.isCancelRequested() && isAutoCheckCancel()) {
                setCompleted(TaskStatus.CANCELLED, false);
                break outer;
            }
            execute();
            if (reentryId != this.reentryId) {
                break outer;
            }
            if (cancelToken.isCancelRequested() && isAutoCheckCancel()) {
                setCompleted(TaskStatus.CANCELLED, false);
            }
        }
        // 如果被内联子节点退出，而源任务未退出，则重新内联
        if (reentryId != this.reentryId && sourceReentryId == source.reentryId) {
            helper.inlineChild(source);
        }
    }

    /**
     * 启动子节点
     *
     * @param child      普通子节点，或需要接收通知的钩子任务
     * @param checkGuard 是否检查子节点的前置条件
     */
    public final void template_startChild(Task<T> child, boolean checkGuard) {
        if (!checkGuard || child.guard == null || template_checkGuard(child.guard)) {
            int initMask = (ctl & MASK_CHECKING_GUARD) == 0 ? 0 : MASK_GUARD_BASE_OPTIONS;
            child.template_start(this, initMask);
        } else {
            child.setGuardFailed(this, false);
        }
    }

    /**
     * 启动钩子节点
     * 1.钩子任务不会触发{@link #onChildRunning(Task, boolean)}和{@link #onChildCompleted(Task)}
     * 2.前置条件其实是特殊的钩子任务
     * 3.条件分支通常不应该有钩子任务
     *
     * @param hook       钩子任务，或不需要接收事件通知的子节点
     * @param checkGuard 是否检查子节点的前置条件
     */
    public final void template_startHook(Task<T> hook, boolean checkGuard) {
        if (!checkGuard || hook.guard == null || template_checkGuard(hook.guard)) {
            hook.template_start(this, MASK_DISABLE_NOTIFY);
        } else {
            hook.setGuardFailed(this, true);
        }
    }

    /**
     * 检查前置条件
     * 1.如果未显式设置guard的上下文，会默认捕获当前Task的上下文
     * 2.guard的上下文在运行结束后会被清理
     * 3.guard只应该依赖共享上下文(黑板和props)，不应当对父节点做任何的假设。
     * 4.guard永远是检查当前Task的上下文，子节点的guard也不例外。
     * 5.guard通常不应该修改数据
     * 6.guard默认不检查取消信号，用户可实现取消信号检测节点。
     * 7.如果guard开启了inverter内联或包含Inverter节点，Status将不能保留原始的错误码 -- 只有Sequence能精确返回错误码。
     *
     * @param guard 前置条件；可以是子节点的guard属性，也可以是条件子节点，也可以是外部的条件节点
     */
    public final boolean template_checkGuard(@Nullable Task<T> guard) {
        if (guard == null) {
            return true;
        }
        // 注意：此时需要从flags读取反转标记，因为尚未运行(且guard.guard失败的情况下不会运行)
        boolean inverted = (guard.flags & MASK_INVERTED_GUARD) != 0;
        try {
            // 极少情况下会有前置的前置，更推荐组合节点，更清晰；guard的guard也是检测当前上下文
            if (guard.guard != null && !template_checkGuard(guard.guard)) {
                guard.ctl |= MASK_DISABLE_NOTIFY;
                guard.setCompleted(inverted ? TaskStatus.SUCCESS : TaskStatus.GUARD_FAILED, false);
                return inverted;
            }
            guard.template_start(this, MASK_DISABLE_NOTIFY | MASK_GUARD_BASE_OPTIONS);
            if (guard.isSucceeded()) {
                if (inverted) {
                    guard.status = TaskStatus.ERROR;
                    return false;
                }
                return true;
            }
            if (guard.isFailed()) {
                if (inverted) {
                    guard.status = TaskStatus.SUCCESS;
                    return true;
                }
                return false;
            }
            throw new IllegalStateException("Illegal guard status '%d'. Guards must either succeed or fail in one step."
                    .formatted(guard.getStatus()));
        } finally {
            guard.unsetControl(); // 条件类节点总是及时清理
        }
    }

    /** @return 内部使用的mask */
    private int captureContext(Task<T> control) {
        this.taskEntry = control.taskEntry;
        this.control = control;

        // 如果黑板不为null，则认为是control预设置的；其它属性同理
        int r = 0;
        if (this.blackboard == null) {
            this.blackboard = Objects.requireNonNull(control.blackboard);
            r |= MASK_INHERITED_BLACKBOARD;
        }
        if (this.sharedProps == null && control.sharedProps != null) {
            this.sharedProps = control.sharedProps;
            r |= MASK_INHERITED_PROPS;
        }
        if (this.cancelToken == null) {
            this.cancelToken = Objects.requireNonNull(control.cancelToken);
            r |= MASK_INHERITED_CANCEL_TOKEN;
        }
        return r;
    }

    /** 释放自动捕获的上下文 -- 如果保留上次的上下文，下次执行就会出错（guard是典型） */
    private void releaseContext() {
        final int ctl = this.ctl;
        if ((ctl & MASK_INHERITED_BLACKBOARD) != 0) {
            blackboard = null;
        }
        if ((ctl & MASK_INHERITED_PROPS) != 0) {
            sharedProps = null;
        }
        if ((ctl & MASK_INHERITED_CANCEL_TOKEN) != 0) {
            cancelToken = null;
        }
    }

    /**
     * 设置任务的控制节点
     */
    public final void setControl(Task<T> control) {
        assert control != this;
        if (this == taskEntry) {
            throw new Error();
        }
        this.taskEntry = Objects.requireNonNull(control.taskEntry);
        this.control = control;
    }

    /**
     * 删除任务的控制节点(用于清理)
     * 该方法在任务结束时并不会自动调用，因为Task上的数据可能是有用的，不能立即删除，只有用户知道是否可以清理。
     */
    public final void unsetControl() {
        if (this == taskEntry) {
            throw new Error();
        }
        this.taskEntry = null;
        this.control = null;
        this.blackboard = null;
        this.sharedProps = null;
        this.cancelToken = null;
        this.controlData = null;
    }
    // endregion

    // region child维护

    /**
     * 1.尽量不要在运行时增删子节点（危险操作）
     * 2.不建议将Task从一棵树转移到另一棵树，可能产生内存泄漏（引用未删除干净）
     *
     * @param task 要添加的子节点
     * @return child index
     */
    public final int addChild(final Task<T> task) {
        checkAddChild(task);
        return addChildImpl(task);
    }

    /**
     * 替换指定索引位置的child
     * （该方法可避免Task的结构发生变化，也可以减少事件数）
     *
     * @return index对应的旧节点
     */
    public final Task<T> setChild(int index, Task<T> newTask) {
        checkAddChild(newTask);
        return setChildImpl(index, newTask);
    }

    private void checkAddChild(Task<T> child) {
        if (child == null) throw new NullPointerException("child");
        if (child == this) throw new IllegalArgumentException("add self to children");
        if (child.isRunning()) throw new IllegalArgumentException("child is running");

        if (child.control != this) {
            // 必须先从旧的父节点上删除，但有可能是自己之前放在一边的子节点
            if (child.taskEntry != null || child.control != null) {
                throw new IllegalArgumentException("child.control is not null");
            }
        }
    }

    /** 删除指定child */
    public final boolean removeChild(final Task<?> task) {
        if (null == task) {
            throw new NullPointerException("task");
        }
        // child未启动的情况下，control可能尚未赋值，因此不能检查control来判别
        int index = indexChild(task);
        if (index > 0) {
            removeChildImpl(index);
            task.unsetControl();
            return true;
        }
        return false;
    }

    /** 删除指定索引的child */
    public final Task<T> removeChild(int index) {
        Task<T> child = removeChildImpl(index);
        child.unsetControl();
        return child;
    }

    /** 删除所有的child -- 不是个常用方法 */
    public final void removeAllChild() {
        for (int idx = getChildCount() - 1; idx >= 0; idx--) {
            removeChildImpl(idx).unsetControl();
        }
    }

    /** @return index or -1 */
    public int indexChild(Task<?> task) {
        for (int idx = getChildCount() - 1; idx >= 0; idx--) {
            if (getChild(idx) == task) {
                return idx;
            }
        }
        return -1;
    }

    /** 访问所有的子节点(含hook节点) */
    public abstract void visitChildren(TaskVisitor<? super T> visitor, Object param);

    /** 子节点的数量（仅包括普通意义上的child，不包括钩子任务） */
    public abstract int getChildCount();

    /** 获取指定索引的child */
    public abstract Task<T> getChild(int index);

    /** @return 为child分配的index */
    protected abstract int addChildImpl(Task<T> task);

    /** @return 索引位置旧的child */
    protected abstract Task<T> setChildImpl(int index, final Task<T> task);

    /** @return index对应的child */
    protected abstract Task<T> removeChildImpl(int index);

    // endregion

    // region util

    /** task是否支持内联 */
    public final boolean isInlinable() {
        return (ctl & (TaskOverrides.MASK_INLINABLE | MASK_BREAK_INLINE)) == TaskOverrides.MASK_INLINABLE;
    }

    /** 获取任务的控制流标记位 */
    public final int getControlFlowOptions() {
        return ctl & MASK_CONTROL_FLOW_OPTIONS;
    }

    /** 将task上的临时控制标记写回到flags中 */
    public final void exportControlFlowOptions() {
        int controlFlowOptions = ctl & MASK_CONTROL_FLOW_OPTIONS;
        flags &= ~MASK_CONTROL_FLOW_OPTIONS;
        flags |= controlFlowOptions;
    }

    /** 设置子节点的取消令牌 -- 会自动传播取消信号 */
    public final void setChildCancelToken(Task<T> child, CancelToken childCancelToken) {
        if (childCancelToken != null && childCancelToken != cancelToken) {
            cancelToken.addListener(childCancelToken);
        }
        child.cancelToken = childCancelToken;
    }

    /** 删除子节点的取消令牌 */
    public final void unsetChildCancelToken(Task<T> child) {
        CancelToken childCancelToken = child.cancelToken;
        if (childCancelToken != null && childCancelToken != cancelToken) {
            cancelToken.remListener(childCancelToken);
            childCancelToken.reset();
        }
        child.cancelToken = null;
    }

    /** 停止目标任务 */
    public static void stop(@Nullable Task<?> task) {
        if (task != null && task.status == TaskStatus.RUNNING) {
            task.stop(TaskStatus.CANCELLED);
        }
    }

    /** 重置目标任务 */
    public static void resetForRestart(@Nullable Task<?> task) {
        if (task != null && task.status != TaskStatus.NEW) {
            task.resetForRestart();
        }
    }

    private void setCtlBit(int mask, boolean enable) {
        if (enable) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    // endregion

    // region 序列化

    public final Task<T> getGuard() {
        return guard;
    }

    public final Task<T> setGuard(Task<T> guard) {
        this.guard = guard;
        return this;
    }

    public final int getFlags() {
        return flags;
    }

    public final Task<T> setFlags(int flags) {
        this.flags = flags;
        return this;
    }

    // endregion
}