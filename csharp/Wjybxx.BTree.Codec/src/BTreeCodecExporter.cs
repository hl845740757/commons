#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using System;
using System.Collections.Generic;
using Wjybxx.BTree;
using Wjybxx.BTreeCodec.Codecs;
using Wjybxx.BTree.FSM;
using Wjybxx.BTree.Leaf;
using Wjybxx.BTree.Decorator;
using Wjybxx.BTree.Branch;
using Wjybxx.BTree.Branch.Join;

namespace Wjybxx.BTreeCodec
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public static class BTreeCodecExporter
{
    public static Dictionary<Type, Type> ExportCodecs() {
        var dic = new Dictionary<Type, Type>(35);
        dic[typeof(TaskEntry<>)] = typeof(TaskEntry1Codec<>);
        dic[typeof(ChangeStateTask<>)] = typeof(ChangeStateTask1Codec<>);
        dic[typeof(StateMachineTask<>)] = typeof(StateMachineTask1Codec<>);
        dic[typeof(Failure<>)] = typeof(Failure1Codec<>);
        dic[typeof(Running<>)] = typeof(Running1Codec<>);
        dic[typeof(SimpleRandom<>)] = typeof(SimpleRandom1Codec<>);
        dic[typeof(Success<>)] = typeof(Success1Codec<>);
        dic[typeof(WaitFrame<>)] = typeof(WaitFrame1Codec<>);
        dic[typeof(AlwaysCheckGuard<>)] = typeof(AlwaysCheckGuard1Codec<>);
        dic[typeof(AlwaysFail<>)] = typeof(AlwaysFail1Codec<>);
        dic[typeof(AlwaysRunning<>)] = typeof(AlwaysRunning1Codec<>);
        dic[typeof(AlwaysSuccess<>)] = typeof(AlwaysSuccess1Codec<>);
        dic[typeof(Inverter<>)] = typeof(Inverter1Codec<>);
        dic[typeof(OnlyOnce<>)] = typeof(OnlyOnce1Codec<>);
        dic[typeof(Repeat<>)] = typeof(Repeat1Codec<>);
        dic[typeof(SubtreeRef<>)] = typeof(SubtreeRef1Codec<>);
        dic[typeof(UntilCond<>)] = typeof(UntilCond1Codec<>);
        dic[typeof(UntilFail<>)] = typeof(UntilFail1Codec<>);
        dic[typeof(UntilSuccess<>)] = typeof(UntilSuccess1Codec<>);
        dic[typeof(ActiveSelector<>)] = typeof(ActiveSelector1Codec<>);
        dic[typeof(FixedSwitch<>)] = typeof(FixedSwitch1Codec<>);
        dic[typeof(Foreach<>)] = typeof(Foreach1Codec<>);
        dic[typeof(Join<>)] = typeof(Join1Codec<>);
        dic[typeof(Selector<>)] = typeof(Selector1Codec<>);
        dic[typeof(SelectorN<>)] = typeof(SelectorN1Codec<>);
        dic[typeof(Sequence<>)] = typeof(Sequence1Codec<>);
        dic[typeof(ServiceParallel<>)] = typeof(ServiceParallel1Codec<>);
        dic[typeof(SimpleParallel<>)] = typeof(SimpleParallel1Codec<>);
        dic[typeof(Switch<>)] = typeof(Switch1Codec<>);
        dic[typeof(JoinAnyOf<>)] = typeof(JoinAnyOf1Codec<>);
        dic[typeof(JoinMain<>)] = typeof(JoinMain1Codec<>);
        dic[typeof(JoinSelector<>)] = typeof(JoinSelector1Codec<>);
        dic[typeof(JoinSequence<>)] = typeof(JoinSequence1Codec<>);
        dic[typeof(JoinWaitAll<>)] = typeof(JoinWaitAll1Codec<>);
        dic[typeof(JoinSelectorN<>)] = typeof(JoinSelectorN1Codec<>);
        return dic;
    }
}
}