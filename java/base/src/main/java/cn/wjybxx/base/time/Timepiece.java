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

package cn.wjybxx.base.time;

import javax.annotation.concurrent.NotThreadSafe;

/**
 * 增量式计时器，需要外部每帧调用{@link #update(int)}累积时间
 *
 * @author wjybxx
 * date 2023/4/4
 */
@NotThreadSafe
public interface Timepiece extends TimeProvider {

    @Override
    long getTime();

    /** 当前帧和前一帧之间的时间跨度 -- {@link #update(int)} */
    int getDeltaTime();

    /** 获取运行帧数 -- 每秒60帧可运行410天 */
    int getFrameCount();

    /**
     * 累加时间
     *
     * @param deltaTime 时间增量，如果该值小于0，则会被修正为0
     */
    void update(int deltaTime);

    /**
     * 设置当前时间
     *
     * @param curTime 当前时间
     */
    void setTime(long curTime);

    /**
     * 在不修改当前时间戳的情况下修改deltaTime
     * （仅仅用在补偿的时候，慎用）
     */
    void setDeltaTime(int deltaTime);

    /**
     * 在不修改当前时间戳的情况下修改frameCount
     * （慎用）
     *
     * @param frameCount 当前帧号
     */
    void setFrameCount(int frameCount);

    /**
     * 重新启动计时 - 累积时间和deltaTime都清零。
     */
    void restart();

    /**
     * 重新启动计时器
     *
     * @param curTime    当前时间
     * @param deltaTime  时间间隔
     * @param frameCount 当前帧号
     */
    void restart(long curTime, int deltaTime, int frameCount);

}