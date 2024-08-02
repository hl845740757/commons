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

import cn.wjybxx.base.ObjectUtils;

import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;

/**
 * {@link ClassName}的解析化池，
 * 解析字符串为结构体的过程的开销还是比较大的，缓存解析结果可以降低内存分配，提高效率。
 *
 * @author wjybxx
 * date - 2024/5/16
 */
@ThreadSafe
public final class ClassNamePool {

    /** 字符串解析结果的缓存 —— ClassName的解析缓存则存储在{@link TypeMeta} */
    private final ConcurrentHashMap<String, ClassName> string2StructDic = new ConcurrentHashMap<>();

    /** 解析Dson风格的字符串名为结构化名字 */
    public ClassName parse(String clsName) {
        Objects.requireNonNull(clsName);
        ClassName className = string2StructDic.get(clsName);
        if (className != null) {
            return className;
        }
        // 程序生成的clsName通常是紧凑的，不包含空白字符(缩进)的，因此可以安全缓存；
        // 如果clsName包含空白字符，通常是用户手写的，缓存有一定的风险性 —— 可能产生恶意缓存
        if (ObjectUtils.containsWhitespace(clsName)) {
            return ClassName.parse(clsName);
        }
        className = ClassName.parse(clsName);
        string2StructDic.put(clsName, className);
        return className;
    }
}