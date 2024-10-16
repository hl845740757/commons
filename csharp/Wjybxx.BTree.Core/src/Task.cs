﻿#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using static Wjybxx.BTree.TaskOptions;

namespace Wjybxx.BTree
{
/// <summary>
/// Task代表任务数（行为树）中的一个任务。
///
/// <h3>取消</h3>
/// 1.取消默认是协作式的，依赖于任务自身检查；如果期望更及时的响应取消信号，则需要注册注册监听器。
/// 2.通常在执行外部代码后都应该检测.
/// 3.一般而言，不管理上下文的节点在子节点取消时都应该取消自己（因为是同一个CancelToken）
/// 4.Task类默认只在心跳方法中检测取消信号，任何的回调和事件方法中都由用户自身检测。
/// 5.Task在运行期间，最多只应该添加一次监听
///
/// <h3>心跳+事件驱动</h3>
/// 1.心跳为主，事件为辅。
/// 2.心跳不是事件！心跳自顶向下驱动，事件则无规律。
///
/// <h3>关于泛型</h3>
/// Task是泛型的，我的Dson库将支持泛型类的序列化；BTree.Codec中的编解码器由Dson库的注解处理器生成。
/// (脚本在测试用例模块)
///
/// <typeparam name="T">黑板的类型</typeparam>
/// </summary>
public abstract class Task<T> : ICancelTokenListener where T : class
{
    /** 低5位记录Task重写了哪些方法 */
    private const int MASK_OVERRIDES = 31;
    /** 低[6~10]位记录前一次的运行结果，范围 [0, 31] */
    private const int MASK_PREV_STATUS = 31 << 5;
    /** 前一次运行结果的存储偏移量 */
    private const int OFFSET_PREV_STATUS = 5;

    private const int MASK_INHERITED_BLACKBOARD = 1 << 10;
    private const int MASK_INHERITED_PROPS = 1 << 11;
    private const int MASK_INHERITED_CANCEL_TOKEN = 1 << 12;
    private const int MASK_STOP_EXIT = 1 << 13;
    private const int MASK_STILLBORN = 1 << 14;
    private const int MASK_DISABLE_NOTIFY = 1 << 15;
    internal const int MASK_CHECKING_GUARD = 1 << 16;
    private const int MASK_NOT_ACTIVE_SELF = 1 << 17;
    private const int MASK_NOT_ACTIVE_IN_HIERARCHY = 1 << 18;
    private const int MASK_REGISTERED_LISTENER = 1 << 19;

    /** 条件节点的基础选项 */
    private const int MASK_GUARD_BASE_OPTIONS = MASK_CHECKING_GUARD | MASK_MANUAL_CHECK_CANCEL;
    /** enter前相关options */
    private const int MASK_BEFORE_ENTER_OPTIONS = MASK_AUTO_LISTEN_CANCEL | MASK_AUTO_RESET_CHILDREN;

#nullable disable
    /** 任务树的入口(缓存以避免递归查找) */
    [NonSerialized] internal TaskEntry<T> taskEntry;
    /** 任务的控制节点，通常是Task的Parent节点 */
    [NonSerialized] internal Task<T> control;

    /// <summary>
    ///任务运行时依赖的黑板（主要上下文）
    ///1.每个任务可有独立的黑板（数据）；
    ///2.运行时不能为null；
    ///3.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
    /// </summary>
    [NonSerialized] protected T blackboard;
    /// <summary>
    /// 共享属性（配置上下文）
    /// 1.用于解决【数据和行为分离】架构下的配置需求，主要解决策划的配置问题，减少维护工作量。
    /// 2.共享属性应该在运行前赋值，不应该也不能被序列化。
    /// 3.共享属性应该是只读的、可共享的，因为它是配置。
    /// 4.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
    /// </summary>
    /// <returns></returns>
    [NonSerialized] protected object sharedProps;
    /// <summary>
    /// 取消令牌（取消上下文）
    /// 1.每个任务可有独立的取消信号；
    /// 2.运行时不能为null；
    /// 3.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
    /// </summary>
    [NonSerialized] protected CancelToken cancelToken;

    /// <summary>
    /// Control为管理子节点存储在子节点上的数据
    /// 1.避免额外映射，提高性能和易用性
    /// 2.entry的逻辑control是用户，因此也可以存储用户的数据
    /// 3.该属性不自动继承，不属于运行上下文。
    /// </summary>
    [NonSerialized] private object controlData;

    /** 任务的状态 -- <see cref="TaskStatus"/>，使用int以支持用户返回更详细的错误码 */
    [NonSerialized] private int status;
    /** 任务运行时的控制信息(bits) -- 每次运行时会重置，仅保留override信息 */
    [NonSerialized] private int ctl;
    /** 启动时的帧号 -- 每次运行时重置为0 */
    [NonSerialized] private int enterFrame;
    /** 结束时的帧号 -- 每次运行时重置为0 */
    [NonSerialized] private int exitFrame;
    /** 重入Id，只增不减 -- 用于事件驱动下检测冲突（递归）；reset时不重置，甚至也增加 */
    [NonSerialized] private short reentryId;

    /// <summary>
    /// 任务绑定的前置条件(precondition太长...)
    /// 编程经验告诉我们：前置条件和行为是由控制节点组合的，而不是行为的属性。
    /// 但由于Task只能有一个Control，因此将前置条件存储在Task可避免额外的映射，从而可提高查询性能和易用性；
    /// 另外，将前置条件存储在Task上，可以让行为树的结构更加清晰。
    /// </summary>
    private Task<T> guard;
    /// <summary>
    /// 任务的自定义标识
    /// 1.对任务进行标记是一个常见的需求，我们将其定义在顶层以简化使用
    /// 2.在运行期间不应该变动
    /// 3.高8位为流程控制特征值，会在任务运行前拷贝到ctl -- 以支持在编辑器导中指定Task的运行特征。
    /// 4.用户可以通过flags来识别child，以判断是否处理child的结果
    /// ps：<see cref="TaskOptions"/>
    /// </summary>
    protected int flags;

