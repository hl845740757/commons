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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Apt;

namespace Wjybxx.BTree.Codec;

public class CodecGenerator
{
    private static readonly string fileHeader = """
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

    """;

    [Test]
    public void Gen() {
        string outDir = "D:\\Temp";
        if (!Directory.Exists(outDir)) {
            return;
        }

        CodecProcessor processor = new CodecProcessor(new List<Type>()
            {
                typeof(BtreeCodecLinker),
                typeof(BtreeCodecLinker.FsmLinker),
                typeof(BtreeCodecLinker.LeafLinker),
                typeof(BtreeCodecLinker.DecoratorLinker),
                typeof(BtreeCodecLinker.BranchLinker),
                typeof(BtreeCodecLinker.JoinPolicyLinker),
            },
            outDir,
            new List<ISpecification>()
            {
                new CodeBlockSpec(CodeBlock.Of("$L", fileHeader))
            });
        processor.codecExporterClassName = ClassName.Get("Wjybxx.BTreeCodec", "BTreeCodecExporter");
        processor.Process();
    }
}