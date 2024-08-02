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

package cn.wjybxx.btree;

/**
 * @author wjybxx
 * date - 2024/7/14
 */
public interface ICancelTokenListener {

    /**
     * 该方法在取消令牌收到取消信号时执行
     *
     * @param cancelToken 收到取消信号的令牌
     */
    void onCancelRequested(ICancelToken cancelToken);
}