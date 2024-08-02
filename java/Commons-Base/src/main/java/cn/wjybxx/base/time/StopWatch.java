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

package cn.wjybxx.base.time;

import javax.annotation.concurrent.NotThreadSafe;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * 停表 -- 用于监测每一步的耗时和总耗时。
 * 示例：
 * <pre>{@code
 *  public void execute() {
 *      // 创建一个已启动的计时器
 *      final StopWatch stopWatch = StopWatch.createStarted("execute");
 *
 *      doSomethingA();
 *      stopWatch.logStep("step1");
 *
 *      doSomethingB();
 *      stopWatch.logStep("step2");
 *
 *      doSomethingC();
 *      stopWatch.logStep("step3");
 *
 *      doSomethingD();
 *      stopWatch.logStep("step4");
 *
 *      // 输出日志
 *      logger.info(stopWatch.getLog());
 *  }
 * }
 * </pre>
 *
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("unused")
@NotThreadSafe
public class StopWatch {

    /** 停表的名字 */
    private final String name;
    /** 运行状态 */
    private State state = State.UNSTARTED;
    /** 启动的时间戳 -- start和resume时更新；打点时也更新 */
    private long startTimeNanos;
    /** 总耗时 */
    private long elapsedNanos;

    /** 当前步骤已耗时 */
    private long stepElapsedNanos;
    /** 历史步骤耗时 */
    private final List<Item> itemList = new ArrayList<>();

    /**
     * @param name 推荐命名格式{@code ClassName:MethodName}
     */
    public StopWatch(String name) {
        this.name = Objects.requireNonNull(name, "name");
    }

    /** 创建一个停表 */
    public static StopWatch create(String name) {
        return new StopWatch(name);
    }

    /** 创建一个已启动的停表 */
    public static StopWatch createStarted(String name) {
        final StopWatch sw = new StopWatch(name);
        sw.start();
        return sw;
    }

    /** 停表的名字 */
    public String getName() {
        return name;
    }

    /** 停表是否已启动，且尚未停止 */
    public boolean isStarted() {
        return state == State.RUNNING || state == State.SUSPENDED;
    }

    /** 停表是否处于运行状态 */
    public boolean isRunning() {
        return state == State.RUNNING;
    }

    /** 停表是否处于挂起/暂停状态 */
    public boolean isSuspended() {
        return state == State.SUSPENDED;
    }

    /** 停表是否已停止 */
    public boolean isStopped() {
        return state == State.STOPPED;
    }

    // region 生命周期

    /**
     * 开始计时。
     * 重复调用start之前，必须调用{@link #reset()}
     */
    public void start() {
        if (isStarted()) {
            throw new IllegalStateException("Stopwatch is running. ");
        }
        state = State.RUNNING;
        startTimeNanos = System.nanoTime();
        elapsedNanos = stepElapsedNanos = 0;
        itemList.clear();
    }

    /**
     * 记录该步骤的耗时
     *
     * @param stepName 该步骤的名称
     */
    public void logStep(String stepName) {
        Objects.requireNonNull(stepName, "stepName");
        if (state != State.RUNNING) {
            throw new IllegalStateException("Stopwatch is not running. ");
        }
        long delta = System.nanoTime() - startTimeNanos;
        startTimeNanos += delta; // 避免再次读取时间戳
        elapsedNanos += delta;
        stepElapsedNanos += delta;

        itemList.add(new Item(stepName, stepElapsedNanos));
        stepElapsedNanos = 0;
    }

    /** 暂停计时 */
    public void suspend() {
        if (!isStarted()) {
            throw new IllegalStateException("Stopwatch must be started to suspend. ");
        }
        if (state == State.RUNNING) {
            long delta = System.nanoTime() - startTimeNanos;
            state = State.SUSPENDED;
            elapsedNanos += delta;
            stepElapsedNanos += delta;
        }
    }

    /** 恢复计时 */
    public void resume() {
        if (!isStarted()) {
            throw new IllegalStateException("Stopwatch must be started to resume. ");
        }
        if (state == State.SUSPENDED) {
            state = State.RUNNING;
            startTimeNanos = System.nanoTime();
        }
    }

    /** 停止计时 */
    public void stop() {
        stop(null);
    }

    /**
     * 停止计时。
     * 停止计时后，{@link #elapsed()}将获得一个稳定的时间值。
     *
     * @param stepName 最后一步的名字，如果为null则不记录
     */
    public void stop(String stepName) {
        if (!isStarted()) {
            return;
        }
        if (state == State.RUNNING) {
            long delta = System.nanoTime() - startTimeNanos;
            elapsedNanos += delta;
            stepElapsedNanos += delta;
            if (stepName != null) {
                itemList.add(new Item(stepName, stepElapsedNanos));
                stepElapsedNanos = 0;
            }
        }
        state = State.STOPPED;
    }

