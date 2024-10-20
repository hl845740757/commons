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

package cn.wjybxx.base.pool;

/**
 * @author wjybxx
 * date - 2024/7/19
 */
public class ArrayBucketConfig {

    private final int arrayCapacity;
    private final int cacheCount;

    public ArrayBucketConfig(int arrayCapacity, int cacheCount) {
        if (arrayCapacity < 0 || cacheCount < 0) {
            throw new IllegalArgumentException("capacity: %d, cacheCount: %d".formatted(arrayCapacity, cacheCount));
        }
        this.arrayCapacity = arrayCapacity;
        this.cacheCount = cacheCount;
    }

    public int getArrayCapacity() {
        return arrayCapacity;
    }

    public int getCacheCount() {
        return cacheCount;
    }
}
