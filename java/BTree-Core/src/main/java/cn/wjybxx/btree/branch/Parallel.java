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
package cn.wjybxx.btree.branch;

import cn.wjybxx.btree.BranchTask;
import cn.wjybxx.btree.Task;

import java.util.ArrayList;
import java.util.List;

/**
 * 并行节点基类
 * 定义该类主要说明一些注意事项，包括：
 * 1.不建议在子节点完成事件中再次驱动子节点，避免运行{@link #execute()}方法，否则可能导致其它task单帧内运行多次。
 * 2.如果有缓存数据，务必小心维护。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class Parallel<T> extends BranchTask<T> {

    /** 用于保存child的上下文等 */
    protected transient final List<ParallelChildHelper<T>> childHelpers = new ArrayList<>();

    public Parallel() {
    }

    public Parallel(List<Task<T>> children) {
        super(children);
    }

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        resetHelpers();
    }

    /** 模板类不重写enter方法，只有数据初始化逻辑 */
    @Override
    protected void beforeEnter() {
//        resetHelpers();
    }

    @Override
    protected void exit() {
        resetHelpers();
    }

    public final ParallelChildHelper<T> getChildHelper(int index) {
        return childHelpers.get(index);
    }

    /**
     * 初始化child关联的helper
     * 1.默认会设置为child的controlData，以避免反向查找开销。
     * 2.建议在enter方法中调用。
     *
     * @param allocCancelToken 是否分配取消令牌
     */
    protected final void initChildHelpers(boolean allocCancelToken) {
        List<ParallelChildHelper<T>> childHelpers = this.childHelpers;
        List<Task<T>> children = this.children;
        while (childHelpers.size() < children.size()) {
            childHelpers.add(new ParallelChildHelper<>());
        }
        for (int i = 0; i < children.size(); i++) {
            Task<T> child = children.get(i);
            ParallelChildHelper<T> childHelper = childHelpers.get(i);
            child.setControlData(childHelper);
            childHelper.reentryId = child.getReentryId();
            if (allocCancelToken && childHelper.cancelToken == null) {
                childHelper.cancelToken = cancelToken.newInstance();
            } else {
                childHelper.cancelToken = null;
            }
        }
    }

    protected final void resetHelpers() {
        // 两者长度可能不一致
        List<Task<T>> children = this.children;
        for (int i = 0; i < children.size(); i++) {
            children.get(i).setControlData(null);
        }
        List<ParallelChildHelper<T>> childHelpers = this.childHelpers;
        for (int i = 0; i < childHelpers.size(); i++) {
            childHelpers.get(i).reset();
        }
    }

    /**
     * 1.并发节点通常不需要在该事件中将自己更新为运行状态，而是应该在{@link #execute()}方法的末尾更新
     * 2.实现类可以在该方法中内联子节点
     */
    @Override
    protected void onChildRunning(Task<T> child) {

    }

}