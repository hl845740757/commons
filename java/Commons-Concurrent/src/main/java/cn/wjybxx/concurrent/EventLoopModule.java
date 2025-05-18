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
import cn.wjybxx.base.fx.ComponentStatus;

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
    private ComponentId<?> cid;
    private ComponentStatus status = ComponentStatus.NEW;

    public EventLoopModule() {
    }

    // region internal

    /** 设置模块的状态，通过该接口可以实现自定义流程 */
    final void setStatus(ComponentStatus status) {
        this.status = status;
    }

    /** 设置EventLoop 会触发{@link #onReady()}事件 */
    final void setEventLoop(IEventLoop eventLoop) {
        Objects.requireNonNull(eventLoop, "eventLoop");
        if (this.eventLoop != null) {
            throw new IllegalStateException("already bind");
        }
        this.eventLoop = eventLoop;
        this.status = ComponentStatus.READY;
        this.onReady();
        // 非脚本组件，直接进入完成状态
        if (getCid().kind != ComponentKind.SCRIPT) {
            this.status = ComponentStatus.STOPPED;
        }
    }

    /** 调用{@link #onDestroy()}方法 */
    final Throwable invokeDestroy() {
        try {
            status = ComponentStatus.DESTROYED;
            onDestroy();
            return null;
        } catch (Throwable ex) {
            return ex;
        } finally {
            eventLoop = null;
        }
    }

    /** 调用{@link #start()}方法 */
    final Throwable invokeStart() {
        assert isScript();
        try {
            status = ComponentStatus.STARTING;
            start();
            status = ComponentStatus.RUNNING;
            return null;
        } catch (Throwable ex) {
            return ex;
        }
    }

    /** 调用{@link #stop()}方法 */
    final Throwable invokeStop() {
        assert isScript();
        try {
            status = ComponentStatus.STOPPING;
            stop();
            return null;
        } catch (Throwable ex) {
            return ex;
        } finally {
            status = ComponentStatus.STOPPED;
        }
    }

    /** 是否是脚本组件 */
    private boolean isScript() {
        return getCid().kind == ComponentKind.SCRIPT;
    }
    // endregion

    // region 默认实现

    @Nonnull
    @Override
    public final ComponentId<?> getCid() {
        if (cid == null) {
            cid = Objects.requireNonNull(parseCid(), "cid");
        }
        return cid;
    }

    /** 允许在自动解析组件id前设置组件id */
    @Override
    public final void setCid(ComponentId<?> cid) {
        if (status != ComponentStatus.NEW) {
            throw new IllegalStateException();
        }
        this.cid = cid;
    }

    /** 解析组件id -- 允许重写方法，从另外的池解析组件id */
    protected ComponentId<?> parseCid() {
        return IEventLoopModule.GLOBAL.valueOf(getClass());
    }

    @Override
    public IEventLoop getEntity() {
        return eventLoop;
    }

    @Override
    public final ComponentStatus getStatus() {
        return status;
    }

    // endregion

}