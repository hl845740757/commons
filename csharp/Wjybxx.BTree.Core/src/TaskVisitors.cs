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

namespace Wjybxx.BTree
{
public static class TaskVisitors
{
    public static TaskVisitor<T> Reset<T>() where T : class {
        return ResetForRestartVisitor<T>.Inst;
    }

    private class ResetForRestartVisitor<T> : TaskVisitor<T> where T : class
    {
        public static readonly ResetForRestartVisitor<T> Inst = new ResetForRestartVisitor<T>();

        public void VisitChild(Task<T> child, int index, object? param) {
            child.Reset();
        }

        public void VisitHook(string name, Task<T> child, object? param) {
            child.Reset();
        }

        public void VisitList(string name, Task<T> child, int index, object? param) {
            child.Reset();
        }

        public void VisitMap<TKey>(string name, Task<T> child, TKey key, object? param) {
            child.Reset();
        }
    }
}
}