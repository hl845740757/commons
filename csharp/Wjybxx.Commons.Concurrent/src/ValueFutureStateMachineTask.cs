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
    /// 池化对象的Next（非volatile，CAS保证可见性）
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
    /// 注：使用栈的方式可以避免分配过大的数组，使得池化所有状态机成为可能；极限情况下可能稍微超过大小限制，无伤大雅；
    /// </summary>
    private static readonly int _poolSize;
    private static volatile int _count;
    private static volatile ValueFutureStateMachineTask<S, T>? _stack;

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
        ValueFutureStateMachineTask<S, T> stack;
        while ((stack = _stack) != null) {
            ValueFutureStateMachineTask<S, T> next = stack._next;
            if (Interlocked.CompareExchange(ref _stack, stack, next) != next) {
                continue;
            }
            stack._next = null;
            Interlocked.Decrement(ref _count);
            return stack;
        }
        return new ValueFutureStateMachineTask<S, T>();
    }

    private static void Release(ValueFutureStateMachineTask<S, T> obj) {
        obj.Reset();
        if (_poolSize == 0) {
            return;
        }
        while (_count < _poolSize) {
            ValueFutureStateMachineTask<S, T> stack = _stack;
            obj._next = stack;
            if (Interlocked.CompareExchange(ref _stack, obj, stack) != stack) {
                continue;
            }
            Interlocked.Increment(ref _count);
            return;
        }
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
            // 返回值是泛型时才使用两个参数 - ReturnType信息不全，无法直接==比较，由此直接比较简单名
            if (methodInfo.ReturnType.Name == typeof(ValueFuture<>).Name) {
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