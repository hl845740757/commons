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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 当要编码的对象类型不存在直接的Codec时，该方法用于转换编解码类型。
/// PS：建议实现为无状态的。
/// </summary>
public interface IDsonCodecCaster
{
    /// <summary>
    /// 转换编码类型
    /// 1.只可以向上转换，因为类型cast限制。
    /// 2.如果目标类型是泛型，返回的Class必须可以继承泛型参数
    /// 3.转换后的类型必须存在对应的Codec
    /// 4.如果是泛型类，传入的是泛型定义类(原型)
    /// </summary>
    /// <param name="clazz">目标类</param>
    /// <returns>要转换的编码类型；null表示找不到合适的类型，将继续查找下一个</returns>
    Type? CastEncoderType(Type clazz);

    /// <summary>
    /// 转换解码类型
    /// 1.理论上可以向上转换：只要超类对应的Codec返回的实例可以向下转换 -- 通常只适用集合。
    /// 2.理论上也可以向下转换，只要数据是兼容的(子类不包含额外的数据) -- 因此接口不是泛型。
    /// 3.如果目标类型是泛型，返回的Class必须可以继承泛型参数
    /// 4.如果是泛型类，传入的是泛型定义类(原型)
    /// 
    /// </summary>
    /// <param name="clazz">目标类</param>
    /// <returns>要转换的解码类型；null表示找不到合适的类型，将继续查找下一个</returns>
    Type? CastDecoderType(Type clazz);
}
}