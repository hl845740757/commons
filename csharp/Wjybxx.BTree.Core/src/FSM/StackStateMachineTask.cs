#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 状态机节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class StackStateMachineTask<T> : StateMachineTask<T> where T : class
{
    private const int QUEUE_CAPACITY = 5;
    // 需要支持编辑器设置
    private int undoQueueCapacity = QUEUE_CAPACITY;
    private int redoQueueCapacity = QUEUE_CAPACITY;
    [NonSerialized]
    private readonly BoundedArrayDeque<Task<T>> undoQueue = new(0, DequeOverflowBehavior.DiscardHead);
    [NonSerialized]
    private readonly BoundedArrayDeque<Task<T>> redoQueue = new(0, DequeOverflowBehavior.DiscardTail);

    #region api

    /** 查看undo对应的state */
    public Task<T>? PeekUndoState() {
        return undoQueue.TryPeekLast(out Task<T> r) ? r : null;
    }

    /** 查看redo对应的state */
    public Task<T>? PeekRedoState() {
        return redoQueue.TryPeekFirst(out Task<T> r) ? r : null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="capacity">最大大小；0表示禁用；大于0启用</param>
    /// <returns>最新的queue</returns>
    public void SetUndoQueueCapacity(int capacity) {
        if (capacity < 0) throw new ArgumentException("capacity: " + capacity);
        this.undoQueueCapacity = capacity;
        undoQueue.SetCapacity(capacity, DequeOverflowBehavior.DiscardHead);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="capacity">最大大小；0表示禁用；大于0启用</param>
    /// <returns>最新的queue</returns>
    public void SetRedoQueueCapacity(int capacity) {
        if (capacity < 0) throw new ArgumentException("capacity: " + capacity);
        this.redoQueueCapacity = capacity;
        redoQueue.SetCapacity(capacity, DequeOverflowBehavior.DiscardTail);
    }

    /// <summary>
    /// 向undo队列中添加一个状态
    /// </summary>
    /// <param name="curState"></param>
    /// <returns>是否添加成功</returns>
    public bool AddUndoState(Task<T> curState) {
        if (undoQueueCapacity < 1) {
            return false;
        }
        undoQueue.AddLast(curState);
        return true;
    }

    /// <summary>
    /// 向redo队列中添加一个状态
    /// </summary>
    /// <param name="curState">是否添加成功</param>
    /// <returns></returns>
    public bool AddRedoState(Task<T> curState) {
        if (redoQueueCapacity < 1) {
            return false;
        }
        redoQueue.AddFirst(curState);
        return true;
    }

    /// <summary>
    /// 撤销到前一个状态
    /// </summary>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <returns>如果有前一个状态则返回true</returns>
    public override bool UndoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.IsUndo()) {
            throw new ArgumentException();
        }
        // 真正切换以后再删除
        if (!undoQueue.TryPeekLast(out Task<T> prevState)) {
            return false;
        }
        ChangeState(prevState, changeStateArgs);
        return true;
    }

    /// <summary>
    /// 重新进入到下一个状态
    /// </summary>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <returns>如果有下一个状态则返回true</returns>
    public override bool RedoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.IsRedo()) {
            throw new ArgumentException();
        }
        // 真正切换以后再删除
        if (!redoQueue.TryPeekFirst(out Task<T> nextState)) {
            return false;
        }
        ChangeState(nextState, changeStateArgs);
        return true;
    }

    #endregion

    #region logic

    public override void ResetForRestart() {
        base.ResetForRestart();
        undoQueue.Clear();
        redoQueue.Clear();
        // 不重写beforeEnter，是因为考虑保留用户的初始队列设置
    }

    protected override void Exit() {
        undoQueue.Clear();
        redoQueue.Clear();
        base.Exit();
    }

    protected override void BeforeChangeState(Task<T>? curState, Task<T>? nextState) {
        if (nextState == null) {
            AddUndoState(curState!);
            return;
        }
        ChangeStateArgs changeStateArgs = (ChangeStateArgs)nextState.ControlData;
        switch (changeStateArgs.cmd) {
            case ChangeStateArgs.CMD_UNDO: {
                undoQueue.TryRemoveLast(out _);
                if (curState != null) {
                    AddRedoState(curState);
                }
                break;
            }
            case ChangeStateArgs.CMD_REDO: {
                redoQueue.TryRemoveFirst(out _);
                if (curState != null) {
                    AddUndoState(curState);
                }
                break;
            }
            default: {
                // 进入新状态，需要清理redo队列
                redoQueue.Clear();
                if (curState != null) {
                    AddUndoState(curState);
                }
                break;
            }
        }
        base.BeforeChangeState(curState, nextState);
    }

    #endregion

    #region 序列化

    public int UndoQueueCapacity {
        get => undoQueueCapacity;
        set => SetUndoQueueCapacity(value);
    }

    public int RedoQueueCapacity {
        get => redoQueueCapacity;
        set => SetRedoQueueCapacity(value);
    }

    #endregion
}
}