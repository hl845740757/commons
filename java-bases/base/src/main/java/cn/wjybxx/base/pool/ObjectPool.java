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

package cn.wjybxx.base.pool;


import java.util.Collection;
import java.util.function.Supplier;

/**
 * 简单对象池
 * (其实acquire和release是比较合适的命名; rent和return通常也是一对)
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface ObjectPool<T> extends Supplier<T> {

    /** 该接口仅用于适配，一般业务不建议使用 */
    @Override
    default T get() {
        return rent();
    }

    /**
     * 从池中租借一个对象
     *
     * @return 如果池中有可用的对象，则返回缓存的对象，否则返回一个新的对象
     */
    T rent();

    /**
     * 将指定的对象放入池中 - 重置策略却决于{@link ResetPolicy}。
     *
     * @param object 要回收的对象
     */
    void returnOne(T object);

    /**
     * 将指定的对象放入池中 - 重置策略却决于{@link ResetPolicy}。
     *
     * @param objects 要回收的对象
     */
    default void returnAll(Collection<? extends T> objects) {
        objects.forEach(e -> {
            if (e != null) returnOne(e);
        });
    }

    /**
     * 释放此池中的所有对象
     * （可空实现）
     */
    void freeAll();

}