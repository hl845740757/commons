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

package cn.wjybxx.base.io;


import javax.annotation.Nonnull;
import java.io.IOException;
import java.io.Writer;
import java.util.Objects;

/**
 * 修改自{@link java.io.StringWriter}
 *
 * @author wjybxx
 * date - 2023/12/24
 */
public class StringBuilderWriter extends Writer {

    private final StringBuilder builder;

    public StringBuilderWriter() {
        this(null);
    }

    public StringBuilderWriter(int initialSize) {
        this(new StringBuilder(initialSize));
    }

    public StringBuilderWriter(StringBuilder builder) {
        if (builder == null) {
            builder = new StringBuilder();
        }
        this.builder = builder;
        this.lock = builder;
    }

    // region

    public void write(int c) {
        builder.append((char) c);
    }

    public void write(char[] cbuf, int off, int len) {
        Objects.checkFromIndexSize(off, len, cbuf.length);
        if (len == 0) {
            return;
        }
        builder.append(cbuf, off, len);
    }

    public void write(@Nonnull String str) {
        builder.append(str);
    }

    public void write(@Nonnull String str, int off, int len) {
        builder.append(str, off, off + len);
    }

    public StringBuilderWriter append(CharSequence csq) {
        builder.append(csq);
        return this;
    }

    public StringBuilderWriter append(CharSequence csq, int start, int end) {
        builder.append(csq, start, end);
        return this;
    }

    public StringBuilderWriter append(char c) {
        builder.append(c);
        return this;
    }
    // endregion

    public String toString() {
        return builder.toString();
    }

    public StringBuilder getBuilder() {
        return builder;
    }

    public void flush() {
    }

    public void close() throws IOException {
    }

}
