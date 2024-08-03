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

import cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerBean;
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

import java.util.IdentityHashMap;

/**
 * 测试静态代理处理单例问题
 *
 * @author houlei
 * date - 2024/4/12
 */
@DsonCodecLinkerBean(
        value = CodecSingletonTest.MockSingleton.class,
        props = @DsonSerializable(
                singleton = "getInstance"
        ))
public class CodecSingletonTest<K, V> extends IdentityHashMap<K, V> {

    private static final MockSingleton INST = new MockSingleton();

    public static MockSingleton getInstance() {
        return INST;
    }

    public static class MockSingleton {
        private String name;
        private int age;

        public String getName() {
            return name;
        }

        public MockSingleton setName(String name) {
            this.name = name;
            return this;
        }

        public int getAge() {
            return age;
        }

        public MockSingleton setAge(int age) {
            this.age = age;
            return this;
        }
    }
}