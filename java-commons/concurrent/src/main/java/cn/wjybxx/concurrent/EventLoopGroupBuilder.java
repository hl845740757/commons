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

package cn.wjybxx.concurrent;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public class EventLoopGroupBuilder {

    private int numChildren = 1;
    private EventLoopFactory eventLoopFactory;
    private EventLoopChooserFactory chooserFactory;
    private Runnable terminationHook;

    //

    /** 创建一个默认的builder - 最终将构建{@link FixedEventLoopGroup} */
    public static EventLoopGroupBuilder newBuilder() {
        return new EventLoopGroupBuilder();
    }
    //

    public EventLoopGroup build() {
        return new DefaultFixedEventLoopGroup(this);
    }

    public int getNumChildren() {
        return numChildren;
    }

    public EventLoopGroupBuilder setNumChildren(int numChildren) {
        this.numChildren = numChildren;
        return this;
    }

    public EventLoopFactory getEventLoopFactory() {
        return eventLoopFactory;
    }

    public EventLoopGroupBuilder setEventLoopFactory(EventLoopFactory eventLoopFactory) {
        this.eventLoopFactory = eventLoopFactory;
        return this;
    }

    public EventLoopChooserFactory getChooserFactory() {
        return chooserFactory;
    }

    public EventLoopGroupBuilder setChooserFactory(EventLoopChooserFactory chooserFactory) {
        this.chooserFactory = chooserFactory;
        return this;
    }

    public Runnable getTerminationHook() {
        return terminationHook;
    }

    /**
     * @param terminationHook 线程组终止时的钩子，注意线程安全性问题
     */
    public EventLoopGroupBuilder setTerminationHook(Runnable terminationHook) {
        this.terminationHook = terminationHook;
        return this;
    }
}