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
 * 3.组件id解析重定向，请使用{@link ComponentRedirect}
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

    /** 用户自定义分组键 */
    int groupKey() default 0;

    /** 挂载路径 */
    String mountPath() default "";

    /** 自定义切面数据 -- 用于自定义解析 */
    String customData() default "";

    /**
     * 组件的超类型
     * 1.用于对齐缓存索引{@link ComponentId#index}，使得可以通过超类组件ID查询子类组件。
     * 2.指向同一个超类型的组件，共享同一个Index。
     * 3.如果实体支持同类型组件挂载多个，不可使用该特性。
     * 4.如果实体支持同类型组件挂载多个，更建议将超类定义为组件，子类型不定义为组件 —— 手动进行类型转换，否则会产生诸多问题。
     * <p>
     * 组件重定向：{@link ComponentRedirect}
     */
    Class<?> baseType() default Object.class;
}