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

import javax.annotation.Nonnull;

/**
 * 满足最小需要的Context实现。
 *
 * @author wjybxx
 * date - 2024/2/21
 */
public final class MiniContext implements IContext {

    public static final MiniContext SHARABLE = new MiniContext(null, ICancelToken.NONE);

    private final ICancelToken cancelToken;
    private final Object state;

    private MiniContext(Object state, ICancelToken cancelToken) {
        this.state = state;
        this.cancelToken = cancelToken == null ? ICancelToken.NONE : cancelToken;
    }

    public static MiniContext ofState(Object state) {
        if (state == null) return SHARABLE;
        return new MiniContext(state, null);
    }

    public static MiniContext ofState(Object state, ICancelToken cancelToken) {
        return new MiniContext(state, cancelToken);
    }

    public static MiniContext ofCancelToken(ICancelToken cancelToken) {
        if (cancelToken == ICancelToken.NONE) return SHARABLE;
        return new MiniContext(null, cancelToken);
    }

    @Override
    public Object state() {
        return state;
    }

    @Nonnull
    @Override
    public ICancelToken cancelToken() {
        return cancelToken;
    }

    @Override
    public Object blackboard() {
        return null;
    }

    @Override
    public Object sharedProps() {
        return null;
    }

}