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
/// <see cref="IDsonCodec{T}"/>的注册表
/// </summary>
public interface IDsonCodecRegistry
{
    /// <summary>
    /// 获取类型对应的编解码器。
    /// 
    /// 1.可以返回超类的codec，因为子类实例可以向上转型，但子类特殊数据将被丢弃。
    /// 2.不可返回子类的codec，因为超类实例不能向下转型。
    /// 
    /// PS：可参考集合和字典的Codec实现。
    /// </summary>
    DsonCodecImpl? GetEncoder(Type type);

    /// <summary>
    /// 获取类型对应的解码器。
    /// 
    /// 1.可以返回子类的Codec，如果子类和当前类数据兼容。
    /// 2.不可返回超类的codec，因为超类Codec创建的实例不能安全向下转型。
    /// 3.如果可以，请尽量返回当前类对应的Codec，以避免错误。
    ///
    /// PS：可参考集合和字典的Codec实现。
    /// </summary>
    DsonCodecImpl? GetDecoder(Type type);
}
}