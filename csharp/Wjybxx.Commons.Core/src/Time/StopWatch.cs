#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Wjybxx.Commons.Time;

/// <summary>
/// 停表
///
/// 使用方式如下：
/// <code>
///  public void Execute() {
///      // 创建一个已启动的计时器
///      final StopWatch stopWatch = StopWatch.CreateStarted("execute");
///
///      DoSomethingA();
///      stopWatch.LogStep("step1");
///
///      DoSomethingB();
///      stopWatch.LogStep("step2");
///
///      DoSomethingC();
///      stopWatch.LogStep("step3");
///
///      DoSomethingD();
///      stopWatch.LogStep("step4");
///
///      // 输出日志
///      logger.Info(stopWatch.GetLog());
///  }
/// </code>
/// 
/// </summary>
public sealed class StopWatch
{
    /** 停表的名字 */
    private readonly string _name;
    /** 运行状态 */
    private State _state = State.Unstarted;
    /** 启动的时间戳 -- start和resume时更新；打点时也更新 */
    private long _startTimeNanos;
    /** 总耗时 */
    private long _elapsedNanos;

    /** 当前步骤已耗时 */
    private long _stepElapsedNanos;
    /** 历史步骤耗时 */
    private readonly List<Item> _itemList = new List<Item>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">停表的名字；推荐命名格式{@code ClassName:MethodName}</param>
    /// <exception cref="ArgumentNullException"></exception>
    public StopWatch(string name) {
        this._name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /** 创建一个停表 */
    public static StopWatch Create(string name) {
        return new StopWatch(name);
    }

    /** 创建一个已启动的停表 */
    public static StopWatch CreateStarted(string name) {
        StopWatch sw = new StopWatch(name);
        sw.Start();
        return sw;
    }

    /** 停表的名字 */
    public string Name => _name;

    /** 停表是否已启动，且尚未停止 */
    public bool IsStarted => _state == State.Running || _state == State.Suspended;

    /** 停表是否处于运行状态 */
    public bool IsRunning => _state == State.Running;

    /** 停表是否处于挂起/暂停状态 */
    public bool IsSuspended => _state == State.Suspended;

    /** 停表是否已停止 */
    public bool IsStopped => _state == State.Stopped;

    // region 生命周期

    /// <summary>
    /// 开始计时。
    /// 重复调用start之前，必须调用{@link #reset()}
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IllegalStateException"></exception>
    public void Start() {
        if (IsStarted) {
            throw new IllegalStateException("Stopwatch is running. ");
        }
        _state = State.Running;
        _startTimeNanos = ObjectUtil.SystemTicks();
        _elapsedNanos = _stepElapsedNanos = 0;
        _itemList.Clear();
    }

    /// <summary>
    /// 记录该步骤的耗时
    /// </summary>
    /// <param name="stepName">该步骤的名称</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="IllegalStateException"></exception>
    public void LogStep(string stepName) {
        if (stepName == null) throw new ArgumentNullException(nameof(stepName));
        if (_state != State.Running) {
            throw new IllegalStateException("Stopwatch is not running. ");
        }
        long delta = ObjectUtil.SystemTicks() - _startTimeNanos;
        _startTimeNanos += delta; // 避免再次读取时间戳
        _elapsedNanos += delta;
        _stepElapsedNanos += delta;

        _itemList.Add(new Item(stepName, _stepElapsedNanos));
        _stepElapsedNanos = 0;
    }

    /** 暂停计时 */
    public void Suspend() {
        if (!IsStarted) {
            throw new IllegalStateException("Stopwatch must be started to suspend. ");
        }
        if (_state == State.Running) {
            long delta = ObjectUtil.SystemTicks() - _startTimeNanos;
            _state = State.Suspended;
            _elapsedNanos += delta;
            _stepElapsedNanos += delta;
        }
    }

    /** 恢复计时 */
    public void Resume() {
        if (!IsStarted) {
            throw new IllegalStateException("Stopwatch must be started to resume. ");
        }
        if (_state == State.Suspended) {
            _state = State.Running;
            _startTimeNanos = ObjectUtil.SystemTicks();
        }
    }

    /**
    /// 如果希望停止计时，则调用该方法。
    /// 停止计时后，{@link #elapsed()}将获得一个稳定的时间值。
     */
    public void Stop() {
        if (!IsStarted) {
            return;
        }
        if (_state == State.Running) {
            long delta = ObjectUtil.SystemTicks() - _startTimeNanos;
            _elapsedNanos += delta;
            _stepElapsedNanos += delta;
        }
        _state = State.Stopped;
    }

    /// <summary>
    /// 重置停表
    /// 注意：为了安全起见，请要么在代码的开始重置，要么在finally块中重置。
    /// </summary>
    public void Reset() {
        if (_state == State.Unstarted) {
            return;
        }
        _state = State.Unstarted;
        _startTimeNanos = 0;
        _elapsedNanos = _stepElapsedNanos = 0;
        _itemList.Clear();
    }

    /// <summary>
    /// 重新启动停表
    /// {@link #reset()}和{@link #start()}的快捷方法
    /// </summary>
    public void Restart() {
        Reset();
        Start();
    }

    // endregion

    // region 获取耗时

    /** 获取开始到现在消耗的总时间 */
    public TimeSpan Elapsed => TimeSpan.FromTicks(ElapsedNanos());

    /** 获取当前步骤已消耗的时间 */
    public TimeSpan StepElapsed => TimeSpan.FromTicks(StepElapsedNanos());

    /** 获取当前已有的步骤耗时信息 */
    public List<KeyValuePair<string, TimeSpan>> ListStepElapsed() {
        List<KeyValuePair<string, TimeSpan>> result = new List<KeyValuePair<string, TimeSpan>>(_itemList.Count);
        foreach (Item item in _itemList) {
            result.Add(new KeyValuePair<string, TimeSpan>(item.stepName, TimeSpan.FromTicks(item.elapsedNanos)));
        }
        return result;
    }

    private long ElapsedNanos() {
        if (_state == State.Running) {
            return _elapsedNanos + (ObjectUtil.SystemTicks() - _startTimeNanos);
        } else {
            return _elapsedNanos;
        }
    }

    private long StepElapsedNanos() {
        if (_state == State.Running) {
            return _stepElapsedNanos + (ObjectUtil.SystemTicks() - _startTimeNanos);
        } else {
            return _stepElapsedNanos;
        }
    }
    // endregion

    /// <summary>
    /// 获取按照时间消耗排序后的log。
    /// 注意：可以在不调用{@link #stop()}的情况下调用该方法。
    /// (获得了一个规律，也失去了一个规律，可能并不如未排序的log看着舒服)
    /// </summary>
    /// <returns></returns>
    public string GetSortedLog() {
        if (_itemList.Count > 0) {
            // 排序开销还算比较小
            List<Item> copiedItems = new List<Item>(_itemList);
            copiedItems.Sort();
            return ToString(copiedItems);
        }
        return ToString(_itemList);
    }

    /// <summary>
    /// 获取最终log
    /// </summary>
    /// <returns></returns>
    public string GetLog() {
        return ToString(_itemList);
    }

    /// <summary>
    /// 格式: StopWatch[name={name}ms][a={a}ms,b={b}ms...]
    /// 1. StepWatch为标记，方便检索。
    /// 2. {@code {x}}表示x的耗时。
    /// 3. 前半部分为总耗时，后半部分为各步骤耗时。
    /// 
    /// Q: 为什么重写{@code toString}？
    /// A: 在输出日志的时候，我们可能常常使用占位符，那么延迟构建内容就是必须的，这要求我们实现{@code toString()}。
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return ToString(_itemList);
    }

