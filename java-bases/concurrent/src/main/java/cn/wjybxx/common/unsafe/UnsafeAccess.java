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

package cn.wjybxx.common.unsafe;

import cn.wjybxx.base.annotation.Internal;
import sun.misc.Unsafe;

import java.lang.reflect.Constructor;
import java.lang.reflect.Field;

/**
 * 该类参考自jctools的实现
 * 注意：可以通过平台类加载器加载该类以避免异常。
 *
 * @author wjybxx
 * date - 2024/1/2
 */
@Internal
public final class UnsafeAccess {

    public static final boolean SUPPORTS_GET_AND_SET_REF = true;
    public static final boolean SUPPORTS_GET_AND_ADD_LONG = true;
    public static final Unsafe UNSAFE;

    static {
        UNSAFE = getUnsafe();
    }

    private static Unsafe getUnsafe() {
        try {
            return Unsafe.getUnsafe();
        } catch (SecurityException ignore) {

        }
        Unsafe instance;
        try {
            final Field field = Unsafe.class.getDeclaredField("theUnsafe");
            field.setAccessible(true);
            instance = (Unsafe) field.get(null);
        } catch (Exception ignored) {
            // Some platforms, notably Android, might not have a sun.misc.Unsafe implementation with a private
            // `theUnsafe` static instance. In this case we can try to call the default constructor, which is sufficient
            // for Android usage.
            try {
                Constructor<Unsafe> c = Unsafe.class.getDeclaredConstructor();
                c.setAccessible(true);
                instance = c.newInstance();
            } catch (Exception e) {
                throw new ExceptionInInitializerError(e);
            }
        }
        return instance;
    }

    @SuppressWarnings("deprecation")
    public static long fieldOffset(Class<?> clz, String fieldName) throws RuntimeException {
        try {
            return UNSAFE.objectFieldOffset(clz.getDeclaredField(fieldName));
        } catch (NoSuchFieldException e) {
            throw new RuntimeException(e);
        }
    }
}
