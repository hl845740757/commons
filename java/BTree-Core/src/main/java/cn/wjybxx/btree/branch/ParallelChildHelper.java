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

package cn.wjybxx.btree.branch;

import cn.wjybxx.btree.CancelToken;
import cn.wjybxx.btree.TaskInlineHelper;

/**
 * Q：为什么不直接叫{@code ChildHelper}？
 * A: 通常只应该在有多个运行中的子节点(含hook)的情况下才需要使用该工具类。
 *
 * @author wjybxx
 * date - 2024/7/24
 */
public class ParallelChildHelper<T> extends TaskInlineHelper<T> {

    /** 子节点的重入id */
    public int reentryId;
    /** 子节点的取消令牌 -- 应当在运行前赋值 */
    public CancelToken cancelToken;
    /** 子节点控制数据 -- 自行规划 */
    public int ctl;
    /** 用户自定义数据 */
    public Object userData;

    public void reset() {
        stopInline();
        reentryId = 0;
        ctl = 0;
        userData = null;
        if (cancelToken != null) {
            cancelToken.reset();
        }
    }
}