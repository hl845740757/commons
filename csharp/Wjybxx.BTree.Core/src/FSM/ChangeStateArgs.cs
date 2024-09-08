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

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 状态切换参数
/// 建议用户通过原型对象的{@link #withExtraInfo(object)}等方法创建
/// </summary>
public class ChangeStateArgs
{
    public const byte CMD_NONE = 0;
    public const byte CMD_UNDO = 1;
    public const byte CMD_REDO = 2;

    /// <summary>
    /// 不延迟
    /// 1.delayArg为当前状态要设置的结果，大于0有效 -- 用于更好的支持FSM。
    /// 2.通常用于状态主动退出时，可避免自身进入被取消状态。
    /// </summary>
    public const byte DELAY_NONE = 0;
    /// <summary>
    /// 在当前子节点完成的时候切换
    /// 1.其它延迟模式也会在当前状态完成时触发
    /// 2.通常用于状态主动退出时，可避免自身进入被取消状态 -- 先调用changeState，然后setSuccess
    /// </summary>
    public const byte DELAY_CURRENT_COMPLETED = 1;

    #region 共享原型

    public static readonly ChangeStateArgs PLAIN = new(0, 0, 0, null);
    public static readonly ChangeStateArgs PLAIN_WHEN_COMPLETED = new(0, DELAY_CURRENT_COMPLETED, 0, null);

    public static readonly ChangeStateArgs PLAIN_SUCCESS = new(0, 0, TaskStatus.SUCCESS, null);
    public static readonly ChangeStateArgs PLAIN_CANCELLED = new(0, 0, TaskStatus.CANCELLED, null);
    public static readonly ChangeStateArgs PLAIN_ERROR = new(0, 0, TaskStatus.ERROR, null);

    public static readonly ChangeStateArgs UNDO = new(CMD_UNDO, 0, 0, null);
    public static readonly ChangeStateArgs UNDO_WHEN_COMPLETED = new(CMD_UNDO, DELAY_CURRENT_COMPLETED, 0, null);

    public static readonly ChangeStateArgs REDO = new(CMD_REDO, 0, 0, null);
    public static readonly ChangeStateArgs REDO_WHEN_COMPLETED = new(CMD_REDO, DELAY_CURRENT_COMPLETED, 0, null);

    #endregion

    /** 切换命名 */
    public readonly byte cmd;
    /** 延迟模式 -- 允许用户扩展 */
    public readonly byte delayMode;
    /** 期望开始运行的帧号；-1表示尚未指定 */
    public readonly int delayArg;
    /** 期望传递给Listener的数据 */
    public readonly object? extraInfo;

    /** 通过原型对象创建 */
    private ChangeStateArgs(byte cmd, byte delayMode, int delayArg, object? extraInfo) {
//        checkCmd(cmd); // 封闭构造方法后可不校验
        this.delayMode = delayMode;
        this.cmd = cmd;
        this.delayArg = delayArg;
        this.extraInfo = extraInfo;
    }

    public bool IsPlain() {
        return cmd == 0;
    }

    public bool IsUndo() {
        return cmd == CMD_UNDO;
    }

    public bool IsRedo() {
        return cmd == CMD_REDO;
    }

    #region 原型方法

    public ChangeStateArgs With(byte delayMode, int delayArg = 0) {
        if (delayMode == this.delayMode && delayArg == this.delayArg) {
            return this;
        }
        return new ChangeStateArgs(cmd, delayMode, delayArg, extraInfo);
    }

    public ChangeStateArgs With(byte delayMode, int delayArg, object? extraInfo) {
        return new ChangeStateArgs(cmd, delayMode, delayArg, extraInfo);
    }

    public ChangeStateArgs WithArg(int delayArg) {
        if (delayArg == this.delayArg) {
            return this;
        }
        return new ChangeStateArgs(cmd, delayMode, delayArg, extraInfo);
    }

    public ChangeStateArgs WithExtraInfo(object? extraInfo) {
        if (extraInfo == this.extraInfo) {
            return this;
        }
        return new ChangeStateArgs(cmd, delayMode, delayArg, extraInfo);
    }

    #endregion
}
}