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

using System;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 文本解析器的设置
/// </summary>
public class DsonTextReaderSettings : DsonReaderSettings
{
    public new static DsonTextReaderSettings Default { get; } = (DsonTextReaderSettings)NewBuilder().Build();

    public readonly DsonType localIdType;

    public DsonTextReaderSettings(Builder builder) : base(builder) {
        localIdType = builder.LocalIdType;

        if (localIdType != DsonType.Int32
            && localIdType != DsonType.Int64
            && localIdType != DsonType.String) {
            throw new ArgumentException("invalid localIdType: " + localIdType);
        }
    }

    public new static Builder NewBuilder() {
        return new Builder();
    }

    public new class Builder : DsonReaderSettings.Builder
    {
        /** localId的类型 -- 限制int32、int64、string */
        public DsonType LocalIdType { get; set; } = DsonType.String;

        public Builder() {
        }

#if UNITY_EDITOR
        public override DsonReaderSettings Build() {
            return new DsonTextReaderSettings(this);
        }
#else
        public override DsonTextReaderSettings Build() {
            return new DsonTextReaderSettings(this);
        }
#endif
    }
}
}