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

import cn.wjybxx.btree.branch.SingleRunningChildBranch;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 该注解用于表示一个task是可以被内联的，
 * 1.适用于{@link SingleRunningChildBranch}和{@link Decorator}
 * 2.该注解不继承，必须在类上显式定义才可以生效。
 *
 * <h3>内联条件</h3>
 * 一个控制是否可被内联，需要满足以下条件：
 * 1.最多只能有一个运行中的子节点，且不可以有运行中的钩子节点。
 * 2.心跳逻辑中不会主动取消子节点执行 -- 即心跳逻辑只是简单驱动子节点运行。
 * 3.不能有特殊的事件处理逻辑，一定是直接派发给子节点。
 *
 * <p>
 * Q：为什么使用注解，而不是虚方法？
 * A：强调是否可内联是类型的信息，与实例状态无关。
 *
 * @author wjybxx
 * date - 2024/7/24
 */
@Target(ElementType.TYPE)
@Retention(RetentionPolicy.RUNTIME)
public @interface TaskInlinable {

}