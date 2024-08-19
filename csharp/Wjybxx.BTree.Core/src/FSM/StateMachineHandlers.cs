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

using System;

namespace Wjybxx.BTree.FSM
{
public static class StateMachineHandlers
{
    public static IStateMachineHandler<T> DefaultHandler<T>() where T : class {
        return DefaultStateMachineHandler<T>.Inst;
    }

    public static IStateMachineHandler<T> RedoHandler<T>() where T : class {
        return CRedoHandler<T>.Inst;
    }

    public static IStateMachineHandler<T> UndoHandler<T>() where T : class {
        return CUndoHandler<T>.Inst;
    }

    public static IStateMachineHandler<T> OfListener<T>(StateMachineListener<T> listener) where T : class {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        return new ListenerHandler<T>(listener);
    }

    private class DefaultStateMachineHandler<T> : IStateMachineHandler<T> where T : class
    {
        internal static readonly DefaultStateMachineHandler<T> Inst = new DefaultStateMachineHandler<T>();

        public void ResetForRestart(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeEnter(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
        }

        public int OnChildCompleted(StateMachineTask<T> stateMachineTask, Task<T> curState) {
            return TaskStatus.RUNNING;
        }
    }

    private class ListenerHandler<T> : IStateMachineHandler<T> where T : class
    {
        private readonly StateMachineListener<T> _listener;

        public ListenerHandler(StateMachineListener<T> listener) {
            _listener = listener;
        }

        public void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
            _listener.Invoke(stateMachineTask, curState, nextState);
        }

        public void ResetForRestart(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeEnter(StateMachineTask<T> stateMachineTask) {
        }

        public int OnChildCompleted(StateMachineTask<T> stateMachineTask, Task<T> curState) {
            return TaskStatus.RUNNING;
        }
    }

    private class CRedoHandler<T> : IStateMachineHandler<T> where T : class
    {
        internal static readonly CRedoHandler<T> Inst = new CRedoHandler<T>();

        public bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            if (stateMachineTask.RedoChangeState()) {
                return true;
            }
            stateMachineTask.SetCompleted(preState.Status, true);
            return true;
        }

        public void ResetForRestart(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeEnter(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
        }

        public int OnChildCompleted(StateMachineTask<T> stateMachineTask, Task<T> curState) {
            return TaskStatus.RUNNING;
        }
    }

    private class CUndoHandler<T> : IStateMachineHandler<T> where T : class
    {
        internal static readonly CUndoHandler<T> Inst = new CUndoHandler<T>();

        public bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            if (stateMachineTask.UndoChangeState()) {
                return true;
            }
            stateMachineTask.SetCompleted(preState.Status, true);
            return true;
        }

        public void ResetForRestart(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeEnter(StateMachineTask<T> stateMachineTask) {
        }

        public void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
        }

        public int OnChildCompleted(StateMachineTask<T> stateMachineTask, Task<T> curState) {
            return TaskStatus.RUNNING;
        }
    }
}
}