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
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 提前就绪的字符流
/// </summary>
class PreparedCharStream : AbstractCharStream
{
#nullable disable
    private IEnumerator<LineInfo> _rawLines;
    private int _firstLn;
#nullable enable

    public PreparedCharStream(IEnumerator<LineInfo> rawLines, int firstLn) {
        this._rawLines = rawLines ?? throw new ArgumentNullException(nameof(rawLines));
        this._firstLn = firstLn;
    }

    protected override int FirstLn => _firstLn;

    protected override bool IsClosed() {
        return _rawLines == null;
    }

    protected override int CharAt(ref LineInfo curLine, int position) {
        return curLine.rawLine[position - curLine.startPos];
    }

    protected override void CheckUnreadOverFlow(int position) {
    }

    protected override void ScanMoreChars(ref LineInfo curLine) {
        throw new InvalidOperationException();
    }

    protected override bool ScanNextLine(in LineInfo curLine) {
        if (!_rawLines.MoveNext()) {
            return false;
        }
        LineInfo lineInfo = _rawLines.Current;
        if (lineInfo.rawLine == null || !lineInfo.IsScanCompleted()) {
            throw new DsonIOException("invalid line: " + lineInfo);
        }
        AddLine(lineInfo);
        return true;
    }

    public override void Dispose() {
        _rawLines = null;
    }
}
}