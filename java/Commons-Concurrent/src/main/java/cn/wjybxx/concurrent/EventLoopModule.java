/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

package cn.wjybxx.concurrent;

import cn.wjybxx.base.fx.ComponentId;
import cn.wjybxx.base.fx.ComponentKind;
import cn.wjybxx.base.fx.Status;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2025/3/26
 */
@NotThreadSafe
public abstract class EventLoopModule implements IEventLoopModule {

    private IEventLoop eventLoop;
    private Status status = Status.NEW;
    private boolean enable = true;
    private ComponentId<?> cid;

    public EventLoopModule() {
    }

    // region internal

    /** 收到修正模块的状态 */
    final void setStatus(Status status) {
        this.status = status;
    }

    /** 设置EventLoop 会触发{@link #onReady()}事件 */
    final void setEventLoop(IEventLoop eventLoop) {
        Objects.requireNonNull(eventLoop, "eventLoop");
        if (this.eventLoop != null) {
            throw new IllegalStateException("already bind");
        }
        getCid(); // 确保组件id完成初始化
        this.eventLoop = eventLoop;
        this.status = Status.READY;
        this.onReady();
        // 非脚本组件，直接进入完成状态
        if (getCid().kind != ComponentKind.SCRIPT) {
            this.status = Status.STOPPED;
        }
    }

    /** 调用{@link #onDestroy()}方法 */
    final Throwable invokeDestroy() {
        try {
            status = Status.DESTROYED;
            onDestroy();
            return null;
        } catch (Throwable ex) {
            return ex;
        }
    }

    /** 调用{@link #start()}方法 */
    final Throwable invokeStart() {
        assert isScript();
        try {
            status = Status.STARTING;
            start();
            status = Status.RUNNING;
            return null;
        } catch (Throwable ex) {
            return ex;
        }
    }

    /** 调用{@link #stop()}方法 */
    final Throwable invokeStop() {
        assert isScript();
        try {
            status = Status.STOPPING;
            stop();
            return null;
        } catch (Throwable ex) {
            return ex;
        } finally {
            status = Status.STOPPED;
        }
    }

    /** 是否是脚本组件 */
    private boolean isScript() {
        return getCid().kind == ComponentKind.SCRIPT;
    }
    // endregion

    // region 默认实现

    /** 允许在自动解析组件id前设置组件id */
    public final void setCid(ComponentId<?> cid) {
        if (this.cid != null) {
            throw new IllegalStateException();
        }
        this.cid = cid; // null是安全的
    }

    @Nonnull
    @Override
    public final ComponentId<?> getCid() {
        if (cid == null) {
            cid = Objects.requireNonNull(parseCid(), "cid");
        }
        return cid;
    }

    /** 解析组件id -- 允许重写方法，从另外的池解析组件id */
    protected ComponentId<?> parseCid() {
        return EventLoopUtils.GLOBAL.valueOf(getClass());
    }

    @Override
    public final Status getStatus() {
        return status;
    }

    @Override
    public IEventLoop getEntity() {
        return eventLoop;
    }

    @Override
    public boolean isEnable() {
        return enable;
    }

    @Override
    public void setEnable(boolean enable) {
        this.enable = enable;
    }
    // endregion

}