    protected Task() {
        ctl = TaskOverrides.MaskOfTask(GetType());
    }

    #region props

    public TaskEntry<T> TaskEntry => taskEntry;

    public Task<T> Control => control;

    public T Blackboard {
        get => blackboard;
        set => blackboard = value;
    }

    public CancelToken CancelToken {
        get => cancelToken;
        set => cancelToken = value;
    }

    public object SharedProps {
        get => sharedProps;
        set => sharedProps = value;
    }

    public object ControlData {
        get => controlData;
        set => controlData = value;
    }

    /// <summary>
    /// 慎重调用set
    /// </summary>
    public int EnterFrame {
        get => enterFrame;
        set => enterFrame = value;
    }

    /// <summary>
    /// 慎重调用set
    /// </summary>
    public int ExitFrame {
        get => exitFrame;
        set => exitFrame = value;
    }

    #endregion

#nullable enable

    #region status

    /** 获取原始的状态码 */
    public int Status {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status;
    }

    /** 获取归一化后的状态码，所有的失败码都转换为<see cref="TaskStatus.ERROR"/> */
    public int NormalizedStatus {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Math.Min(status, TaskStatus.ERROR);
    }

    /** 任务是否正在运行 */
    public bool IsRunning {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status == TaskStatus.RUNNING;
    }

    /** 任务是否已完成 */
    public bool IsCompleted {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status >= TaskStatus.SUCCESS;
    }

    /** 任务是否已成功 */
    public bool IsSucceeded {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status == TaskStatus.SUCCESS;
    }

    /** 任务是否被取消 */
    public bool IsCancelled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status == TaskStatus.CANCELLED;
    }

    /** 任务是否已失败 */
    public bool IsFailed {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status > TaskStatus.CANCELLED;
    }

    /** 任务是否已失败或被取消 */
    public bool IsFailedOrCancelled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status >= TaskStatus.CANCELLED;
    }

    #endregion

    #region context

#nullable disable
    /// <summary>
    /// 获取行为树绑定的实体 -- 最好让Entity也在黑板中
    /// </summary>
    /// <value></value>
    /// <exception cref="InvalidOperationException"></exception>
    public object Entity {
        get {
            if (taskEntry == null) {
                throw new InvalidOperationException("This task has never run");
            }
            return taskEntry.Entity;
        }
    }
