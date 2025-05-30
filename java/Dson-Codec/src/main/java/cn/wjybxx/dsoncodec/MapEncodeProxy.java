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

import java.util.Collection;
import java.util.Map;

/**
 * 字典的编码代理
 *
 * @author wjybxx
 * date - 2024/5/19
 */
public class MapEncodeProxy<V> {

    private MapEncodePolicy policy = MapEncodePolicy.DOCUMENT;
    private Collection<Map.Entry<String, V>> entries;

    public MapEncodeProxy() {
    }

    public MapEncodeProxy(MapEncodePolicy policy) {
        this.policy = policy;
    }

    public void setPolicy(MapEncodePolicy policy) {
        this.policy = policy;
    }

    public MapEncodePolicy getPolicy() {
        return policy;
    }

    public Collection<Map.Entry<String, V>> getEntries() {
        return entries;
    }

    public void setEntries(Collection<Map.Entry<String, V>> entries) {
        this.entries = entries;
    }

}