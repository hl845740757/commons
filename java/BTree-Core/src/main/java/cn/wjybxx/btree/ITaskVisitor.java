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

package cn.wjybxx.btree;

/**
 * Task访问器，用于访问Task的内部结构。
 * <p>
 * 1.钩子节点指的是不在ChildCount计数中的Task。
 * 2.新版本增强了访问者模式，使得构建Debug视图成为可能。
 * 3.访问器在访问过程中不能导致Task产生状态迁移，即不能使Task进入完成状态。
 *
 * @author wjybxx
 * date - 2024/9/4
 */
public interface ITaskVisitor<T> {

    /**
     * 访问普通子节点
     *
     * @param child 子节点
     * @param index 子节点下标
     * @param param 用户参数
     */
    void visitChild(Task<? extends T> child, int index, Object param);

    /**
     * 访问钩子节点
     *
     * @param name  钩子的名字
     * @param child 钩子节点
     * @param param 用户参数
     */
    void visitHook(String name, Task<? extends T> child, Object param);

    /**
     * 访问List类型钩子节点
     *
     * @param name  钩子的名字
     * @param child 钩子的子节点
     * @param index 钩子子节点索引
     * @param param 用户参数
     */
    void visitList(String name, Task<? extends T> child, int index, Object param);

    /**
     * 访问字典类型钩子节点
     *
     * @param name  钩子的名字
     * @param child 钩子的子节点
     * @param key   字典key
     * @param param 用户参数
     */
    <K> void visitMap(String name, Task<? extends T> child, K key, Object param);

}