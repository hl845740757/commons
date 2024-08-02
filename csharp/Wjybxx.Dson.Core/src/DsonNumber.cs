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

namespace Wjybxx.Dson
{
/// <summary>
/// Dson数字类型抽象
/// </summary>
public abstract class DsonNumber : DsonValue
{
    /** 将value转为int */
    public abstract int IntValue { get; }

    /** 将value转long */
    public abstract long LongValue { get; }

    /** 将value转float */
    public abstract float FloatValue { get; }

    /** 将value转double */
    public abstract double DoubleValue { get; }
}
}