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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 字典的键解码器
/// </summary>
public interface IKeyCodec<T>
{
    /// <summary>
    /// features必须是最终的特征值，该方法内部不查询上下文信息
    /// </summary>
    /// <param name="value"></param>
    /// <param name="features"></param>
    /// <returns></returns>
    string EncodeKey(T value, SerializeFeatures features);

    T DecodeKey(string keyString);
}
}