#nullable enable

    /// <summary>
    /// 运行的帧数
    /// 1.任务如果在首次<see cref="Execute"/>的时候就进入完成状态，那么运行帧数0
    /// 2.运行帧数是非常重要的统计属性，值得我们定义在顶层.
    /// </summary>
    /// <value></value>
    public int RunFrames {
        get {
            if (status == TaskStatus.RUNNING) {
                return taskEntry.CurFrame - enterFrame;
            }
            // 不测试taskEntry，是因为child可能在运行后被删除
            return exitFrame - enterFrame;
        }
    }

    /// <summary>
    /// 获取任务前一次的执行结果
    /// 1.取值范围[0,31] -- 其实只要能区分成功失败就够；
    /// 2.这并不是一个运行时必须的属性，而是为Debug和Ui视图用的；
    /// </summary>
    public int PrevStatus {
        get => (ctl & MASK_PREV_STATUS) >> OFFSET_PREV_STATUS;
        set {
            value = Math.Clamp(value, 0, TaskStatus.MAX_PREV_STATUS);
            ctl |= (value << OFFSET_PREV_STATUS);
        }
    }

    #endregion

    #region core

    /// <summary>
    /// 该方法用于初始化对象。
    /// 1.不命名为init，是因为init通常让人觉得只调用一次。
    /// 2.该方法不可以使自身进入完成状态。
    /// </summary>
    protected virtual void BeforeEnter() {
    }

    /// <summary>
    /// 该方法在Task进入运行状态时执行
    /// 1.数据初始化需要放在<see cref="BeforeEnter"/>中，避免执行逻辑时对象未初始化完成。
    /// 2.如果要初始化子节点，也放到<see cref="BeforeEnter"/>方法;
    /// 3.允许更新自己为完成状态
    /// </summary>
    /// <param name="reentryId">用于判断父类是否使任务进入了完成状态；虽然也可先捕获再调用超类方法，但传入会方便许多。</param>
    protected virtual void Enter(int reentryId) {
    }

    /// <summary>
    /// Task的心跳方法，在Task进入完成状态之前会反复执行。
    /// 1.运行中可通过<see cref="SetSuccess"/>、<see cref="SetFailed"/>、<see cref="SetCancelled"/>将自己更新为完成状态。
    /// 2.如果不想和<see cref="Enter"/>同步执行，可通过<see cref="IsSlowStart"/> 实现。
    /// 3.不建议直接调用该方法，而是通过模板方法<see cref="Template_Execute"/>运行。
    /// </summary>
    protected abstract void Execute();

    /// <summary>
    /// 该方法在Task进入完成状态时执行
    /// 1.该方法与<see cref="Enter"/>对应，通常用于停止<see cref="Enter"/>中启动的逻辑。
    /// 2.该方法也用于清理部分运行时产生的临时数据。
    /// 3.一定记得取消注册的各种监听器。
    /// </summary>
    protected virtual void Exit() {
    }

    /** 设置为运行成功 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSuccess() {
        SetCompleted(TaskStatus.SUCCESS, false);
    }

    /** 设置为取消 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCancelled() {
        SetCompleted(TaskStatus.CANCELLED, false);
    }

    /** 设置为执行失败 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFailed(int status) {
        if (status < TaskStatus.ERROR) {
            throw new ArgumentException("status " + status);
        }
        SetCompleted(status, false);
    }

    /// <summary>
    /// 设置为前置条件测试失败
    /// 1.该方法仅适用于control测试child的guard失败，令child在未运行的情况下直接失败的情况。
    /// 2.对于运行中的child，如果发现child的guard失败，不能继续运行，应当取消子节点的执行（stop）。
    /// </summary>
    /// <param name="control">由于task未运行，其control可能尚未赋值，因此要传入</param>
    /// <param name="isHook">是否是钩子任务，钩子任务则禁用回调</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetGuardFailed(Task<T> control, bool isHook) {
        Debug.Assert(this.status != TaskStatus.RUNNING);
        SetCtlBit(MASK_DISABLE_NOTIFY, isHook); // 需要覆盖旧值
        SetControl(control);
        SetCompleted(TaskStatus.GUARD_FAILED, false);
    }

    /** 设置为完成 -- 通常用于通过子节点的结果设置自己 */
    public void SetCompleted(int status, bool fromChild) {
        if (status < TaskStatus.SUCCESS) throw new ArgumentException();
        if (status == TaskStatus.GUARD_FAILED && fromChild) {
            status = TaskStatus.ERROR; // GUARD_FAILED 不能向上传播
        }
        int prevStatus = this.status;
        if (prevStatus == TaskStatus.RUNNING) {
            if (status == TaskStatus.GUARD_FAILED) {
                throw new ArgumentException("Running task cant fail with 'GUARD_FAILED'");
            }
            this.status = status;
            // template_exit
            if ((ctl & MASK_REGISTERED_LISTENER) != 0) {
                cancelToken.RemListener(this);
            }
            exitFrame = taskEntry.CurFrame;
            StopRunningChildren();
            if ((ctl & TaskOverrides.MASK_EXIT) != 0) {
                Exit();
            }
            ReleaseContext();
            reentryId++;
        } else {
            // 未调用Enter和Exit，需要补偿 -- 保留当前的ctl会更好
            this.PrevStatus = prevStatus;
            this.enterFrame = exitFrame;
            this.reentryId++;
            this.ctl |= MASK_STILLBORN;

            this.status = status;
        }
        if ((ctl & MASK_DISABLE_NOTIFY) == 0 && control != null) {
            control.OnChildCompleted(this);
        }
    }

    /// <summary>
    /// 子节点还需要继续运行
    /// 1.该方法在运行期间可能被多次调用(非启动时调用表示修复内联)
    /// 2.该方法不应该触发状态迁移，即不应该使自己进入完成状态
    /// </summary>
    /// <param name="child"></param>
    /// <param name="starting">是否是启动时调用，即首次调用</param>
    protected abstract void OnChildRunning(Task<T> child, bool starting);

    /// <summary>
    /// 子节点进入完成状态
    /// 1.避免方法数太多，实现类测试task的status即可
    /// 2.<see cref="NormalizedStatus"/>有助于switch测试
    /// 3.task可能是取消状态，甚至可能没运行过直接失败（前置条件失败）
    /// 4.钩子任务和guard不会调用该方法
    /// 5.同一子节点连续通知的情况下，completed的逻辑应当覆盖<see cref="OnChildRunning"/>的影响。
    /// 6.任何的回调和事件方法中都由用户自身检测取消信号
    /// </summary>
    /// <param name="child"></param>
    protected abstract void OnChildCompleted(Task<T> child);

    /// <summary>
    /// Task收到外部事件
    /// @see #onEventImpl(Object)
    /// </summary>
    /// <param name="eventObj">外部事件</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnEvent(object eventObj) {
        if ((ctl & TaskOverrides.MASK_CAN_HANDLE_EVENT) == 0) {
            if (status == TaskStatus.RUNNING) {
                OnEventImpl(eventObj);
            }
        } else if (CanHandleEvent(eventObj)) {
            OnEventImpl(eventObj);
        }
    }

    /// <summary>
    /// 该方法用于测试自己的状态和事件数据。
    /// (任何事件处理中，都用用户自身检测取消信号)
    ///
    /// 如果通过条件Task来实现事件过滤，那么通常的写法如下：
    /// <code>
    ///     blackboard.Set("event", event); // task通过黑板获取事件对象
    ///     try {
    ///         return Template_CheckGuard(eventFilter);
    ///     } finally {
    ///         blackboard.Remove("event");
    ///     }
    /// </code>
    /// ps: 如果想支持编辑器中测试事件属性，event通常需要实现为KV结构。
    /// </summary>
    /// <param name="eventObj"></param>
    /// <returns></returns>
    public virtual bool CanHandleEvent(object eventObj) {
        return status == TaskStatus.RUNNING;
    }

    /// <summary>
    /// 对于控制节点，通常将事件派发给约定的子节点或钩子节点。
    /// 对于叶子节点，通常自身处理事件。
    /// 注意：
    /// 1.转发事件时应该调用子节点的<see cref="OnEvent"/>方法
    /// 2.在AI这样的领域中，建议将事件转化为信息存储在Task或黑板中，而不是尝试立即做出反应。
    /// </summary>
    protected abstract void OnEventImpl(object eventObj);

    /// <summary>
    /// 取消令牌的回调方法
    /// 注意：如果未启动自动监听，手动监听时也建议绑定到该方法
    /// </summary>
    /// <param name="cancelToken">进入取消状态的取消令牌</param>
    public virtual void OnCancelRequested(CancelToken cancelToken) {
        if (IsRunning) SetCompleted(TaskStatus.CANCELLED, false);
    }

    /// <summary>
    /// 强制停止任务
    /// 1.只应该由Control调用，因此不会通知Control
    /// 2.未完成的任务默认会进入Cancelled状态
    /// 3.不命名为cancel，否则容易误用；我们设计的cancel是协作式的，可通过<see cref="CancelToken"/>发出请求请求。
    /// </summary>
    /// <param name="result">取消任务时的分配的结果，0默认为取消 -- 更好的支持FSM</param>
    public void Stop(int result = TaskStatus.CANCELLED) {
        if (result == 0) {
            result = TaskStatus.CANCELLED;
        }
        // 被显式调用stop的task不能通知父节点，只要任务执行过就需要标记
        if (this.status == TaskStatus.RUNNING) {
            ctl |= MASK_DISABLE_NOTIFY;
            SetCompleted(result, false);
        }
    }

    /// <summary>
    /// 停止所有运行中的子节点
    /// 1.该方法在自身的exit之前调用
    /// 2.如果有特殊的子节点（钩子任务），也需要在这里停止
    /// 3.该方法不采用visitor实现，是因为停止任务可能有特殊的时序。
    /// 4.如果子节点不是按原始序启动的，务必要重写该方法。
    /// </summary>
    protected virtual void StopRunningChildren() {
        // 停止child时默认逆序停止；一般而言都是后面的子节点依赖前面的子节点
        for (int idx = ChildCount - 1; idx >= 0; idx--) {
            Task<T> child = GetChild(idx);
            if (child.status == TaskStatus.RUNNING) {
                child.Stop();
            }
        }
    }

    /// <summary>
    /// 重置任务以便重新启动(清理运行产生的所有临时数据)
    ///
    /// 1. 和exit一样，清理的是运行时产生的临时数据，而不是所有数据；不过该方法是比exit更彻底的清理。
    /// 2. 钩子任务也应当被重置。
    /// 3. 与<see cref="BeforeEnter"/>相同，重写方法时，应先执行父类逻辑，再重置自身属性。
    /// 4. 有临时数据的Task都应该重写该方法，行为树通常是需要反复执行的。
    /// </summary>
    public virtual void ResetForRestart() {
        if (status == TaskStatus.NEW) {
            return;
        }
        if (status == TaskStatus.RUNNING) {
            Stop();
        }
        ResetChildrenForRestart();
        if (guard != null) {
            guard.ResetForRestart();
        }
        if (this != taskEntry) {
            UnsetControl();
        }
        status = 0;
        ctl &= MASK_OVERRIDES; // 保留Overrides信息
        enterFrame = 0;
        exitFrame = 0;
        reentryId++; // 上下文变动，和之前的执行分开
    }

    /// <summary>
    /// 重置所有的子节点
    /// </summary>
    public void ResetChildrenForRestart() {
        // 由于所有的子节点都已停止，因此重置顺序无影响
        VisitChildren(TaskVisitors.ResetForRestart<T>(), null);
    }

    /// <summary>
    /// 当前节点自身是否为active状态，
    /// </summary>
    public bool IsActiveSelf => (ctl & MASK_NOT_ACTIVE_SELF) == 0;

    /// <summary>
    /// 当前节点及其所有父节点是否都为active状态
    /// </summary>
    public bool IsActiveInHierarchy => (ctl & MASK_NOT_ACTIVE_IN_HIERARCHY) == 0;

    /// <summary>
    /// 修改节点的active状态
    /// 注意：
    /// 1.active为false表示可以不执行心跳逻辑<see cref="Execute"/>。
    /// 2.只有停止Execute而不影响逻辑的场景，才可能需要该特性。比如：等待事件发生。
    /// 3.如果等待条件或事件的过程中需要响应超时，通常需要通过定时任务唤醒。
    /// 4.如果Task处于非运行状态，该属性在运行时被重置。
    /// 5.该属性对条件检查无效。
    /// </summary>
    /// <param name="value"></param>
    public void SetActive(bool value) {
        if (IsActiveSelf == value) {
            return;
        }
        SetCtlBit(MASK_NOT_ACTIVE_SELF, !value); // 取反
        RefreshActiveInHierarchy();
    }

    /// <summary>
    /// 当节点在层次结构中的Active状态发生变化时调用
    /// 1.该方法只在Task处于运行状态下调用。
    /// 2.该方法不应该产生状态迁移，即不应该使Task进入完成状态。
    /// 3.该方法主要用于暂停关联的外部逻辑，如停止外部计时器。
    /// 4.重写该方法通常应该重写enter方法，在enter方法中处理未激活的情况。
    /// </summary>
    protected virtual void OnActiveInHierarchyChanged() {
    }

    /// <summary>
    /// 刷新Task在层次结构中的active状态
    /// </summary>
    public void RefreshActiveInHierarchy() {
        bool newState = IsActiveSelf && (control == null || control.IsActiveInHierarchy);
        if (newState == IsActiveInHierarchy) {
            return;
        }
        SetCtlBit(MASK_NOT_ACTIVE_IN_HIERARCHY, !newState); // 取反
        if (status == TaskStatus.RUNNING) {
            OnActiveInHierarchyChanged();
            VisitChildren(TaskVisitors.RefreshActive<T>(), null);
        }
    }

    #endregion

    #region execute-util

    /// <summary>
    /// 注册取消信号监听器，任务在退出时将自动触发删除
    /// </summary>
    public void RegisterCancelListener() {
        cancelToken.AddListener(this);
        ctl |= MASK_REGISTERED_LISTENER;
    }

    /// <summary>
    /// 检查取消
    /// @see #isExited(int)
    /// </summary>
    /// <param name="rid">重入id；方法保存的局部变量</param>
    /// <returns>任务是否已进入完成状态；如果返回true，调用者应立即退出</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckCancel(int rid) {
        if (rid != this.reentryId) { // exit
            return true;
        }
        if (cancelToken.IsCancelRequested) { // 这里是手动检查
            SetCompleted(TaskStatus.CANCELLED, false);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取重入id
    /// 1.重入id用于解决事件（或外部逻辑）可能使当前Task进入完成状态的问题。
    /// 2.如果执行的外部逻辑可能触发状态切换，在执行外部逻辑前最好捕获重入id，再执行外部逻辑后以检查是否可进行运行。
    /// </summary>
    /// <value></value>
    public int ReentryId {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reentryId; // 勿修改返回值类型，以便以后扩展
    }

    /// <summary>
    /// 重入id对应的任务是否已退出，即：是否已执行<see cref="Exit"/>方法。
    /// 1.如果已退出，当前逻辑应该立即退出。
    /// 2.通常在执行外部代码后都应该检测，eg：运行子节点，派发事件，执行用户钩子...
    /// 3.通常循环体中的代码应该调用<see cref="CheckCancel"/>
    /// 4.也可以用于检测是否已重新启动
    /// </summary>
    /// <param name="rid">重入id；方法保存的局部变量</param>
    /// <returns>重入id对应的任务是否已退出</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsExited(int rid) {
        return rid != this.reentryId;
    }

    /// <summary>
    /// 任务是否未启动就失败了。常见原因：
    /// 1. 前置条件失败
    /// 2. 任务开始前检测到取消
    /// </summary>
    /// <returns>未成功启动则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsStillborn() {
        return (ctl & MASK_STILLBORN) != 0;
    }

    /** 当前是否是条件检查上下文 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCheckingGuard() {
        return (ctl & MASK_CHECKING_GUARD) != 0;
    }

    /** 是否可以延迟启动 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckSlowStart(int ctl) {
        // 条件节点不可延迟启动；其它情况下只有用户请求延迟启动的Task才可延迟启动
        return (ctl & (MASK_CHECKING_GUARD | MASK_SLOW_START)) == MASK_SLOW_START;
    }

    #endregion

    #region options

    /// <summary>
    /// 告知模板方法否将<see cref="Enter"/>和<see cref="Execute"/>方法分开执行。
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认不分开执行
    /// 2.要覆盖默认值应当在<see cref="BeforeEnter"/>和<see cref="Enter"/>方法中调用
    /// 3.该属性运行期间不应该调整，调整也无效>。
    /// </summary>
    public bool IsSlowStart {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_SLOW_START) != 0;
        set => SetCtlBit(MASK_SLOW_START, value);
    }

    /// <summary>
    /// 告知模板方法是否在<see cref="Enter"/>前自动调用<see cref="ResetChildrenForRestart"/>
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认false
    /// 2.要覆盖默认值应当在<see cref="BeforeEnter"/>方法中调用
    /// 3.部分任务可能在调用<see cref="ResetForRestart()"/>之前不会再次运行，因此需要该特性
    /// </summary>
    public bool IsAutoResetChildren {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_AUTO_RESET_CHILDREN) != 0;
        set => SetCtlBit(MASK_AUTO_RESET_CHILDREN, value);
    }

    /// <summary>
    /// 告知模板方法是否手动检测取消
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认false
    /// 2.是否检测取消信号是一个动态的属性，可随时更改 -- 因此不要轻易缓存。
    /// </summary>
    public bool IsManualCheckCancel {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_MANUAL_CHECK_CANCEL) != 0;
        set => SetCtlBit(MASK_MANUAL_CHECK_CANCEL, value);
    }

    private bool IsAutoCheckCancel {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_MANUAL_CHECK_CANCEL) == 0;
    }

    /// <summary>
    /// 告知模板方法是否自动监听取消事件
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认不自动监听！自动监听有较大的开销，绝大多数业务只需要在Entry监听。
    /// 2.要覆盖默认值应当在<see cref="BeforeEnter"/>方法中调用
    /// </summary>
    public bool IsAutoListenCancel {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_AUTO_LISTEN_CANCEL) != 0;
        set => SetCtlBit(MASK_AUTO_LISTEN_CANCEL, value);
    }

    /// <summary>
    /// 是否每个child一个独立的取消令牌
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认false。
    /// 2.要覆盖默认值应当在<see cref="BeforeEnter"/>方法中调用
    /// 3.该值是否生效取决于控制节点的实现，这里只是提供配置接口。
    /// </summary>
    public bool IsCancelTokenPerChild {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_CANCEL_TOKEN_PER_CHILD) != 0;
        set => SetCtlBit(MASK_CANCEL_TOKEN_PER_CHILD, value);
    }

    /// <summary>
    /// 是否每个child一个独立的黑板（常见于栈式黑板）
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认false。
    /// 2.该值是否生效取决于控制节点的实现，这里只是提供配置接口。
    /// </summary>
    public bool IsBlackboardPerChild {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_BLACKBOARD_PER_CHILD) != 0;
        set => SetCtlBit(MASK_BLACKBOARD_PER_CHILD, value);
    }

    /// <summary>
    /// 当task作为guard节点时，是否取反(减少栈深度) -- 避免套用<see cref="Decorator.Inverter{T}"/>
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认false
    /// 2.要覆盖默认值应当在<see cref="BeforeEnter"/>方法中调用
    /// </summary>
    public bool IsInvertedGuard {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_INVERTED_GUARD) != 0;
        set => SetCtlBit(MASK_INVERTED_GUARD, value);
    }

    /// <summary>
    /// 当Task可以被内联时是否打破内联
    /// 1.默认值由<see cref="Flags"/>中的信息指定，默认false
    /// 2.要覆盖默认值应当在<see cref="BeforeEnter"/>方法中调用
    /// 3.它的作用是避免被内联子节点进入完成状态时产生【过长的恢复路径】
    /// </summary>
    public bool IsBreakInline {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_BREAK_INLINE) != 0;
        set => SetCtlBit(MASK_BREAK_INLINE, value);
    }

    #endregion

    #region 模板方法

    /** start方法不暴露，否则以后难以改动 */
    internal void Template_Start(Task<T>? control, int initMask) {
        Debug.Assert(status != TaskStatus.RUNNING);
        // 初始化基础上下文后才可以检测取消
        if (control != null) {
            initMask |= CaptureContext(control);
            initMask |= (control.ctl & MASK_NOT_ACTIVE_IN_HIERARCHY);
        }
        initMask |= (ctl & MASK_OVERRIDES); // 方法实现bits
        initMask |= (flags & MASK_CONTROL_FLOW_OPTIONS); // 控制流bits
        ctl = initMask;

        CancelToken cancelToken = this.cancelToken;
        if (cancelToken.IsCancelRequested) { // 胎死腹中
            ReleaseContext();
            SetCompleted(TaskStatus.CANCELLED, false);
            return;
        }

        int prevStatus = Math.Min(TaskStatus.MAX_PREV_STATUS, this.status);
        initMask |= (prevStatus << OFFSET_PREV_STATUS);
        ctl = initMask;
        status = TaskStatus.RUNNING; // 先更新为running状态，以避免执行过程中外部查询task的状态时仍处于上一次的结束status
        enterFrame = exitFrame = taskEntry.CurFrame;
        short reentryId = ++this.reentryId; // 和上次执行的exit分开
        // beforeEnter
        if ((initMask & TaskOverrides.MASK_BEFORE_ENTER) != 0) {
            BeforeEnter(); // 这里用户可能修改控制流标记
        }
        if ((ctl & MASK_BEFORE_ENTER_OPTIONS) != 0) {
            if (IsAutoResetChildren) {
                ResetChildrenForRestart();
            }
            if (IsAutoListenCancel) {
                cancelToken.AddListener(this);
                ctl |= MASK_REGISTERED_LISTENER;
            }
        }
        // enter
        if ((initMask & TaskOverrides.MASK_ENTER) != 0) {
            Enter(reentryId); // enter可能导致结束和取消信号，也可能修改ctl
            if (reentryId != this.reentryId) {
                return;
            }
#if !TASK_MANUAL_CHECK_CANCEL
            if (cancelToken.IsCancelRequested && IsAutoCheckCancel) {
                SetCompleted(TaskStatus.CANCELLED, false);
                return;
            }
#endif
        }
        if (CheckSlowStart(ctl)) { // 需要使用最新的ctl
            if ((initMask & MASK_DISABLE_NOTIFY) == 0 && control != null) {
                control.OnChildRunning(this, true);
            }
            return;
        }
        // execute
        Execute();
        if (reentryId != this.reentryId) {
            return;
        }
#if !TASK_MANUAL_CHECK_CANCEL
        if (cancelToken.IsCancelRequested && IsAutoCheckCancel) {
            SetCompleted(TaskStatus.CANCELLED, false);
            return;
        }
#endif
        if ((initMask & MASK_DISABLE_NOTIFY) == 0 && control != null) {
            control.OnChildRunning(this, true);
        }
    }

    /// <summary>
    /// execute模板方法
    /// (通过参数的方式，有助于我们统一代码，也简化子类实现；同时避免遗漏)
    /// </summary>
    /// <param name="fromControl">是否由父节点调用(判断是否是心跳)</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Template_Execute(bool fromControl) {
        Debug.Assert(status == TaskStatus.RUNNING);
        // 事件驱动下无法精确判断是否是心跳走到这里，因此只要处于非激活状态就拒绝父节点请求(装死)
        if (fromControl && (ctl & MASK_NOT_ACTIVE_IN_HIERARCHY) != 0) {
            return;
        }
