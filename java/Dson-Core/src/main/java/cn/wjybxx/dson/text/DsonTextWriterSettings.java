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

package cn.wjybxx.dson.text;

import cn.wjybxx.base.io.StringBuilderWriter;
import cn.wjybxx.dson.DsonWriterSettings;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
@Immutable
public class DsonTextWriterSettings extends DsonWriterSettings {

    public static final DsonTextWriterSettings DEFAULT = DsonTextWriterSettings.newBuilder().build();
    private static final int MIN_LINE_LENGTH = 10;

    public final String lineSeparator;
    public final int softLineLength;

    public final boolean enableText;
    public final float textStringLength;
    public final boolean textAlignLeft;

    public final boolean unicodeChar;
    public final int maxLengthOfUnquoteString;
    public final boolean accessBackingBuilder;

    private DsonTextWriterSettings(Builder builder) {
        super(builder);
        this.lineSeparator = Objects.requireNonNull(builder.lineSeparator);
        this.softLineLength = Math.max(MIN_LINE_LENGTH, builder.softLineLength);

        // 标准模式下才可启用纯文本
        this.enableText = builder.enableText;
        this.textStringLength = Math.max(MIN_LINE_LENGTH, builder.textStringLength);
        this.textAlignLeft = builder.textAlignLeft;

        this.unicodeChar = builder.unicodeChar;
        this.maxLengthOfUnquoteString = builder.maxLengthOfUnquoteString;
        this.accessBackingBuilder = builder.accessBackingBuilder;
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder extends DsonWriterSettings.Builder {

        /** 行分隔符 */
        private String lineSeparator = System.lineSeparator();
        /**
         * 行长度，该值是一个换行参考值
         * 精确控制行长度较为复杂，那样我们需要考虑每一种值toString后长度超出的问题；
         * 另外在美观性上也不好，比如：一个integer写到一半换行。。。
         * 另外，这个行长度是是码元计数，不是字符计数。
         */
        private int softLineLength = 120;

        /**
         * 是否启用纯文本模式
         * 如果{@link #unicodeChar}为true，该值通常需要关闭，text模式不会执行转义，也就不会处理unicode字符
         */
        private boolean enableText = true;
        /** 触发text模式的字符串长度 */
        private float textStringLength = 120;
        /** 纯文本换行是否启用左对齐 */
        private boolean textAlignLeft = true;

        /**
         * 不可打印的ascii码字符是否转为unicode字符
         * (ascii码32~126以外的字符)
         * 通常用于非UTF8文本的移植
         */
        private boolean unicodeChar = false;
        /** 自动模式下无引号字符串的最大长度 -- 过大会降低序列化速度 */
        private int maxLengthOfUnquoteString = 20;
        /** 目标Writer是{@link StringBuilderWriter}时是否直接访问底层的Builder代替额外的分配，这可以节省大量的开销 */
        private boolean accessBackingBuilder = true;

        private Builder() {
        }

        @Override
        public DsonTextWriterSettings build() {
            return new DsonTextWriterSettings(this);
        }

        public String getLineSeparator() {
            return lineSeparator;
        }

        public Builder setLineSeparator(String lineSeparator) {
            this.lineSeparator = lineSeparator;
            return this;
        }

        public int getSoftLineLength() {
            return softLineLength;
        }

        public Builder setSoftLineLength(int softLineLength) {
            this.softLineLength = softLineLength;
            return this;
        }

        public boolean isAccessBackingBuilder() {
            return accessBackingBuilder;
        }

        public Builder setAccessBackingBuilder(boolean accessBackingBuilder) {
            this.accessBackingBuilder = accessBackingBuilder;
            return this;
        }

        public boolean isUnicodeChar() {
            return unicodeChar;
        }

        public Builder setUnicodeChar(boolean unicodeChar) {
            this.unicodeChar = unicodeChar;
            return this;
        }

        public boolean isEnableText() {
            return enableText;
        }

        public Builder setEnableText(boolean enableText) {
            this.enableText = enableText;
            return this;
        }

        public float getTextStringLength() {
            return textStringLength;
        }

        public Builder setTextStringLength(float textStringLength) {
            this.textStringLength = textStringLength;
            return this;
        }

        public int getMaxLengthOfUnquoteString() {
            return maxLengthOfUnquoteString;
        }

        public Builder setMaxLengthOfUnquoteString(int maxLengthOfUnquoteString) {
            this.maxLengthOfUnquoteString = maxLengthOfUnquoteString;
            return this;
        }

        public boolean isTextAlignLeft() {
            return textAlignLeft;
        }

        public Builder setTextAlignLeft(boolean textAlignLeft) {
            this.textAlignLeft = textAlignLeft;
            return this;
        }
    }

}