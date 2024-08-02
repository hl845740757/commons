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
using System.IO;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// Dson字符流
/// </summary>
public interface IDsonCharStream : IDisposable
{
    /// <summary>
    /// 1.如果产生换行，则返回 -2
    /// 2.如果到达文件尾，则返回 -1 -- 且下一次read抛出异常。
    /// 3.其它情况下返回对应字符
    ///
    /// 注意：
    /// 1.首次读也会产生换行
    /// 2.position不一定是加 1，与换行符相关
    /// 
    /// </summary>
    /// <returns>下一字符或事件</returns>
    int Read();

    /// <summary>
    /// 1.如果产生换行，则返回 -2
    /// 2.退出eof状态，则返回 -1
    /// 3.其它情况下返回 0
    ///
    /// 1.回退可以是有限制的，以节省开销
    /// 2.unread后再read必须返回相同的值
    /// 3.position不一定是减1，与换行符相关
    /// </summary>
    /// <returns>事件</returns>
    int Unread();

    /// <summary>
    /// 跳过当前行，下次读取时产生换行或eof
    /// </summary>
    void SkipLine();

    /// <summary>
    /// 当前已读位置
    /// 1.初始位置-1，表示尚未开始；如果是空文件，position也将始终是-1
    /// 2.有效值0-base，第1行第1个字符位置为0 -- 我们更需要当前位置而不是下一个位置
    /// 3.换行时position可能不连续，如果换行符是\r\n
    /// </summary>
    /// <value></value>
    int Position { get; }

    /// <summary>
    /// 获取当前行数据
    /// </summary>
    /// <value></value>
    LineInfo? CurLine { get; }

    /**
  * 获取行号
  * 1.初始0，表示尚未开始
  * 2.初始行号可能不为1，部分输入流可能是截断的
  */
    int Ln {
        get {
            LineInfo curLine = CurLine;
            return curLine == null ? 0 : curLine.ln;
        }
    }

    /**
     * 获取列号
     * 1. 初始0，表示尚未开始
     * 2. 正式值从1开始。
     */
    int Column {
        get {
            LineInfo curLine = CurLine;
            return curLine == null ? 0 : (Position - curLine.startPos + 1);
        }
    }

    /// <summary>
    /// 丢弃指定位置之前的缓存
    /// 该接口用于外部告诉Buffer可以安全的丢弃不再读取字符位置
    /// 注意：并不是只有调用该接口的时候才触发丢弃字符，Stream为了控制内存在{@link #read()}的时候是可能丢弃字符的。
    /// </summary>
    /// <param name="position">已读取位置，该位置的字符需要保留；position可能是一个估测值，因此position小于等于0则不处理</param>
    void DiscardReadChars(int position) {
//        assert position <= getPosition();
    }

    #region 工厂方法

    /** 创建一个基于string的字符流 */
    public static IDsonCharStream NewCharStream(string dsonString) {
        return new StringCharStream(dsonString);
    }

    /// <summary>
    /// 创建一个基于TextReader的带缓存的Dson字符串流
    /// </summary>
    /// <param name="reader">Stream流</param>
    /// <param name="autoClose">是否自动关闭Stream</param>
    /// <returns></returns>
    public static IDsonCharStream NewBufferedCharStream(TextReader reader, bool autoClose = true) {
        return new BufferedCharStream(reader, autoClose);
    }

    #endregion
}
}