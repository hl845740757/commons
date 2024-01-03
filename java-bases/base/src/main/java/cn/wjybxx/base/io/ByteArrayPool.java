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

import cn.wjybxx.base.pool.ObjectPool;

/**
 * Java不是真泛型，不能直接定义ArrayPool，
 * 不过对于IO操作而言，绝大多数情况下使用字节数组的池就可以。
 *
 * @author wjybxx
 * date - 2024/1/3
 */
public interface ByteArrayPool extends ObjectPool<byte[]> {

    /**
     * 注意：返回的字节数组可能大于期望的数组长度
     *
     * @param minimumLength 期望的最小数组长度
     * @return 池化的字节数组
     */
    byte[] rent(int minimumLength);

    /** 归还数组到池，默认情况下不清理数据 */
    @Override
    void returnOne(byte[] buffer);

    /**
     * 归还数组到池
     *
     * @param buffer 租借的对象
     * @param clear  是否清理数组
     */
    void returnOne(byte[] buffer, boolean clear);
}