#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 0.由<see cref="TaskPoolSizeAttribute"/>在目标方法上配置对象池大小。
/// 1.该类型由于要复用，不能继承Promise，否则可能导致用户使用到错误的接口。
/// 2.用户在获取结果时触发回收。
/// 3.该实现并不是严格线程安全的，用在非StateMachine场景可能导致错误。
/// </summary>
/// <typeparam name="S">状态机类型</typeparam>
/// <typeparam name="T">任务的结果类型</typeparam>
internal sealed class ValueFutureStateMachineTask<S, T> : ValuePromise<T>, IValueFutureStateMachineTask<T>
    where S : IAsyncStateMachine
{
    /// <summary>
    /// 任务状态机
    /// </summary>
    private S _stateMachine;
    /// <summary>
    /// 驱动状态机的委托
    /// </summary>
    private readonly Action _moveToNext;
    /// <summary>
    /// 池化对象的Next（非volatile，由自旋锁保证可见性）
    /// </summary>
    private ValueFutureStateMachineTask<S, T>? _next;

    private ValueFutureStateMachineTask() {
        _moveToNext = Run;
    }

    private void Run() {
        _stateMachine.MoveNext();
    }

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// </summary>
    public Action MoveToNext => _moveToNext;

    protected override void Reset() {
        base.Reset();
        _stateMachine = default;
    }

    protected override void PrepareToRecycle() {
        if (_poolSize > 0) {
            Release(this);
        }
    }

    #region factory

    // ReSharper disable StaticMemberInGenericType
    /// <summary>
    /// 期望的池大小
    /// 注：使用栈的方式可以避免分配过大的数组，使得池化所有状态机成为可能。
    /// </summary>
    private static readonly int _poolSize;
    /// <summary>
    /// 保护_stack和_count的自旋锁：0-空闲，1-已加锁
    /// 注：简单CAS置换栈顶可能出现ABA问题，导致栈顶的next指向错误的对象。
    /// </summary>
    private static int _spinLock;
    private static int _count;
    private static ValueFutureStateMachineTask<S, T>? _stack;

    static ValueFutureStateMachineTask() {
        _poolSize = GetPoolSize();
    }

    public static void SetStateMachine(ref S stateMachine, out IValueFutureStateMachineTask<T> task, out int reentryId) {
        ValueFutureStateMachineTask<S, T> result = Acquire();

        // task和reentryId是builder的属性，而builder是状态机的属性，需要在拷贝状态机之前完成初始化
        // init builder before copy state machine
        task = result;
        reentryId = result.IncReentryId(); // 重用时也+1

        // copy struct... 从栈拷贝到堆，此后栈上的状态机将被丢弃
        result._stateMachine = stateMachine;
    }

    private static ValueFutureStateMachineTask<S, T> Acquire() {
        if (_poolSize == 0) {
            return new ValueFutureStateMachineTask<S, T>();
        }
        Lock();
        try {
            ValueFutureStateMachineTask<S, T>? result = _stack;
            if (result == null) {
                return new ValueFutureStateMachineTask<S, T>();
            }
            _stack = result._next;
            result._next = null;
            _count--;
            return result;
        }
        finally {
            Unlock();
        }
    }

    private static void Release(ValueFutureStateMachineTask<S, T> obj) {
        obj.Reset();
        if (_poolSize == 0 || Volatile.Read(ref _count) >= _poolSize) {
            return;
        }
        Lock();
        try {
            if (_count >= _poolSize) {
                return;
            }
            obj._next = _stack;
            _stack = obj;
            _count++;
        }
        finally {
            Unlock();
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Lock() {
        while (Interlocked.CompareExchange(ref _spinLock, 1, 0) != 0) {
            Thread.SpinWait(1); // 也可以考虑尝试一定次数后放弃池化
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Unlock() {
        Volatile.Write(ref _spinLock, 0);
    }

    private static int GetPoolSize() {
        string stateName = typeof(S).Name;
        MethodInfo? methodInfo = GetAsyncMethod(typeof(S).DeclaringType!, stateName);
        if (methodInfo == null) {
            return 0;
        }
        TaskPoolSizeAttribute attribute = methodInfo.GetCustomAttribute<TaskPoolSizeAttribute>();
        if (attribute == null) {
            return 0;
        }
        if (methodInfo.IsGenericMethod) {
            methodInfo = methodInfo.GetGenericMethodDefinition();
            // 返回值是泛型时才使用两个参数
            if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType != typeof(T)) {
                bool isIntOrObject = typeof(T) == typeof(int) || typeof(T) == typeof(object);
                return isIntOrObject ? attribute.poolSize : attribute.poolSize2;
            }
        }
        return attribute.poolSize;
    }

    private static MethodInfo? GetAsyncMethod(Type declaredType, string stateName) {
        string methodName = stateName.Substring2(1, stateName.IndexOf('>'));
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        try {
            return declaredType.GetMethod(methodName, bindingFlags);
        }
        catch (AmbiguousMatchException) {
            // 处理重载问题(成本较高)
            int index = stateName.LastIndexOf('`');
            int typeParameterCount = index > 0 ? int.Parse(stateName.AsSpan(index + 1)) : 0;
            foreach (MethodInfo method in typeof(S).DeclaringType!.GetMethods(bindingFlags)) {
                if (method.Name != methodName) {
                    continue;
                }
                if ((typeParameterCount > 0) != method.IsGenericMethod) {
                    continue;
                }
                if (typeParameterCount > 0 && (typeParameterCount != method.GetGenericArguments().Length)) {
                    continue;
                }
                return method;
            }
        }
        return null;
    }

    #endregion
}
}