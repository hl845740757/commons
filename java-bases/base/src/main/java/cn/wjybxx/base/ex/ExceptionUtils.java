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

package cn.wjybxx.base.ex;

import cn.wjybxx.base.CollectionUtils;

import java.util.ArrayList;
import java.util.List;

/**
 * 异常工具类
 * <p>
 * 这里部分修改自Commons-Lang3，为避免依赖，我们选择拷贝代码。
 * 我用的最多的就是{@link #rethrow(Throwable)}...
 *
 * @author wjybxx
 * date - 2024/1/3
 */
public class ExceptionUtils {

    /** 获取异常的根 */
    public static Throwable getRootCause(final Throwable throwable) {
        final List<Throwable> list = getThrowableList(throwable);
        return list.isEmpty() ? null : list.getLast();
    }

    /** 展开异常信息 */
    public static List<Throwable> getThrowableList(Throwable throwable) {
        final List<Throwable> list = new ArrayList<>(4);
        while (throwable != null && !CollectionUtils.containsRef(list, throwable)) {
            list.add(throwable);
            throwable = throwable.getCause();
        }
        return list;
    }

    /** 是否是受检异常 */
    public static boolean isChecked(final Throwable throwable) {
        return !(throwable instanceof Error || throwable instanceof RuntimeException);
    }

    /** 是否是非受检异常 -- 通常指运行时异常 */
    public static boolean isUnchecked(final Throwable throwable) {
        return (throwable instanceof Error || throwable instanceof RuntimeException);
    }

    /**
     * 抛出原始异常，消除编译时警告
     *
     * @param <R> 方法正常执行的返回值类型
     */
    public static <R> R rethrow(final Throwable throwable) {
        return ExceptionUtils.throwAsUncheckedException(throwable);
    }

    /**
     * 如果异常是非受检异常，则直接抛出，否则返回异常对象。
     */
    public static <T extends Throwable> T throwUnchecked(final T throwable) {
        if (isUnchecked(throwable)) {
            return ExceptionUtils.throwAsUncheckedException(throwable);
        }
        return throwable; // 返回异常
    }

    /**
     * @param <R> 方法正常执行的返回值类型
     * @param <T> 异常类型约束
     */
    @SuppressWarnings("unchecked")
    private static <R, T extends Throwable> R throwAsUncheckedException(final Throwable throwable) throws T {
        throw (T) throwable;
    }

}