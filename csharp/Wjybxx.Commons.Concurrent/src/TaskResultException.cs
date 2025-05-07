#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该异常用于定时任务返回结果
/// 周期任务需要返回结果的情况不常见，因此我们通过异常实现。
/// 
/// Q：为什么不是泛型类？
/// A；担心泛型类的is测试会导致问题 -- 泛型参数是子类的时候可能会导致问题。
/// </summary>
public sealed class TaskResultException : Exception
{
    /// <summary>
    /// null共享对象
    /// </summary>
    public static readonly TaskResultException NULL = new TaskResultException(null);

    private object? _result;

    public TaskResultException(object? result) {
        _result = result;
    }

    public object? Result {
        get => _result;
        set => _result = value;
    }

    /// <summary>
    /// 该接口是个快捷方法，以免类型转换异常导致错误
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? Cast<T>() {
        if (_result is T r) return r;
        return default;
    }

    public override string? StackTrace => null;
}
}