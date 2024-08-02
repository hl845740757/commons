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
using System.Collections.Concurrent;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// <see cref="ClassName"/>的解析化池，
/// 解析字符串为结构体的过程的开销还是比较大的，缓存解析结果可以降低内存分配，提高效率
/// </summary>
[ThreadSafe]
public sealed class ClassNamePool
{
    /** 字符串解析结果的缓存 —— ClassName的解析缓存则存储在<see cref="TypeMeta"/> */
    private readonly ConcurrentDictionary<string, ClassName> string2StructDic = new ConcurrentDictionary<string, ClassName>();

    public ClassNamePool() {
    }

    /// <summary>
    /// 解析Dson风格的字符串名为结构化名字
    /// </summary>
    /// <param name="clsName"></param>
    /// <returns></returns>
    public ClassName Parse(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        if (string2StructDic.TryGetValue(clsName, out ClassName result)) {
            return result;
        }
        // 程序生成的clsName通常是紧凑的，不包含空白字符(缩进)的，因此可以安全缓存；
        // 如果clsName包含空白字符，通常是用户手写的，缓存有一定的风险性 —— 可能产生恶意缓存
        if (ObjectUtil.ContainsWhitespace(clsName)) {
            return ClassName.Parse(clsName);
        }
        result = ClassName.Parse(clsName);
        string2StructDic.TryAdd(clsName, result);
        return result;
    }
}
}