#if !TASK_MANUAL_CHECK_CANCEL
        if (cancelToken.IsCancelRequested && IsAutoCheckCancel) {
            SetCompleted(TaskStatus.CANCELLED, false);
            return;
        }
#endif
        short reentryId = this.reentryId;
        Execute();
        if (reentryId != this.reentryId) {
            return;
        }
#if !TASK_MANUAL_CHECK_CANCEL
        if (cancelToken.IsCancelRequested && IsAutoCheckCancel) {
            SetCompleted(TaskStatus.CANCELLED, false);
            return;
        }
#endif
        // 如果可以被内联的子节点没有被内联执行，则尝试通知父节点修复内联
        if (fromControl && IsInlinable && (ctl & MASK_DISABLE_NOTIFY) == 0) {
            control.OnChildRunning(this, false);
        }
    }

    /// <summary>
    /// 被内联子节点的execute模板方法
    /// </summary>
    /// <param name="helper">存储被内联子节点的对象</param>
    /// <param name="source">被内联前的Task</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Template_ExecuteInlined(ref TaskInlineHelper<T> helper, Task<T> source) {
        Debug.Assert(status == TaskStatus.RUNNING && this != source);
        if ((ctl & MASK_NOT_ACTIVE_IN_HIERARCHY) != 0) {
            return;
        }
        // 理论上这里可以先检查一下source的取消令牌，但如果source收到取消信号，则被内联的节点的子节点也一定收到取消信号
        short sourceReentryId = source.reentryId;
        short reentryId = this.reentryId;
        // 内联template_execute逻辑
        {
#if !TASK_MANUAL_CHECK_CANCEL
            if (cancelToken.IsCancelRequested && IsAutoCheckCancel) {
                SetCompleted(TaskStatus.CANCELLED, false);
                goto outer;
            }
#endif
            Execute();
            if (reentryId != this.reentryId) {
                goto outer;
            }
#if !TASK_MANUAL_CHECK_CANCEL
            if (cancelToken.IsCancelRequested && IsAutoCheckCancel) {
                SetCompleted(TaskStatus.CANCELLED, false);
            }
#endif
        }
        outer:
        {
            // 如果被内联子节点退出，而源任务未退出，则重新内联
            if (reentryId != this.reentryId && sourceReentryId == source.reentryId) {
                helper.InlineChild(source);
            }
        }
    }

    /// <summary>
    /// 启动子节点
    /// </summary>
    /// <param name="child">普通子节点，或需要接收通知的钩子任务</param>
    /// <param name="checkGuard">是否检查子节点的前置条件</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Template_StartChild(Task<T> child, bool checkGuard) {
        if (!checkGuard || child.guard == null || Template_CheckGuard(child.guard)) {
            int initMask = (ctl & MASK_CHECKING_GUARD) == 0 ? 0 : MASK_GUARD_BASE_OPTIONS;
            child.Template_Start(this, initMask);
        } else {
            child.SetGuardFailed(this, false);
        }
    }

    /// <summary>
    /// 启动钩子节点
    /// 1.钩子任务不会触发<see cref="OnChildRunning"/>和<see cref="OnChildCompleted"/>
    /// 2.前置条件其实是特殊的钩子任务
    /// 3.条件分支通常不应该有钩子任务
    /// </summary>
    /// <param name="hook">钩子任务，或不需要接收事件通知的子节点</param>
    /// <param name="checkGuard">是否检查子节点的前置条件</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Template_StartHook(Task<T> hook, bool checkGuard) {
        if (!checkGuard || hook.guard == null || Template_CheckGuard(hook.guard)) {
            hook.Template_Start(this, MASK_DISABLE_NOTIFY);
        } else {
            hook.SetGuardFailed(this, true);
        }
    }

    /// <summary>
    /// 检查前置条件
    /// 1.如果未显式设置guard的上下文，会默认捕获当前Task的上下文
    /// 2.guard的上下文在运行结束后会被清理
    /// 3.guard只应该依赖共享上下文(黑板和props)，不应当对父节点做任何的假设。
    /// 4.guard永远是检查当前Task的上下文，子节点的guard也不例外。
    /// 5.guard通常不应该修改数据
    /// 6.guard默认不检查取消信号，用户可实现取消信号检测节点。
    /// 7.如果guard开启了inverter内联或包含Inverter节点，Status将不能保留原始的错误码 -- 只有Sequence能精确返回错误码。
    /// </summary>
    /// <param name="guard">前置条件；可以是子节点的guard属性，也可以是条件子节点，也可以是外部的条件节点</param>
    /// <returns></returns>
    public bool Template_CheckGuard(Task<T>? guard) {
        if (guard == null) {
            return true;
        }
        // 注意：此时需要从flags读取反转标记，因为尚未运行(且guard.guard失败的情况下不会运行)
        bool inverted = (guard.flags & MASK_INVERTED_GUARD) != 0;
        try {
            // 极少情况下会有前置的前置，更推荐组合节点，更清晰；guard的guard也是检测当前上下文
            if (guard.guard != null && !Template_CheckGuard(guard.guard)) {
                guard.ctl |= MASK_DISABLE_NOTIFY;
                guard.SetCompleted(inverted ? TaskStatus.SUCCESS : TaskStatus.GUARD_FAILED, false);
                return inverted;
            }
            guard.Template_Start(this, MASK_DISABLE_NOTIFY | MASK_GUARD_BASE_OPTIONS);
            if (guard.IsSucceeded) {
                if (inverted) {
                    guard.status = TaskStatus.ERROR;
                    return false;
                }
                return true;
            }
            if (guard.IsFailed) {
                if (inverted) {
                    guard.status = TaskStatus.SUCCESS;
                    return true;
                }
                return false;
            }
            throw new IllegalStateException($"Illegal guard status {guard.status}. Guards must either succeed or fail in one step.");
        }
        finally {
            guard.UnsetControl(); // 条件类节点总是及时清理
        }
    }

    /** @return 内部使用的mask */
    private int CaptureContext(Task<T> control) {
        this.taskEntry = control.taskEntry;
        this.control = control;

        // 如果黑板不为null，则认为是control预设置的；其它属性同理
        int r = 0;
        if (this.blackboard == null) {
            this.blackboard = control.blackboard ?? throw new NullReferenceException("control.blackboard");
            r |= MASK_INHERITED_BLACKBOARD;
        }
        if (this.sharedProps == null && control.sharedProps != null) {
            this.sharedProps = control.sharedProps;
            r |= MASK_INHERITED_PROPS;
        }
        if (this.cancelToken == null) {
            this.cancelToken = control.cancelToken ?? throw new NullReferenceException("control.cancelToken");
            r |= MASK_INHERITED_CANCEL_TOKEN;
        }
        return r;
    }

    /** 释放自动捕获的上下文 -- 如果保留上次的上下文，下次执行就会出错（guard是典型） */
    private void ReleaseContext() {
        int ctl = this.ctl;
        if ((ctl & MASK_INHERITED_BLACKBOARD) != 0) {
            blackboard = default;
        }
        if ((ctl & MASK_INHERITED_PROPS) != 0) {
            sharedProps = null;
        }
        if ((ctl & MASK_INHERITED_CANCEL_TOKEN) != 0) {
            cancelToken = null;
        }
    }

    /// <summary>
    /// 设置任务的控制节点
    /// </summary>
    /// <param name="control">控制节点</param>
    /// <exception cref="Exception"></exception>
    public void SetControl(Task<T> control) {
        Debug.Assert(control != this);
        if (this == taskEntry) {
            throw new Exception();
        }
        this.taskEntry = control.taskEntry ?? throw new NullReferenceException("control.taskEntry");
        this.control = control;
    }

    /// <summary>
    /// 删除任务的控制节点(用于清理)
    /// 该方法在任务结束时并不会自动调用，因为Task上的数据可能是有用的，不能立即删除，只有用户知道是否可以清理。
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void UnsetControl() {
        if (this == taskEntry) {
            throw new Exception();
        }
        this.taskEntry = null;
        this.control = null;
        this.blackboard = default;
        this.sharedProps = null;
        this.cancelToken = null;
        this.controlData = null;
    }

    #endregion

    #region child维护

