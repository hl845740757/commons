﻿#region LICENSE

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
/// 数字格式化方式
/// 数字可能有不同的格式化需要，比如控制精度，因此定义为接口，默认实现见<see cref="NumberStyles"/>
/// </summary>
public interface INumberStyle
{
    StyleOut ToString(int value);

    StyleOut ToString(long value);

    StyleOut ToString(float value);

    StyleOut ToString(double value);
}
}