    /**
     * 重置停表
     * 注意：为了安全起见，请要么在代码的开始重置，要么在finally块中重置。
     */
    public void reset() {
        if (state == State.UNSTARTED) {
            return;
        }
        state = State.UNSTARTED;
        startTimeNanos = 0;
        elapsedNanos = stepElapsedNanos = 0;
        itemList.clear();
    }

    /**
     * {@link #reset()}和{@link #start()}的快捷方法
     */
    public void restart() {
        reset();
        start();
    }

    // endregion

    // region 获取耗时

    /** 获取开始到现在消耗的总时间 */
    public Duration elapsed() {
        return Duration.ofNanos(elapsedNanos());
    }

    /** 获取开始到现在消耗的总时间 */
    public long elapsed(TimeUnit desiredUnit) {
        return desiredUnit.convert(elapsedNanos(), TimeUnit.NANOSECONDS);
    }

    /** 获取当前步骤已消耗的时间 */
    public Duration stepElapsed() {
        return Duration.ofNanos(stepElapsedNanos());
    }

    /** 获取当前步骤已消耗的时间 */
    public long stepElapsed(TimeUnit desiredUnit) {
        return desiredUnit.convert(stepElapsedNanos(), TimeUnit.NANOSECONDS);
    }

    /** 获取当前已有的步骤耗时信息 */
    public List<Map.Entry<String, Duration>> listStepElapsed() {
        List<Map.Entry<String, Duration>> result = new ArrayList<>(itemList.size());
        for (Item item : itemList) {
            result.add(Map.entry(item.stepName, Duration.ofNanos(item.elapsedNanos)));
        }
        return result;
    }

    private long elapsedNanos() {
        if (state == State.RUNNING) {
            return elapsedNanos + (System.nanoTime() - startTimeNanos);
        } else {
            return elapsedNanos;
        }
    }

    private long stepElapsedNanos() {
        if (state == State.RUNNING) {
            return stepElapsedNanos + (System.nanoTime() - startTimeNanos);
        } else {
            return stepElapsedNanos;
        }
    }
    // endregion

    /**
     * 获取按照时间消耗排序后的log。
     * 注意：可以在不调用{@link #stop()}的情况下调用该方法。
     * (获得了一个规律，也失去了一个规律，可能并不如未排序的log看着舒服)
     */
    public String getSortedLog() {
        if (!itemList.isEmpty()) {
            // 排序开销还算比较小
            ArrayList<Item> copiedItems = new ArrayList<>(itemList);
            copiedItems.sort(null);
            return toString(copiedItems);
        }
        return toString(itemList);
    }

    /**
     * 获取最终log。
     */
    public String getLog() {
        return toString(itemList);
    }

    /**
     * 格式: StopWatch[name={name}ms][a={a}ms,b={b}ms...]
     * 1. StepWatch为标记，方便检索。
     * 2. {@code {x}}表示x的耗时。
     * 3. 前半部分为总耗时，后半部分为各步骤耗时。
     * <p>
     * Q: 为什么重写{@code toString}？
     * A: 在输出日志的时候，我们可能常常使用占位符，那么延迟构建内容就是必须的，这要求我们实现{@code toString()}。
     */
    @Override
    public String toString() {
        return toString(itemList);
    }

    /** @param itemList 避免排序修改数据 */
    private String toString(List<Item> itemList) {
        StringBuilder sb = new StringBuilder(32);
        // 总耗时 - 此时可能正在运行
        sb.append("StopWatch[").append(name).append('=')
                .append(elapsedNanos() / TimeUtils.NANOS_PER_MILLI)
                .append("ms]");
        // 每个步骤耗时
        sb.append('[');
        for (int i = 0; i < itemList.size(); i++) {
            final Item item = itemList.get(i);
            if (i > 0) {
                sb.append(',');
            }
            sb.append(item.stepName).append('=')
                    .append(item.elapsedNanos / TimeUtils.NANOS_PER_MILLI)
                    .append("ms");
        }
        sb.append(']');
        return sb.toString();
    }

    private enum State {
        /** 未启动 */
        UNSTARTED,
        /** 运行中 */
        RUNNING,
        /** 挂起 */
        SUSPENDED,
        /** 已停止 */
        STOPPED;
    }

    private static class Item implements Comparable<Item> {

        final String stepName;
        final long elapsedNanos;

        Item(String stepName, long elapsedNanos) {
            this.stepName = stepName;
            this.elapsedNanos = elapsedNanos;
        }

        @Override
        public int compareTo(Item that) {
            final int timeCompareResult = Long.compare(elapsedNanos, that.elapsedNanos);
            if (timeCompareResult != 0) {
                // 时间逆序
                return -1 * timeCompareResult;
            }
            // 字母自然序
            return stepName.compareTo(that.stepName);
        }
    }

}