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
 * 被缓存的事件对象
 * 1.用于Disruptor或类似的系统，当我们缓存对象时，更适合将字段展开以提高内存利用率
 * 2.这只是个简单的数据传输对象，getter/setter什么的不必要
 * 3.实现{@link Runnable}以支持直接提交到{@link java.util.concurrent.Executor}
 * 4.支持{@link EventLoopAgent}的都将支持该事件。
 * <p>
 * PS:如果要支持判断是否是批量处理的最后一个事件，可以在这里添加字段。
 *
 * @author wjybxx
 * date 2023/4/10
 */
public final class RingBufferEvent implements IAgentEvent {

    private int type = TYPE_INVALID;
    public Object obj0;
    public int options;

    // 扩展字段
    public int intVal1;
    public int intVal2;
    public long longVal1;
    public long longVal2;
    public Object obj1;
    public Object obj2;

    public RingBufferEvent copy() {
        RingBufferEvent event = new RingBufferEvent();
        event.copyFrom(this);
        return event;
    }

    public void copyFrom(RingBufferEvent src) {
        this.type = src.type;
        this.obj0 = src.obj0;

        // 扩展字段
        this.intVal1 = src.intVal1;
        this.intVal2 = src.intVal2;
        this.longVal1 = src.longVal1;
        this.longVal2 = src.longVal2;
        this.obj1 = src.obj1;
        this.obj2 = src.obj2;
    }

    @Override
    public void clean() {
        type = TYPE_INVALID;
        options = 0;
        obj0 = null;

        obj1 = null;
        obj2 = null;
    }

    @Override
    public void cleanAll() {
        clean();
        intVal1 = 0;
        intVal2 = 0;
        longVal1 = 0;
        longVal2 = 0;
    }

    @Override
    public int getType() {
        return type;
    }

    @Override
    public void setType(int type) {
        this.type = type;
    }

    @Override
    public int getOptions() {
        return options;
    }

    @Override
    public void setOptions(int options) {
        this.options = options;
    }

    @Override
    public Object getObj0() {
        return obj0;
    }

    @Override
    public void setObj0(Object obj0) {
        this.obj0 = obj0;
    }

    @Override
    public String toString() {
        return "RingBufferEvent{" +
                "type=" + type +
                ", obj0=" + obj0 +
                ", intVal1=" + intVal1 +
                ", intVal2=" + intVal2 +
                ", longVal1=" + longVal1 +
                ", longVal2=" + longVal2 +
                ", obj1=" + obj1 +
                ", obj2=" + obj2 +
                '}';
    }

}