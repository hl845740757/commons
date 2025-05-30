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

package cn.wjybxx.dsoncodec;

import cn.wjybxx.base.EnumLite;

/**
 * 字典的编码策略
 *
 * <h3>key限制</h3>
 * 1.当字典的Key需要转换为字符串时，仅支持：int32、int64、enum、string.
 * 2.当字典的Key需要转换为字符串时，<b>Key的运行时类型必须和声明类型相同</b>。
 * 3.当字典的key不是约定的类型时，仅可以使用{@link #ARRAY}和{@link #PAIR_AS_ARRAY}两种策略。
 *
 * <h3>字典的本质是数组</h3>
 * 本质上讲，Map是数组，而不是普通的Object，因为标准的Map是允许复杂key的，因此Map默认应该序列化为数组。但存在两个特殊的场景：
 * 1.与脚本语言通信
 * 脚本语言通常没有静态语言中的字典结构，由object充当，但object不支持复杂的key作为键，通常仅支持数字和字符串作为key。
 * 因此在与脚本语言通信时，要求将Map序列化为简单的object。
 * 2.配置文件读写
 * 配置文件通常是无类型的，因此读取到内存中通常是一个字典结构；程序在输出配置文件时，同样需要将字典结构输出为object结构。
 *
 * @author wjybxx
 * date - 2025/5/30
 */
public enum MapEncodePolicy implements EnumLite {

    /**
     * 将字典写为普通数组
     * {@code
     * [K, V, K2, V2, K3, V3...]
     * }
     */
    ARRAY(0),

    /**
     * 将字典写为普通文档
     * {@code
     * { K1: V1, K2: V2, K3: V3... }
     * }
     */
    DOCUMENT(1),

    /**
     * 将Pair写为子数组
     * {@code
     * [[K1, V1], [K2, V2], [K3, V3]...]
     * }
     */
    PAIR_AS_ARRAY(2),

    /**
     * 将Pair写为子文档
     * {@code
     * [{K1: V1}, {K2: V2}, {K3: V3}...]
     * }
     */
    PAIR_AS_DOCUMENT(3);

    private final int number;

    MapEncodePolicy(int number) {
        this.number = number;
    }

    @Override
    public int getNumber() {
        return number;
    }
}