#nullable disable
    /// <summary>
    /// 1.尽量不要在运行时增删子节点（危险操作）
    /// 2.不建议将Task从一棵树转移到另一棵树，可能产生内存泄漏（引用未删除干净）
    /// </summary>
    /// <param name="task"></param>
    /// <returns>child的下标</returns>
    public int AddChild(Task<T> task) {
        CheckAddChild(task);
        return AddChildImpl(task);
    }

    /// <summary>
    /// 替换指定索引位置的child
    /// （该方法可避免Task的结构发生变化，也可以减少事件数）
    /// </summary>
    /// <param name="index">下标</param>
    /// <param name="newTask">新任务</param>
    /// <returns>旧任务，可能null</returns>
    public Task<T> SetChild(int index, Task<T> newTask) {
        CheckAddChild(newTask);
        return SetChildImpl(index, newTask);
    }

    private void CheckAddChild(Task<T> child) {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (child == this) throw new ArgumentException("add self to children");
        if (child.IsRunning) throw new ArgumentException("child is running");

        if (child.control != this) {
            // 必须先从旧的父节点上删除，但有可能是自己之前放在一边的子节点
            if (child.taskEntry != null || child.control != null) {
                throw new ArgumentException("child.control is not null");
            }
        }
    }

    /** 删除指定child */
    public bool RemoveChild(Task<T> task) {
        if (task == null) throw new ArgumentNullException(nameof(task));
        // child未启动的情况下，control可能尚未赋值，因此不能检查control来判别
        int index = IndexChild(task);
        if (index > 0) {
            RemoveChildImpl(index);
            task.UnsetControl();
            return true;
        }
        return false;
    }

    /** 删除指定索引的child */
    public Task<T> RemoveChild(int index) {
        Task<T> child = RemoveChildImpl(index);
        child.UnsetControl();
        return child;
    }

    /** 删除所有的child -- 不是个常用方法 */
    public void RemoveAllChild() {
        for (int idx = ChildCount - 1; idx >= 0; idx--) {
            RemoveChildImpl(idx).UnsetControl();
        }
    }

    /** @return index or -1 */
    public virtual int IndexChild(Task<T> task) {
        for (int idx = ChildCount - 1; idx >= 0; idx--) {
            if (GetChild(idx) == task) {
                return idx;
            }
        }
        return -1;
    }

    /** 访问所有的子节点(含hook节点) */
    public abstract void VisitChildren(TaskVisitor<T> visitor, object param);

    /** 子节点的数量（仅包括普通意义上的child，不包括钩子任务） */
    public abstract int ChildCount { get; }

    /** 获取指定索引的child */
    public abstract Task<T> GetChild(int index);

    /** @return 为child分配的index */
    protected abstract int AddChildImpl(Task<T> task);

    /** @return 索引位置旧的child */
    protected abstract Task<T> SetChildImpl(int index, Task<T> task);

    /** @return index对应的child */
    protected abstract Task<T> RemoveChildImpl(int index);
