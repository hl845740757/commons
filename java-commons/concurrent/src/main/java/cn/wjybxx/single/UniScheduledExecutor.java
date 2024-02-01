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

package cn.wjybxx.single;

import cn.wjybxx.concurrent.IScheduledExecutorService;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.TimeUnit;

/**
 * {@inheritDoc}
 * 定时任务调度器，时间单位取决于具体的实现，通常是毫秒 -- 也可能是帧数。
 *
 * <h3>时序保证</h3>
 * 1. 单次执行的任务之间，有严格的时序保证，当过期时间(超时时间)相同时，先提交的一定先执行。
 * 2. 周期性执行的的任务，仅首次执行具备时序保证，当进入周期运行时，与其它任务之间便不具备时序保证。
 *
 * <h3>避免死循环</h3>
 * 子类实现必须在保证时序的条件下解决可能的死循环问题。
 * Q: 死循环是如何产生的？
 * A: 对于周期性任务，我们严格要求了周期间隔大于0，因此周期性的任务不会引发无限循环问题。
 * 但如果用户基于{@link #schedule(Runnable, long, TimeUnit)}实现循环，则在执行回调时可能添加一个立即执行的task（超时时间小于等于0），则可能陷入死循环。
 * 这种情况一般不是有意为之，而是某些特殊情况下产生的，比如：下次执行的延迟是计算出来的，而算出来的延迟总是为0或负数（线程缓存了时间戳，导致计算结果同一帧不会变化）。
 * 如果很好的限制了单帧执行的任务数，可以避免死循环。不过，错误的调用仍然可能导致其它任务得不到执行。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface UniScheduledExecutor extends UniExecutorService, IScheduledExecutorService {

}