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

using Wjybxx.Commons.Attributes;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 计数器 -- 非线程安全
/// </summary>
[NotThreadSafe]
public class Counter
{
    public readonly IDictionary<int, long> sequenceMap = new Dictionary<int, long>();
    public readonly IList<string> errorMsgList = new List<string>();

    public void Count(int type, long sequence) {
        if (type < 1) {
            errorMsgList.Add($"code1, event.type: {type} (expected: > 0)");
            return;
        }
        sequenceMap.TryGetValue(type, out long nextSequence);
        if (sequence != nextSequence) {
            if (errorMsgList.Count < 100) {
                errorMsgList.Add($"code2, event.type: {type}, nextSequence: {sequence} (expected: = {nextSequence})");
            }
        }
        sequenceMap[type] = nextSequence + 1;
    }

    public Action NewTask(int type, long sequence) {
        if (type <= 0) throw new ArgumentException("invalidType: " + type);
        return () => Count(type, sequence);
    }
}