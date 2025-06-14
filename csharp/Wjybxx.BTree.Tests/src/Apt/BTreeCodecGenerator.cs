#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using Wjybxx.BTreeCodec;
using Wjybxx.Dson.Apt2;
using Wjybxx.Dson.Codec.Attributes;

#pragma warning disable CS0169
namespace BTree.Tests.Apt;

/// <summary>
/// 反射为行为树库生成Codec
/// </summary>
[DsonCodecLinkerGroup]
public class BTreeCodecGenerator
{
    [Test]
    public void Test() {
        string directory = GetDirectory("bin") + "/../../Wjybxx.BTree.Codec/src/Generated";
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }
        var processor = new CodecProcessor(new List<Type>()
        {
            typeof(BtreeCodecLinker)
        }, directory);
        processor.Process();
    }

    /// <summary>
    /// 从工作目录向上查找指定目录
    /// </summary>
    /// <param name="dirName"></param>
    /// <returns></returns>
    private static string GetDirectory(string dirName) {
        DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        while (true) {
            if (directoryInfo.Name == dirName) {
                return directoryInfo.FullName;
            }
            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null) {
                throw new IOException($"dic {dirName} not found");
            }
        }
    }
}