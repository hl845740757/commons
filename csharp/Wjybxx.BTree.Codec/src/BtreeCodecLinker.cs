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

using System.Reflection;
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.BTree.Decorator;
using Wjybxx.BTree.FSM;
using Wjybxx.BTree.FSM.Handler;
using Wjybxx.BTree.Leaf;
using Wjybxx.Dson.Codec.Attributes;

#pragma warning disable CS0169
#pragma warning disable CS1591

namespace Wjybxx.BTreeCodec
{
/// <summary>
/// 本文件是用于生成Codec的配置文件
///
/// 运行时可通过该类型定位<see cref="Assembly"/>，获取程序集下的所有的Codec。
/// </summary>
[DsonCodecLinkerGroup(OutputNamespace = "Wjybxx.BTreeCodec.Codecs")]
public class BtreeCodecLinker
{
#nullable disable
    public TaskEntry<object> taskEntry;

    [DsonCodecLinkerGroup(OutputNamespace = "Wjybxx.BTreeCodec.Codecs")]
    public class FsmLinker
    {
        private ChangeStateTask<object> changeStateTask;
        private StateMachineTask<object> stateMachineTask;
        private StackStateMachineTask<object> stackStateMachine;
        private FsmStateCfg<object> fsmStateCfg;

        [DsonCodecLinker(Singleton = "Inst")]
        private DefaultStateMachineHandler<object> _defHandler;
        [DsonCodecLinker(Singleton = "Inst")]
        private RedoStateMachineHandler<object> _redoStateMachineHandler;
        [DsonCodecLinker(Singleton = "Inst")]
        private UndoStateMachineHandler<object> _undoStateMachineHandler;
    }

    [DsonCodecLinkerGroup(OutputNamespace = "Wjybxx.BTreeCodec.Codecs")]
    public class BranchLinker
    {
        private ActiveSelector<object> activeSelector;
        private FixedSwitch<object> fixedSwitch;
        private Foreach<object> foreachTask;
        private Join<object> join;
        private Selector<object> selector;
        private SelectorN<object> selectorN;
        private Sequence<object> sequence;
        private ServiceParallel<object> serviceParallel;
        private SimpleParallel<object> simpleParallel;
        private Switch<object> switchTask;
    }

    [DsonCodecLinkerGroup(OutputNamespace = "Wjybxx.BTreeCodec.Codecs")]
    public class JoinPolicyLinker
    {
        [DsonCodecLinker(Singleton = "GetInstance")]
        private JoinAnyOf<object> joinAnyOf;
        [DsonCodecLinker(Singleton = "GetInstance")]
        private JoinMain<object> joinMain;
        [DsonCodecLinker(Singleton = "GetInstance")]
        private JoinSelector<object> joinSelector;
        [DsonCodecLinker(Singleton = "GetInstance")]
        private JoinSequence<object> joinSequence;
        [DsonCodecLinker(Singleton = "GetInstance")]
        private JoinWaitAll<object> joinWaitAll;
        // selectorN有状态，不能单例
        private JoinSelectorN<object> joinSelectorN;
    }

    [DsonCodecLinkerGroup(OutputNamespace = "Wjybxx.BTreeCodec.Codecs")]
    public class DecoratorLinker
    {
        private AlwaysCheckGuard<object> alwaysCheckGuard;
        private AlwaysFail<object> alwaysFail;
        private AlwaysRunning<object> alwaysRunning;
        private AlwaysSuccess<object> alwaysSuccess;
        private Inverter<object> inverter;
        private OnlyOnce<object> onlyOnce;
        private Repeat<object> repeat;
        private SubtreeRef<object> subtreeRef;
        private UntilCond<object> untilCond;
        private UntilFail<object> untilFail;
        private UntilSuccess<object> untilSuccess;
    }

    [DsonCodecLinkerGroup(OutputNamespace = "Wjybxx.BTreeCodec.Codecs")]
    public class LeafLinker
    {
        private Failure<object> failure;
        private Running<object> running;
        private SimpleRandom<object> simpleRandom;
        private Success<object> success;
        private WaitFrame<object> waitFrame;
    }
}
}