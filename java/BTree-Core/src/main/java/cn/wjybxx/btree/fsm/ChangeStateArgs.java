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
package cn.wjybxx.btree.fsm;

/**
 * 状态切换参数
 * 建议用户通过原型对象的{@link #withExtraInfo(Object)}等方法创建
 */
public final class ChangeStateArgs {

    public static final byte CMD_NONE = 0;
    public static final byte CMD_UNDO = 1;
    public static final byte CMD_REDO = 2;

    /** 不延迟 */
    public static final byte DELAY_NONE = 0;
    /**
     * 在当前子节点完成的时候切换
     * 1.其它延迟模式也会在当前状态完成时触发；
     * 2.通常用于状态主动退出时，可避免自身进入被取消状态 -- 先调用changeState，然后setSuccess;
     */
    public static final byte DELAY_CURRENT_COMPLETED = 1;
    /** 下一帧执行 */
    public static final byte DELAY_NEXT_FRAME = 2;

    // region 共享原型
    public static final ChangeStateArgs PLAIN = new ChangeStateArgs((byte) 0, (byte) 0, 0, null);
    public static final ChangeStateArgs PLAIN_WHEN_COMPLETED = new ChangeStateArgs((byte) 0, DELAY_CURRENT_COMPLETED, 0, null);
    public static final ChangeStateArgs PLAIN_NEXT_FRAME = new ChangeStateArgs((byte) 0, DELAY_NEXT_FRAME, -1, null);

    public static final ChangeStateArgs UNDO = new ChangeStateArgs(CMD_UNDO, (byte) 0, 0, null);
    public static final ChangeStateArgs UNDO_WHEN_COMPLETED = new ChangeStateArgs(CMD_UNDO, DELAY_CURRENT_COMPLETED, 0, null);
    public static final ChangeStateArgs UNDO_NEXT_FRAME = new ChangeStateArgs(CMD_UNDO, DELAY_NEXT_FRAME, -1, null);

    public static final ChangeStateArgs REDO = new ChangeStateArgs(CMD_REDO, (byte) 0, 0, null);
    public static final ChangeStateArgs REDO_WHEN_COMPLETED = new ChangeStateArgs(CMD_REDO, DELAY_CURRENT_COMPLETED, 0, null);
    public static final ChangeStateArgs REDO_NEXT_FRAME = new ChangeStateArgs(CMD_REDO, DELAY_NEXT_FRAME, -1, null);
    // endregion

    /** 切换命令 */
    public final byte cmd;
    /** 延迟模式 -- 不再限制，允许用户扩展 */
    public final byte delayMode;
    /** 期望开始运行的帧号；-1表示尚未指定 */
    public final int frame;
    /** 期望传递给Listener的数据 */
    public final Object extraInfo;

    /** 通过原型对象创建 */
    private ChangeStateArgs(byte cmd, byte delayMode, int frame, Object extraInfo) {
//        checkCmd(cmd); // 封闭构造方法后可不校验
        this.delayMode = delayMode;
        this.cmd = cmd;
        this.frame = frame;
        this.extraInfo = extraInfo;
    }

    public boolean isPlain() {
        return cmd == 0;
    }

    public boolean isUndo() {
        return cmd == CMD_UNDO;
    }

    public boolean isRedo() {
        return cmd == CMD_REDO;
    }

    // region 原型方法

    public ChangeStateArgs with(byte delayMode, int frame) {
        return new ChangeStateArgs(cmd, delayMode, frame, extraInfo);
    }

    public ChangeStateArgs with(byte delayMode, int frame, Object extraInfo) {
        return new ChangeStateArgs(cmd, delayMode, frame, extraInfo);
    }

    public ChangeStateArgs withDelayMode(byte delayMode) {
        if (delayMode == this.delayMode) {
            return this;
        }
        return new ChangeStateArgs(cmd, delayMode, frame, extraInfo);
    }

    public ChangeStateArgs withFrame(int frame) {
        if (frame == this.frame) {
            return this;
        }
        return new ChangeStateArgs(cmd, delayMode, frame, extraInfo);
    }

    public ChangeStateArgs withExtraInfo(Object extraInfo) {
        if (extraInfo == this.extraInfo) {
            return this;
        }
        return new ChangeStateArgs(cmd, delayMode, frame, extraInfo);
    }
    // endregion
}