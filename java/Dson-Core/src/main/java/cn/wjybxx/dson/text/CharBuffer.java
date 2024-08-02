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

import cn.wjybxx.base.io.ByteBufferUtils;

import javax.annotation.Nonnull;
import java.nio.BufferOverflowException;
import java.util.Arrays;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/7/8
 */
class CharBuffer implements CharSequence {

    public char[] array;
    public int ridx;
    public int widx;

    CharBuffer(int length) {
        this.array = new char[length];
    }

    CharBuffer(char[] array) {
        this.array = Objects.requireNonNull(array);
    }

    public int capacity() {
        return array.length;
    }

    public boolean isReadable() {
        return ridx < widx;
    }

    public boolean isWritable() {
        return widx < array.length;
    }

    public int writableChars() {
        return array.length - widx;
    }

    public int readableChars() {
        return Math.max(0, widx - ridx);
    }

    // region CharSequence

    /** Length为可读字节数 */
    @Override
    public int length() {
        return Math.max(0, widx - ridx);
    }

    @Override
    public char charAt(int index) {
        return array[ridx + index];
    }

    @Nonnull
    @Override
    public CharSequence subSequence(int start, int end) {
        return new String(array, ridx + start, end - start);
    }

    // endregion

    // region 读写

    public char read() {
        if (ridx == widx) throw new BufferOverflowException();
        return array[ridx++];
    }

    public void unread() {
        if (ridx == 0) throw new BufferOverflowException();
        ridx--;
    }

    public void write(char c) {
        if (widx == array.length) {
            throw new BufferOverflowException();
        }
        array[widx++] = c;
    }

    public void write(char[] chars) {
        if (chars.length == 0) {
            return;
        }
        if (widx + chars.length > array.length) {
            throw new BufferOverflowException();
        }
        System.arraycopy(chars, 0, array, widx, chars.length);
        widx += chars.length;
    }

    public void write(char[] chars, int offset, int len) {
        if (len == 0) {
            return;
        }
        ByteBufferUtils.checkBuffer(chars.length, offset, len);
        if (widx + len > array.length) {
            throw new BufferOverflowException();
        }
        System.arraycopy(chars, offset, array, widx, len);
        widx += len;
    }

    /**
     * 将给定buffer中的可读字符写入到当前buffer中
     * 给定的buffer的读索引将更新，当前buffer的写索引将更新
     *
     * @return 写入的字符数；返回0时可能是因为当前buffer已满，或给定的buffer无可读字符
     */
    public int write(CharBuffer charBuffer) {
        int n = Math.min(writableChars(), charBuffer.readableChars());
        if (n == 0) {
            return 0;
        }
        write(charBuffer.array, charBuffer.ridx, n);
        charBuffer.addRidx(n);
        return n;
    }
    // endregion

    //region 索引调整

    public void addRidx(int count) {
        setRidx(ridx + count);
    }

    public void addWidx(int count) {
        setWidx(widx + count);
    }

    public void setRidx(int ridx) {
        if (ridx < 0 || ridx > widx) {
            throw new IllegalArgumentException("ridx overflow");
        }
        this.ridx = ridx;
    }

    public void setWidx(int widx) {
        if (widx < ridx || widx > array.length) {
            throw new IllegalArgumentException("widx overflow");
        }
        this.widx = widx;
    }

    public void setIndexes(int ridx, int widx) {
        if (ridx < 0 || ridx > widx) {
            throw new IllegalArgumentException("ridx overflow");
        }
        if (widx > array.length) {
            throw new IllegalArgumentException("widx overflow");
        }
        this.ridx = ridx;
        this.widx = widx;
    }
    // endregion

    //region 容量调整

    public void shift(int shiftCount) {
        if (shiftCount <= 0) {
            return;
        }
        if (shiftCount >= array.length) {
//                Arrays.fill(buffer, (char) 0);
            ridx = 0;
            widx = 0;
        } else {
            System.arraycopy(array, shiftCount, array, 0, array.length - shiftCount);
            ridx = Math.max(0, ridx - shiftCount);
            widx = Math.max(0, widx - shiftCount);
//            Arrays.fill(buffer, widx, buffer.length, (char) 0);
        }
    }

    public char[] grow(char[] newArray) {
        char[] oldArray = this.array;
        System.arraycopy(oldArray, 0, newArray, 0, oldArray.length);
        this.array = newArray;
        return oldArray;
    }

    public void grow(int capacity) {
        char[] buffer = this.array;
        if (capacity <= buffer.length) {
            return;
        }
        this.array = Arrays.copyOf(this.array, capacity);
    }
    // endregion

    public void clear() {
        ridx = widx = 0;
    }

    @Nonnull
    @Override
    public String toString() {
        return "CharBuffer{" +
                "buffer='" + encodeBuffer() + "'" +
                ", ridx=" + ridx +
                ", widx=" + widx +
                '}';
    }

    private String encodeBuffer() {
        if (ridx >= widx) {
            return "";
        }
        return new String(array, ridx, Math.max(0, widx - ridx));
    }
}