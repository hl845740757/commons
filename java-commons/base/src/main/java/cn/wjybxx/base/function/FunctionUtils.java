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

package cn.wjybxx.base.function;

import java.util.Objects;
import java.util.function.*;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class FunctionUtils {

    private static final IntConsumer _emptyIntConsumer = v -> {};
    private static final LongConsumer _emptyLongConsumer = v -> {};
    private static final DoubleConsumer _emptyDoubleConsumer = v -> {};
    private static final Runnable _emptyRunnable = () -> {};

    private static final Function<?, ?> identity = t -> t;
    private static final Consumer<?> _emptyConsumer = v -> {};
    private static final BiConsumer<?, ?> _emptyBiConsumer = (a, b) -> {};

    private static final Predicate<?> _alwaysTrue = v -> true;
    private static final Predicate<?> _alwaysFalse = v -> false;
    private static final BiPredicate<?, ?> _biAlwaysTrue = (t, u) -> true;
    private static final BiPredicate<?, ?> _biAlwaysFalse = (t, u) -> false;

    private static final Function<?, String> _toString = Object::toString;
    private static final Predicate<?> _notNull = Objects::nonNull;
    private static final Predicate<?> _isNull = Objects::isNull;

    // region 单例

    public static IntConsumer emptyIntConsumer() {
        return _emptyIntConsumer;
    }

    public static LongConsumer emptyLongConsumer() {
        return _emptyLongConsumer;
    }

    public static DoubleConsumer emptyDoubleConsumer() {
        return _emptyDoubleConsumer;
    }

    public static Runnable emptyRunnable() {
        return _emptyRunnable;
    }

    @SuppressWarnings("unchecked")
    public static <T> Function<T, T> identity() {
        return (Function<T, T>) identity;
    }

    @SuppressWarnings("unchecked")
    public static <T> Consumer<T> emptyConsumer() {
        return (Consumer<T>) _emptyConsumer;
    }

    @SuppressWarnings("unchecked")
    public static <T, U> BiConsumer<T, U> emptyBiConsumer() {
        return (BiConsumer<T, U>) _emptyBiConsumer;
    }

    @SuppressWarnings("unchecked")
    public static <T> Predicate<T> alwaysTrue() {
        return (Predicate<T>) _alwaysTrue;
    }

    @SuppressWarnings("unchecked")
    public static <T> Predicate<T> alwaysFalse() {
        return (Predicate<T>) _alwaysFalse;
    }

    @SuppressWarnings("unchecked")
    public static <T, U> BiPredicate<T, U> biAlwaysTrue() {
        return (BiPredicate<T, U>) _biAlwaysTrue;
    }

    @SuppressWarnings("unchecked")
    public static <T, U> BiPredicate<T, U> biAlwaysFalse() {
        return (BiPredicate<T, U>) _biAlwaysFalse;
    }

    @SuppressWarnings("unchecked")
    public static <T> Function<T, String> toStringFunction() {
        return (Function<T, String>) _toString;
    }

    @SuppressWarnings("unchecked")
    public static <T> Predicate<T> isNull() {
        return (Predicate<T>) _isNull;
    }

    @SuppressWarnings("unchecked")
    public static <T> Predicate<T> notNull() {
        return (Predicate<T>) _notNull;
    }
    // endregion

    // region 适配

    public static <T, R> Function<T, R> toFunction(Consumer<T> action, R result) {
        if (action == null) throw new NullPointerException();
        return t -> {
            action.accept(t);
            return result;
        };
    }

    public static <T> Function<T, Object> toFunction(Consumer<T> action) {
        if (action == null) throw new NullPointerException();
        return t -> {
            action.accept(t);
            return null;
        };
    }

    public static <T, R> Consumer<T> toConsumer(Function<T, R> function) {
        if (function == null) throw new NullPointerException();
        return function::apply;
    }

    public static <T, U, R> BiFunction<T, U, R> toBiFunction(BiConsumer<T, U> action) {
        if (action == null) throw new NullPointerException();
        return (t, u) -> {
            action.accept(t, u);
            return null;
        };
    }

    public static <T, U, R> BiFunction<T, U, R> toBiFunction(BiConsumer<T, U> action, R r) {
        if (action == null) throw new NullPointerException();
        return (t, u) -> {
            action.accept(t, u);
            return r;
        };
    }

    public static <T, U, R> BiConsumer<T, U> toBiConsumer(BiFunction<T, U, R> function) {
        if (function == null) throw new NullPointerException();
        return function::apply;
    }

    // endregion

}