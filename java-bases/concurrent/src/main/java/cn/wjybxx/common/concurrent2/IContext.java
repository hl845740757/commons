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

package cn.wjybxx.common.concurrent2;

import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;

/**
 * 这本质是一个共享上下文
 * 在异步和并发编程中，共享上下文是很必要的，且显式的共享优于隐式的共享。
 * 共享上下文可实现的功能：
 * 1.传递取消信号
 * 2.传递超时信息
 * 3.共享数据(K-V结果)
 * <p>
 * 在最初的设计里，取消的命令和查询也是分开的，就像future和promise的设计一样 -- 任务的执行者是只需要查询取消状态的。
 * 但是通过Future取消关联的任务已深入java开发者内心，我们必须要兼容JDK，因此取消接口必须对Future开放。
 * <p>
 * 在最初的设计里，Context只包含取消功能；如果单论取消信号的传递，Future+Promise是完全可以支持的，
 * 只是Future上的接口过多，直接使用Future+Promise会增加一些混乱。
 *
 * @author wjybxx
 * date - 2023/11/6
 */
public interface IContext {

    /** 父上下文 */
    IContext parent();

    /**
     * 任务绑定的黑板（用于数据共享）
     * <p>
     * 这里未直接实现为类似Map的读写接口，是故意的。
     * 因为提供类似Map的读写接口，会导致创建Context的开销变大，而在许多情况下是不必要的。
     * 将黑板设定为Object类型，既可以增加灵活性，也可以减少一般情况下的开销。
     * ps：一般而言，黑板需要实现递归向上查找。
     */
    Object blackboard();

    /**
     * 创建子Context，创建的子Context会在当前Context被取消时取消
     * 1.子context将沿用当前context的黑板
     * 2.子Context不是readonly的，因为需要有取消权限
     */
    default IContext newChild() {
        return newChild(blackboard());
    }

    /** @throws NullPointerException 如果黑板为null */
    IContext newChild(Object blackboard);

    /**
     * 转为只读的Context视图，cancel系列方法抛出{@link UnsupportedOperationException}
     * 其作用类似{@link IPromise#asReadonly()}，只不过由于要兼容jdk，因此不是传递给任务的执行者的。
     *
     * @return 如果当前context是只读的，则可能返回this；否则必定返回新的对象。
     */
    IContext asReadonly();

    /**
     * 由于{@link #cancel(int)}等接口暴露在顶层接口，因此需要提供查询方法。
     * 注意：readonly对黑板无约束。
     */
    boolean isReadOnly();

    // region 取消

    /**
     * 将Token置为取消状态
     *
     * @param fullCode 取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}
     * @return 如果Token已被取消，则返回旧值（大于0）；如果Token尚未被取消，则将Token更新为取消状态，并返回0。
     * @throws IllegalArgumentException      如果code小于等于0；或reason部分为0
     * @throws UnsupportedOperationException 如果context是只读的
     */
    int cancel(int fullCode);

    default int cancel() {
        return cancel(REASON_DEFAULT); // 末位1，默认情况
    }

    /**
     * 该方法主要用于兼容JDK
     *
     * @param mayInterruptIfRunning 是否可以中断目标线程；注意该参数由任务自身处理，且任务监听了取消信号才有用
     */
    default int cancel(boolean mayInterruptIfRunning) {
        return cancel(mayInterruptIfRunning ? (MASK_INTERRUPT & REASON_DEFAULT) : REASON_DEFAULT);
    }

    /** 在一段时间后发送取消命令1 */
    void cancelAfter(int cancelCode, long time, TimeUnit timeUnit);

    void cancelAfter(int cancelCode, long time, ScheduledExecutorService executorService);

    /**
     * 取消码
     * 1. 按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
     * 2. 低16位为取消原因；高16位为特殊信息
     * 3. 不为0表示已发起取消请求
     * 4. 取消时至少赋值一个信息，reason通常应该赋值
     */
    int cancelCode();

    /**
     * 是否已发出取消指令
     * 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
     */
    default boolean isCancelling() {
        return cancelCode() != 0;
    }

    /**
     * 取消的原因
     * (1~10为底层使用，10以上为用户自定义)
     */
    default int reason() {
        return (cancelCode() & MASK_REASON);
    }

    /** 取消的紧急程度 */
    default int urgencyDegree() {
        return (cancelCode() & MASK_DEGREE) >>> 16;
    }

    /** 取消指令中是否要求了中断线程 */
    default boolean isInterruptible() {
        return (cancelCode() & MASK_INTERRUPT) != 0;
    }

    /**
     * 添加的action将在Context收到取消信号时执行
     * （支持取消的的任务可以监听取消信号以唤醒线程）
     */
    void onCancelRequested(Consumer<? super IContext> action);

    // endregion

    // region 常量
    /** 原因的掩码 */
    int MASK_REASON = 0xFFFF;
    /** 紧迫程度的掩码（4it）-- 0表示未指定 */
    int MASK_DEGREE = 0x000F_0000;
    /** 预留4bit */
    int MASK_REVERSED = 0x00F0_0000;
    /** 中断的掩码 （1bit） */
    int MASK_INTERRUPT = 1 << 24;

    int OFFSET_REASON = 0;
    int OFFSET_DEGREE = 16;
    int OFFSET_INTERRUPT = 24;

    /** 默认原因 */
    int REASON_DEFAULT = 1;
    /** 超时 */
    int REASON_TIMEOUT = 2;
    /** Executor关闭 */
    int REASON_SHUTDOWN = 3;
    /** 进程关闭(应用关闭) */
    int REASON_APP_SHUTDOWN = 4;
    /** 用户错误 -- 用户（任务的创建者）出现错误，取消关联的任务；如果是调度错误，Future会进入失败完成状态 */
    int REASON_USER_ERROR = 5;

    // endregion

}