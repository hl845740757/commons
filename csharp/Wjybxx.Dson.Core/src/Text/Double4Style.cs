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

using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// <see cref="Double4"/>的文本输出格式
///
/// 注：
/// 1.解码时固定顺序读取，忽略字段名。
/// 2.慎重选择编码样式，选择错误可能导致数据丢失。
/// </summary>
public enum Double4Style : byte
{
    // 数组格式
    Array4 = 0, // [@D4 v0, v1, v2, v3] 
    Array3 = 1, // [@D4 v0, v1, v2] 
    Array2 = 2, // [@D4 v0, v1]
    // 向量格式
    Vector4 = 3, // {@D4 X: 1, Y: 1, z: 1, w: 1}
    Vector3 = 4, // {@D4 X: 1, Y: 1, z: 1}
    Vector2 = 5, // {@D4 X: 1, Y: 1}
    // 整数向量
    Vector4Int = 6, // {@D4 X: 1, Y: 1, z: 1, w: 1}
    Vector3Int = 7, // {@D4 X: 1, Y: 1, z: 1}
    Vector2Int = 8, // {@D4 X: 1, Y: 1}
    // 颜色值
    Rgba = 9, // {@D4 r: 1, g: 1, b: 1, a: 1}
    Rgb = 10, // {@D4 r: 1, g: 1, b: 1}
    // 矩形
    Rect = 11, // {@D4 x: 1, y: 1, w: 50, h: 50}
    RectInt = 12, // {@D4 x: 1, y: 1, w: 50, h: 50}
}
}