/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.common.concurrent2;

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.function.Consumer;

/**
 * context可以看做简化的promise，
 * <p>
 * <p>
 * promise是完全可以充当取消令牌的
 *
 * @author wjybxx
 * date - 2023/11/18
 */
public class Context implements IContext {

    private final IContext parent;
    private final Object blackboard;

    private volatile int code;
    private volatile Completion stack;

    public Context(IContext parent, Object blackboard) {
        this.parent = parent;
        this.blackboard = blackboard;
    }

    private Context(IContext parent, Object blackboard, int code) {
        this.parent = parent;
        this.blackboard = blackboard;
        this.code = code;
    }

    /** 允许子类重写 */
    protected Context newContext(IContext parent, Object blackboard) {
        return new Context(parent, blackboard);
    }

    protected ReadonlyContext newReadonlyContext() {
        return new ReadonlyContext(this);
    }

    protected final Context newChild(IContext parent, Object blackboard) {
        Context context = newContext(parent, blackboard);
        int code = this.code;
        if (code != 0) {
            VH_CODE.setRelease(context, code);
        } else {
            this.onCancelRequested(new Signaller(context));
        }
        return context;
    }

    @Override
    public IContext parent() {
        return parent;
    }

    @Override
    public Object blackboard() {
        return blackboard;
    }

    @Override
    public IContext newChild() {
        return newChild(this, blackboard);
    }

    @Override
    public IContext newChild(Object blackboard) {
        return newChild(this, blackboard);
    }

    @Override
    public IContext asReadonly() {
        return newReadonlyContext();
    }

    @Override
    public final boolean isReadOnly() {
        return false;
    }

    @Override
    public final int cancel(int fullCode) {
        return 0;
    }

    @Override
    public final int cancelCode() {
        return code;
    }

    @Override
    public final boolean isCancelling() {
        return code != 0;
    }

    @Override
    public final int reason() {
        return (code & MASK_REASON);
    }

    @Override
    public final int urgencyDegree() {
        return (code & MASK_DEGREE) >> 16;
    }

    @Override
    public final boolean isInterruptible() {
        return (code & MASK_INTERRUPT) != 0;
    }

    @Override
    public void onCancelRequested(Consumer<? super IContext> action) {
        if (isCancelling()) {
            action.accept(this);
        } else {
            // TODO addListener
        }
    }

    private static final VarHandle VH_CODE;
    private static final VarHandle VH_STACK;
    private static final VarHandle VH_NEXT;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_CODE = l.findVarHandle(Context.class, "code", int.class);
            VH_STACK = l.findVarHandle(Context.class, "stack", Completion.class);
            VH_NEXT = l.findVarHandle(Completion.class, "next", Completion.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    // region

    private static class Completion {

        /** 非volatile，通过{@link Context#stack}的原子更新来保证可见性 */
        Completion next;

    }

    /** 表示stack已被清理 */
    private static final Completion TOMBSTONE = new Completion() {
    };

    private static class Signaller extends Completion implements Consumer<IContext> {

        final Context context;

        private Signaller(Context context) {
            this.context = context;
        }

        @Override
        public void accept(IContext context) {
            this.context.cancel(context.cancelCode());
        }
    }

    protected static class ReadonlyContext implements IContext {

        protected final Context back;

        protected ReadonlyContext(Context back) {
            this.back = back;
        }

        @Override
        public IContext parent() {
            return back.parent;
        }

        @Override
        public Object blackboard() {
            return back.blackboard;
        }

        @Override
        public IContext newChild() {
            // 万不可直接调度员 back.newChild();
            // back.newChild的parent是back，那么readonly的封装就失败了，因为用户拿到了底层的context
            return back.newChild(this, back.blackboard);
        }

        @Override
        public IContext newChild(Object blackboard) {
            return back.newChild(this, blackboard);
        }

        @Override
        public ReadonlyContext asReadonly() {
            return this;
        }

        @Override
        public final boolean isReadOnly() {
            return true;
        }

        @Override
        public int cancel(int fullCode) {
            return back.cancel(fullCode);
        }

        @Override
        public int cancelCode() {
            return back.code;
        }

        @Override
        public void onCancelRequested(Consumer<? super IContext> action) {
            back.onCancelRequested(action);
        }
    }

    // endregion
}