#nullable enable

    #endregion

    #region util

    /** task是否支持内联 */
    public bool IsInlinable {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & (TaskOverrides.MASK_INLINABLE | MASK_BREAK_INLINE)) == TaskOverrides.MASK_INLINABLE;
    }

    /** 任务的控制流标记 */
    public int ControlFlowOptions => ctl & MASK_CONTROL_FLOW_OPTIONS;

    /** 将task上的临时控制标记写回到flags中 */
    public void ExportControlFlowOptions() {
        int controlFlowOptions = ctl & MASK_CONTROL_FLOW_OPTIONS;
        flags &= ~MASK_CONTROL_FLOW_OPTIONS;
        flags |= controlFlowOptions;
    }

    /** 设置子节点的取消令牌 -- 会自动传播取消信号 */
    public void SetChildCancelToken(Task<T> child, CancelToken? childCancelToken) {
        if (childCancelToken != null && childCancelToken != cancelToken) {
            cancelToken.AddListener(childCancelToken);
        }
        child.cancelToken = childCancelToken;
    }

    /** 删除子节点的取消令牌 */
    public void UnsetChildCancelToken(Task<T> child) {
        CancelToken? childCancelToken = child.cancelToken;
        if (childCancelToken != null && childCancelToken != cancelToken) {
            cancelToken.RemListener(childCancelToken);
            childCancelToken.Reset();
        }
        child.cancelToken = null;
    }

    /** 停止目标任务 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Stop(Task<T>? task) {
        if (task != null && task.status == TaskStatus.RUNNING) {
            task.Stop();
        }
    }

    /** 重置目标任务 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetForRestart(Task<T>? task) {
        if (task != null && task.status != TaskStatus.NEW) {
            task.ResetForRestart();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtlBit(int mask, bool enable) {
        if (enable) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    #endregion

#nullable disable

    #region 序列化

    public Task<T> Guard {
        get => guard;
        set => guard = value;
    }

    public int Flags {
        get => flags;
        set => flags = value;
    }

    #endregion
}
}