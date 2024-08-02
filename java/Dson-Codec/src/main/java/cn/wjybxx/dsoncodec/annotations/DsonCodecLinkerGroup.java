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
 * 定义一组要自动生成Codec的类
 * (表示当前类是一个配置文件)
 * <p>
 * 1.每一个字段表示一个需要序列化的类型。
 * 2.需要为目标类型定义特殊属性时，可在字段上使用{@link DsonCodecLinker}注解。
 *
 * @author wjybxx
 * date - 2023/12/10
 */
@Target(ElementType.TYPE)
@Retention(RetentionPolicy.SOURCE)
public @interface DsonCodecLinkerGroup {

    /** 生成的codec的输出目录  -- 默认值为当前配置类的包名！ */
    String outputPackage() default "";

}