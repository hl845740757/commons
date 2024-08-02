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

import javax.annotation.concurrent.Immutable;

/**
 * @author wjybxx
 * date - 2023/10/14
 */
@Immutable
public class DsonWriterSettings {

    public static final DsonWriterSettings DEFAULT = newBuilder().build();

    public final int recursionLimit;
    public final boolean autoClose;

    protected DsonWriterSettings(Builder builder) {
        this.recursionLimit = Math.max(1, builder.recursionLimit);
        this.autoClose = builder.autoClose;
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        /** 递归深度限制 */
        private int recursionLimit = 32;
        /** 是否自动关闭底层的输入输出流 */
        private boolean autoClose = true;

        protected Builder() {
        }

        public int getRecursionLimit() {
            return recursionLimit;
        }

        public Builder setRecursionLimit(int recursionLimit) {
            this.recursionLimit = recursionLimit;
            return this;
        }

        public boolean isAutoClose() {
            return autoClose;
        }

        public Builder setAutoClose(boolean autoClose) {
            this.autoClose = autoClose;
            return this;
        }

        public DsonWriterSettings build() {
            return new DsonWriterSettings(this);
        }
    }
}