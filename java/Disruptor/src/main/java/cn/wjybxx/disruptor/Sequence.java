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
package cn.wjybxx.disruptor;

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;


/**
 * 序列，用于追踪RingBuffer和EventProcessor的进度，表示生产/消费进度。
 */
public final class Sequence {

    private static final long INITIAL_VALUE = -1L;
    private static final VarHandle VH_VALUE;

    // region padding
    @SuppressWarnings("unused")
    private long p1, p2, p3, p4, p5, p6, p7;
    // endregion

    private volatile long value;

    // region padding
    @SuppressWarnings("unused")
    private long p11, p12, p13, p14, p15, p16, p17;
    // endregion

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_VALUE = l.findVarHandle(Sequence.class, "value", long.class);
        } catch (Exception e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    public Sequence() {
        this(INITIAL_VALUE);
    }

    public Sequence(final long initialValue) {
        VH_VALUE.setRelease(this, initialValue);
    }

    /** volatile读 */
    public long getVolatile() {
        return value;
    }

    /** volatile写 - 会插入写屏障，且尝试刷新缓存 */
    public void setVolatile(final long value) {
        VH_VALUE.setVolatile(this, value);
    }

    /** acquire模式读 - 会插入读屏障 */
    public long getAcquire() {
        return (long) VH_VALUE.getAcquire(this);
    }

    /** release模式写 - 会插入写屏障，但不立即刷新缓存 */
    public void setRelease(final long value) {
        VH_VALUE.setRelease(this, value);
    }

    /** 无内存语义读 */
    public long getPlain() {
        return (long) VH_VALUE.get(this);
    }

    /** 无内存语义写 */
    public void setPlain(long value) {
        VH_VALUE.set(this, value);
    }

    /** 原子比较更新 */
    public boolean compareAndSet(final long expectedValue, final long newValue) {
        return VH_VALUE.compareAndSet(this, expectedValue, newValue);
    }

    /** 原子+1 并返回+1 后的结果 */
    public long incrementAndGet() {
        return addAndGet(1L);
    }

    /** 原子+1 并返回+1 前的结果 */
    public long getAndIncrement() {
        return getAndAdd(1L);
    }

    /** 原子-1 并返回-1 后的结果 */
    public long decrementAndGet() {
        return addAndGet(-1L);
    }

    /** 原子-1 并返回-1 前的结果 */
    public long getAndDecrement() {
        return getAndAdd(-1L);
    }

    /** 原子加上给定数并返回增加后的值 */
    public long addAndGet(final long increment) {
        final long prev = (long) VH_VALUE.getAndAdd(this, increment);
        return prev + increment;
    }

    /** 原子加上给定数并返回增加前的值 */
    public long getAndAdd(final long increment) {
        return (long) VH_VALUE.getAndAdd(this, increment);
    }

    @Override
    public String toString() {
        return Long.toString(getVolatile());
    }

}
