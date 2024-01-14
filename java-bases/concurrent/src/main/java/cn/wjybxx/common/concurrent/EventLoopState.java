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

package cn.wjybxx.common.concurrent;

/**
 * EventLoop的状态 -- UniExecutor也使用该枚举。
 *
 * @author wjybxx
 * date - 2024/1/14
 */
public enum EventLoopState {

    /** 初始状态 -- 已创建，但尚未启动 */
    UNSTARTED(0),
    /** 启动中 */
    STARTING(1),
    /** 启动成功，运行中 */
    RUNNING(2),
    /** 正在关闭 */
    SHUTTING_DOWN(3),
    /** 二阶段关闭状态，终止前的清理工作 */
    SHUTDOWN(4),
    /** 终止 */
    TERMINATED(5);

    public final int number;

    EventLoopState(int number) {
        this.number = number;
    }

    public static EventLoopState valueOf(int number) {
        return switch (number) {
            case 0 -> UNSTARTED;
            case 1 -> STARTING;
            case 2 -> RUNNING;
            case 3 -> SHUTTING_DOWN;
            case 4 -> SHUTDOWN;
            case 5 -> TERMINATED;
            default -> throw new IllegalArgumentException("invalid number: " + number);
        };
    }

    /** 初始状态，未启动状态 */
    public static final int ST_UNSTARTED = 0;
    /** 启动中 */
    public static final int ST_STARTING = 1;
    /** 运行状态 */
    public static final int ST_RUNNING = 2;
    /** 正在关闭状态 */
    public static final int ST_SHUTTING_DOWN = 3;
    /** 已关闭状态，正在进行最后的清理 */
    public static final int ST_SHUTDOWN = 4;
    /** 终止状态 */
    public static final int ST_TERMINATED = 5;

}