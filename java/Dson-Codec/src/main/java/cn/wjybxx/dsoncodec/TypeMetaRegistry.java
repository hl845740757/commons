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

package cn.wjybxx.dsoncodec;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 类型元数据注册表
 * 注意：需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象） —— 在运行时通常不会变化。
 *
 * @author wjybxx
 * date - 2023/4/26
 */
@ThreadSafe
public interface TypeMetaRegistry {

    /**
     * 通过完整的类型信息查询类型元数据
     */
    @Nullable
    TypeMeta ofType(TypeInfo type);

    /**
     * 通过字符串名字找到类型信息
     */
    TypeMeta ofName(String clsName);

}