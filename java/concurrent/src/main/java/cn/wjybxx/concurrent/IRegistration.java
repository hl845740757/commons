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
 * 注册监听器产生的句柄。
 * 1. 用户可通过{@link #close()}取消注册。
 * 2. 不建议复用该对象。
 * <p>
 * 友情提醒：如果在句柄接口上提供了获取主题(Subject)的接口，转发实现一定要小心，小心封装泄漏。
 *
 * @author wjybxx
 * date - 2024/1/12
 */
public interface IRegistration extends AutoCloseable {

    /**
     * {@inheritDoc}
     * 重复调用不应该抛出异常 -- 视为已关闭。
     */
    @Override
    void close();

}