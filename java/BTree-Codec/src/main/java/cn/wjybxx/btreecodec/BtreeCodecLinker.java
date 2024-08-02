/*
 * Copyright 2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package cn.wjybxx.btreecodec;

import cn.wjybxx.btree.TaskEntry;
import cn.wjybxx.btree.branch.*;
import cn.wjybxx.btree.branch.join.*;
import cn.wjybxx.btree.decorator.*;
import cn.wjybxx.btree.fsm.ChangeStateTask;
import cn.wjybxx.btree.fsm.StateMachineTask;
import cn.wjybxx.btree.leaf.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecLinker;
import cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerGroup;
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

/**
 * 这是一个配置文件，用于生成行为树关联的codec
 *
 * @author wjybxx
 * date - 2023/12/24
 */
@DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btreecodec")
public class BtreeCodecLinker {

    public TaskEntry<?> taskEntry;

    @DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btreecodec.fsm")
    private static class FsmLinker {
        private ChangeStateTask<?> changeStateTask;
        private StateMachineTask<?> stateMachineTask;
    }

    @DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btreecodec.branch")
    private static class BranchLinker {
        private ActiveSelector<?> activeSelector;
        private FixedSwitch<?> fixedSwitch;
        private Foreach<?> foreachTask;
        private Join<?> join;
        private Selector<?> selector;
        private SelectorN<?> selectorN;
        private Sequence<?> sequence;
        private ServiceParallel<?> serviceParallel;
        private SimpleParallel<?> simpleParallel;
        private Switch<?> switchTask;
    }

    @DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btreecodec.branch.join")
    private static class JoinPolicyLinker {
        @DsonCodecLinker(props = @DsonSerializable(singleton = "getInstance"))
        private JoinAnyOf<?> joinAnyOf;
        @DsonCodecLinker(props = @DsonSerializable(singleton = "getInstance"))
        private JoinMain<?> joinMain;
        @DsonCodecLinker(props = @DsonSerializable(singleton = "getInstance"))
        private JoinSelector<?> joinSelector;
        @DsonCodecLinker(props = @DsonSerializable(singleton = "getInstance"))
        private JoinSequence<?> joinSequence;
        @DsonCodecLinker(props = @DsonSerializable(singleton = "getInstance"))
        private JoinWaitAll<?> joinWaitAll;
        // selectorN有状态，不能单例
        private JoinSelectorN<?> joinSelectorN;
    }

    @DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btreecodec.decorator")
    private static class DecoratorLinker {
        private AlwaysCheckGuard<?> alwaysCheckGuard;
        private AlwaysFail<?> alwaysFail;
        private AlwaysRunning<?> alwaysRunning;
        private AlwaysSuccess<?> alwaysSuccess;
        private Inverter<?> inverter;
        private OnlyOnce<?> onlyOnce;
        private Repeat<?> repeat;
        private SubtreeRef<?> subtreeRef;
        private UntilCond<?> untilCond;
        private UntilFail<?> untilFail;
        private UntilSuccess<?> untilSuccess;
    }

    @DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btreecodec.leaf")
    private static class LeafLinker {
        private Failure<?> failure;
        private Running<?> running;
        private SimpleRandom<?> simpleRandom;
        private Success<?> success;
        private WaitFrame<?> waitFrame;
    }
}