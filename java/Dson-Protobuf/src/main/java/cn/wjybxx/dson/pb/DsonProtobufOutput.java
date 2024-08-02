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
package cn.wjybxx.dson.pb;

import cn.wjybxx.dson.io.DsonOutput;
import com.google.protobuf.MessageLite;

/**
 * @author wjybxx
 * date - 2023/12/16
 */
public interface DsonProtobufOutput extends DsonOutput {

    /** 向输出流中写入一个消息，不包含长度 */
    void writeMessage(MessageLite message);

}