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

package cn.wjybxx.dsoncodec.annotations;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 主要用于为引入的外部库中的Bean自动生成Codec。
 * 1.字段的类型就是我们要自动生成Codec的类型，泛型等信息会被忽略。
 * 2.仅适用简单的Bean，复杂的Bean还是需要用户自行实现，或使用{@link DsonCodecLinkerBean}。
 * 2.1 目标Bean必须包含无参构造函数；
 * 2.2 目标Bean的transient字段将被忽略；
 * 2.3 目标Bean要序列化的字段必须包含getter/setter；
 *
 * @author wjybxx
 * date - 2023/12/10
 */
@Target(ElementType.FIELD)
@Retention(RetentionPolicy.SOURCE)
public @interface DsonCodecLinker {

    /** 为类型附加的信息 */
    DsonSerializable props() default @DsonSerializable;

}