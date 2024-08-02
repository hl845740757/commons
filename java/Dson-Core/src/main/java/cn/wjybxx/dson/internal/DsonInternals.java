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

package cn.wjybxx.dson.internal;

/**
 * 存放一些基础的工具方法，不想定义过多的小类，减少维护量
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class DsonInternals {

    /** 上下文缓存池大小 */
    public static final int CONTEXT_POOL_SIZE = 64;
    /** 垂直制表符号 - java不支持... */
    public static final char CHAR_VERTICAL_TAB = '\u000b';

    /** 是否设置了任意bit */
    public static boolean isAnySet(int flags, int mask) {
        return (flags & mask) != 0;
    }

    /** 是否设置了mask关联的所有bit */
    public static boolean isSet(int value, int mask) {
        return (value & mask) == mask;
    }

}