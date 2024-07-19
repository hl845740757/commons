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
using System.Text;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.IO;

/// <summary>
/// 提供基础的handler实现
/// </summary>
public static class PoolableObjectHandlers
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">工厂方法</param>
    /// <param name="resetHandler">重置方法</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static IPoolableObjectHandler<T> Of<T>(Func<T> factory,
                                                  Action<T>? resetHandler = null,
                                                  Func<T, bool>? filter = null) {
        return new PoolableObjectHandler1<T>(factory, resetHandler, filter);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">工厂方法</param>
    /// <param name="resetHandler">重置方法</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static IPoolableObjectHandler<T> Of<T>(Func<int, T> factory,
                                                  Action<T>? resetHandler = null,
                                                  Func<T, bool>? filter = null) {
        return new PoolableObjectHandler2<T>(factory, resetHandler, filter);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="minCapacity">初始空间</param>
    /// <param name="maxCapacity">最大空间</param>
    /// <returns></returns>
    public static IPoolableObjectHandler<StringBuilder> NewStringBuilderHandler(int minCapacity, int maxCapacity) {
        return new StringBuilderHandler(minCapacity, maxCapacity);
    }

    private class PoolableObjectHandler1<T> : IPoolableObjectHandler<T>
    {
        private readonly Func<T> _factory;
        private readonly Action<T>? _resetHandler;
        private readonly Func<T, bool>? _filter;

        internal PoolableObjectHandler1(Func<T> factory, Action<T>? resetHandler = null, Func<T, bool>? filter = null) {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _resetHandler = resetHandler;
            _filter = filter;
        }

        public T Create(IObjectPool<T> pool, int capacity) {
            return _factory.Invoke();
        }

        public bool Test(T obj) {
            return _filter == null || _filter.Invoke(obj);
        }

        public void Reset(T obj) {
            if (_resetHandler != null) {
                _resetHandler.Invoke(obj);
            }
        }

        public void Destroy(T obj) {
        }
    }

    private class PoolableObjectHandler2<T> : IPoolableObjectHandler<T>
    {
        private readonly Func<int, T> _factory;
        private readonly Action<T>? _resetHandler;
        private readonly Func<T, bool>? _filter;

        internal PoolableObjectHandler2(Func<int, T> factory, Action<T>? resetHandler = null, Func<T, bool>? filter = null) {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _resetHandler = resetHandler;
            _filter = filter;
        }

        public T Create(IObjectPool<T> pool, int capacity) {
            return _factory.Invoke(capacity);
        }

        public bool Test(T obj) {
            return _filter == null || _filter.Invoke(obj);
        }

        public void Reset(T obj) {
            if (_resetHandler != null) {
                _resetHandler.Invoke(obj);
            }
        }

        public void Destroy(T obj) {
        }
    }

    private class StringBuilderHandler : IPoolableObjectHandler<StringBuilder>
    {
        private readonly int minCapacity;
        private readonly int maxCapacity;

        internal StringBuilderHandler(int minCapacity, int maxCapacity) {
            if (minCapacity < 0 || maxCapacity < minCapacity) {
                throw new ArgumentException();
            }
            this.minCapacity = minCapacity;
            this.maxCapacity = maxCapacity;
        }

        public StringBuilder Create(IObjectPool<StringBuilder> pool, int capacity) {
            return new StringBuilder(capacity > 0 ? capacity : minCapacity);
        }

        public bool Test(StringBuilder obj) {
            return obj.Capacity >= minCapacity && obj.Capacity <= maxCapacity;
        }

        public void Reset(StringBuilder obj) {
            obj.Clear();
        }

        public void Destroy(StringBuilder obj) {
        }
    }
}