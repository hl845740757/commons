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

package cn.wjybxx.common.concurrent;

import cn.wjybxx.base.SystemPropsUtils;
import cn.wjybxx.base.ex.NoLogRequiredException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Collection;
import java.util.Collections;
import java.util.Objects;
import java.util.Set;
import java.util.concurrent.CancellationException;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 该类用于打印并发库中的错误日志
 *
 * @author wjybxx
 * date - 2024/1/13
 */
public final class FutureLogger {

    private static final Logger logger = LoggerFactory.getLogger(Promise.class);
    /**
     * 是否记录日志
     * <p>
     * 在使用{@link CompletableFuture}的过程中，有一个头疼的问题：出现异常时毫无表现，排查错误异常困难。
     * 由于异常默认会被捕获传递，因此只有链的末尾才可以准确记录错误日志；
     * 但没有人能做到总是链的末尾添加一个记录日志的Action，因此有时候程序出现错误，而开发者毫不知情。
     * <p>
     * 保守安全的方式是在底层自动记录错误，这可能记录一部分冗余的日志，但随着错误的减少，增加的开销也会变少。
     * 如果一个异常不需要记录，可实现{@link NoLogRequiredException}接口，或者通过{@link #addNoLogRequiredException(Class)}指定。
     * <p>
     * 如果继续使用{@link CompletableFuture}，那么安全的方式就是继承它，然后将用户的每一个Action都封装一层，在其抛出异常时先记录日志，再抛出。
     * 但这种方式代码丑陋且低效。
     */
    private static final boolean logCause = SystemPropsUtils.getBool("cn.wjybxx.concurrent.FutureLogger.logCause", true);
    private static final Set<Class<?>> noLogRequiredExceptions = Collections.newSetFromMap(new ConcurrentHashMap<>());

    /** 添加一个无需自动记录日志的异常 */
    public static void addNoLogRequiredException(Class<? extends Throwable> ex) {
        checkExceptionClass(ex);
        noLogRequiredExceptions.add(ex);
    }

    /** 批量添加 */
    public static void addNoLogRequiredExceptions(Collection<? extends Class<? extends Throwable>> classes) {
        Objects.requireNonNull(classes);
        for (Class<? extends Throwable> exClass : classes) {
            checkExceptionClass(exClass);
        }
        noLogRequiredExceptions.addAll(classes); // 底层并无特别优化
    }

    /** 删除一个异常类型 */
    public static boolean removeNoLogRequiredException(Class<? extends Throwable> ex) {
        Objects.requireNonNull(ex);
        return noLogRequiredExceptions.remove(ex);
    }

    private static void checkExceptionClass(Class<? extends Throwable> ex) {
        Objects.requireNonNull(ex);
        if (!Throwable.class.isAssignableFrom(ex)) {
            throw new IllegalArgumentException();
        }
    }

    private static boolean testException(Throwable x) {
        if (!logCause || x == null) {
            return false;
        }
        return !(x instanceof NoLogRequiredException)
                && !(x instanceof CancellationException)
                && !noLogRequiredExceptions.contains(x.getClass());
    }

    public static void logCause(Throwable x, String message) {
        Objects.requireNonNull(message);
        if (testException(x)) {
            try {
                logger.warn(message, x);
            } catch (Throwable ignore) {
                // 万一日志挂了...
            }
        }
    }

    public static void logCause(Throwable x) {
        if (testException(x)) {
            try {
                logger.warn("future completed with exception", x);
            } catch (Throwable ignore) {
                // 万一日志挂了...
            }
        }
    }
}