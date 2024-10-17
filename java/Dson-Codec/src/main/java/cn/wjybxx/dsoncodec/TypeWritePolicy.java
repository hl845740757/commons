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

/**
 * 类型信息的写入策略
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public enum TypeWritePolicy {

    /**
     * 总是不写入对象的类型信息，无论运行时类型与声明类型是否相同；
     * 当与脚本语言交互时可能有用。
     */
    NONE,

    /**
     * 优化写入策略。
     * 1.当[encoderType]和[declaredType]完全相同时不写入类型信息 -- 包括泛型参数也相同。
     * 2.当[encoderType]是[declaredType]的默认解析类型，且泛型参数相同时，不写入类型信息。
     * <p>
     * ps：通常我们的字段类型定义是明确的，因此可以省去大量不必要的类型信息；
     * 当用于静态语言之间通信时，可选择该策略。
     */
    OPTIMIZED,

    /**
     * 总是写入对象的类型信息，无论运行时类型与声明类型是否相同
     * 这种方式有更好的兼容性，对跨语言友好，因为目标语言可能没有泛型，或没有注解处理器生成辅助代码等；
     * 但这种方式会大幅增加序列化后的大小，需要慎重考虑。
     */
    ALWAYS;
}