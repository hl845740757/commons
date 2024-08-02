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

import cn.wjybxx.dson.ext.MarkableIterator;
import org.apache.commons.lang3.RandomUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/8/9
 */
@SuppressWarnings("deprecation")
public class MarkableItrTest {

    @Test
    void test() {
        DsonObject<String> object = DsonCodecTest.genRandObject();
        MarkableIterator<DsonValue> iterator = new MarkableIterator<>(object.values().iterator());

        while (iterator.hasNext()) {
            if (RandomUtils.nextBoolean()) {
                iterator.mark();
                DsonValue markedNext = iterator.next();

                int skip = RandomUtils.nextInt(1, 10);
                int c = 0;
                while (iterator.hasNext() && c < skip) {
                    iterator.next();
                    c++;
                }
                iterator.reset();

                DsonValue realNext = iterator.next();
                Assertions.assertSame(markedNext, realNext);
            } else {
                iterator.next();
            }
        }
    }

}