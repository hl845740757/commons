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

package cn.wjybxx.dson;

import javax.annotation.Nullable;
import javax.annotation.concurrent.Immutable;

/**
 * @author wjybxx
 * date - 2023/10/14
 */
@Immutable
public class DsonReaderSettings {

    public static final DsonReaderSettings DEFAULT = newBuilder().build();

    public final int recursionLimit;
    public final boolean autoClose;
    public final Boolean enableNameIntern;

    public DsonReaderSettings(Builder builder) {
        this.recursionLimit = Math.max(1, builder.recursionLimit);
        this.autoClose = builder.autoClose;
        this.enableNameIntern = builder.enableNameIntern;
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        /** 递归深度限制 */
        private int recursionLimit = 32;
        /** 是否自动关闭底层的输入输出流 */
        private boolean autoClose = true;
        /**
         * 是否池化字段名
         * 1.字段名几乎都是常量，因此命中率几乎百分之百 —— 字典由Codec处理。
         * 2.池化字段名可以降低字符串内存占用，有一定的查找开销。
         * 3.如果未设置，则完全由代码控制；如果为false，则全局禁用；如果为true，则全局启用，用户可临时关闭；
         */
        private Boolean enableNameIntern = null;

        protected Builder() {
        }

        public int getRecursionLimit() {
            return recursionLimit;
        }

        public Builder setRecursionLimit(int recursionLimit) {
            this.recursionLimit = recursionLimit;
            return this;
        }

        @Nullable
        public Boolean isEnableNameIntern() {
            return enableNameIntern;
        }

        public Builder setEnableNameIntern(Boolean enableNameIntern) {
            this.enableNameIntern = enableNameIntern;
            return this;
        }

        public boolean isAutoClose() {
            return autoClose;
        }

        public Builder setAutoClose(boolean autoClose) {
            this.autoClose = autoClose;
            return this;
        }

        public DsonReaderSettings build() {
            return new DsonReaderSettings(this);
        }
    }
}