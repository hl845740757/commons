using System;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;

namespace Wjybxx.BTree
{
/// <summary>
/// C# await语法支持
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct TaskAwaiter<T> : ICriticalNotifyCompletion where T : class
{
    private readonly TaskEntry<T> taskEntry;
    private readonly int reentryId;

    public TaskAwaiter(TaskEntry<T> taskEntry) {
        this.taskEntry = taskEntry;
        this.reentryId = this.taskEntry.ReentryId;
    }

    // 1.IsCompleted
    public bool IsCompleted => taskEntry.IsCompleted;

    // 2. GetResult
    // TaskEntry不抛出一次
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() {
        if (reentryId != taskEntry.ReentryId) {
            throw new IllegalStateException();
        }
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        if (taskEntry.Handler == null) {
            throw new IllegalStateException();
        }
        taskEntry.Handler.AwaitOnCompleted(taskEntry, continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (taskEntry.Handler == null) {
            throw new IllegalStateException();
        }
        taskEntry.Handler.AwaitOnCompleted(taskEntry, continuation);
    }
}
}