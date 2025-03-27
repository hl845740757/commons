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

package cn.wjybxx.base.fx;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 默认的组件id定义注解
 * 1.该注解不会被继承，使用子类的Class查询得到的将是另一个组件id。
 * 2.可以通过额外的注解附加信息，需要定制解析器
 *
 * @author wjybxx
 * date - 2025/3/26
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.TYPE)
public @interface ComponentDefine {

    /** 组件的名字 -- 默认使用{@link Object#getClass()}类的简单名 */
    String name() default "";

    /** 组件类型 -- 默认为脚本类型，即所有方法都生效 */
    ComponentKind kind() default ComponentKind.SCRIPT;

    /** 是否是共享组件 */
    boolean shared() default false;

    /** 最大组件数 */
    int maxCount() default 1;

    /** 用户自定义flags */
    int flags() default 0;

    /** 自定义切面数据 -- 用于自定义解析 */
    String customData() default "";
}