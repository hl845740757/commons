#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 行首类型（Line Head Type）
/// </summary>
public enum LineHead
{
    /** 注释 */
    Comment,
    /** 添加新行 */
    AppendLine,
    /** 与上一行合并 */
    Append,
    /** 切换模式 */
    SwitchMode,
    /** 文本输入结束 */
    EndOfText
}
}