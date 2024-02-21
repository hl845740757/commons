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
 * 只包含取消令牌的context，该类实例通常不暴露给用户的Action
 *
 * @author wjybxx
 * date - 2024/2/21
 */
public class MiniContext implements IContext {

    private static final MiniContext SHARABLE = new MiniContext(ICancelToken.NONE);

    private final ICancelToken cancelToken;

    private MiniContext(ICancelToken cancelToken) {
        this.cancelToken = cancelToken == null ? ICancelToken.NONE : cancelToken;
    }

    public static MiniContext create(ICancelToken cancelToken) {
        if (cancelToken == null || cancelToken == ICancelToken.NONE) {
            return SHARABLE;
        }
        return new MiniContext(cancelToken);
    }

    @Override
    public Object state() {
        return null;
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

    @Override
    public IContext toSharable() {
        return SHARABLE;
    }

}