    /** @param itemList 避免排序修改数据 */
    private string ToString(List<Item> itemList) {
        StringBuilder sb = new StringBuilder(32);
        // 总耗时
        sb.Append("StopWatch[").Append(_name).Append('=')
            .Append(_elapsedNanos / DatetimeUtil.TicksPerMillisecond)
            .Append("ms]");
        // 每个步骤耗时
        sb.Append('[');
        for (int i = 0; i < itemList.Count; i++) {
            Item item = itemList[i];
            if (i > 0) {
                sb.Append(',');
            }
            sb.Append(item.stepName).Append('=')
                .Append(item.elapsedNanos / DatetimeUtil.TicksPerMillisecond)
                .Append("ms");
        }
        sb.Append(']');
        return sb.ToString();
    }

    private enum State : byte
    {
        /** 未启动 */
        Unstarted = 0,
        /** 运行中 */
        Running = 1,
        /** 挂起 */
        Suspended = 2,
        /** 已停止 */
        Stopped = 3
    }

    private class Item : IComparable<Item>
    {
        internal readonly string stepName;
        internal readonly long elapsedNanos;

        internal Item(string stepName, long elapsedNanos) {
            this.stepName = stepName;
            this.elapsedNanos = elapsedNanos;
        }

        public int CompareTo(Item that) {
            int timeCompareResult = elapsedNanos.CompareTo(that.elapsedNanos);
            if (timeCompareResult != 0) {
                // 时间逆序
                return -1 * timeCompareResult;
            }
            // 字母自然序
            return string.Compare(stepName, that.stepName, StringComparison.Ordinal);
        }
    }
}