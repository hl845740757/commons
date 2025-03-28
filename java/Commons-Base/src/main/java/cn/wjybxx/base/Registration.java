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

package cn.wjybxx.base;

/**
 * 可关闭资源的句柄
 *
 * @author wjybxx
 * date - 2025/3/28
 */
public final class Registration implements IRegistration {

    public static final Registration CLOSED = new Registration(null, 0);

    private final IPooledCloseable res;
    private final int rid;

    @Override
    public void close() {
        if (res != null) res.close(rid);
    }

    public Registration(IPooledCloseable res, int rid) {
        this.res = res;
        this.rid = rid;
    }
}