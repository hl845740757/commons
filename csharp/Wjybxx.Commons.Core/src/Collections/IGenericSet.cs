#region LICENSE

//  Copyright 2023 wjybxx
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System.Collections.Generic;

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 没有实现<see cref="ISet{T}"/>是故意的，ISet中的接口过于术语化，日常的使用不需要那么多接口，多数情况下我们只想判断是否包含某个元素。
/// 那些特殊的接口，用户可以通过自定义Util方法解决。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IGenericSet<T> : IGenericCollection<T>
{
    /// <summary>
    /// 向Set中添加一个元素
    /// </summary>
    /// <param name="item"></param>
    /// <returns>如果是新元素则返回true</returns>
    new bool Add(T item);

    #region 接口适配

    void ICollection<T>.Add(T item) {
        Add(item);
    }

    #endregion
}