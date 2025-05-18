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

import javax.annotation.Nullable;
import java.util.Objects;

/**
 * 可关闭资源的句柄
 *
 * @author wjybxx
 * date - 2025/3/28
 */
public final class Registration implements IRegistration {

    public static final Registration CLOSED = new Registration(null, 0);

    private final IPooledCloseable res;
    private final long rid;

    public Registration(@Nullable IPooledCloseable res, long rid) {
        this.res = res;
        this.rid = rid;
    }

    /** 是否绑定了资源对象 */
    public boolean hasResource() {
        return res != null;
    }

    /** 资源对象是否已销毁 -- 是否已退出当前生命周期 */
    public boolean isClosed() {
        return res == null || res.isClosed(rid);
    }

    @Override
    public void close() {
        if (res != null) res.close(rid);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Registration that = (Registration) o;
        return rid == that.rid
                && res == that.res; // 引用相等
    }

    @Override
    public int hashCode() {
        int result = Objects.hashCode(res);
        result = 31 * result + Long.hashCode(rid);
        return result;
    }
}