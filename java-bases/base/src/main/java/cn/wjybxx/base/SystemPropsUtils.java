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

package cn.wjybxx.base;

import javax.annotation.Nonnull;
import java.io.*;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Objects;
import java.util.Properties;

/**
 * 系统属性工具类
 * 除了系统属性外，我们不会再使用properties格式的配置文件，会选择json或dson.
 *
 * @author wjybxx
 * date - 2024/1/5
 */
public class SystemPropsUtils {

    /** 工作目录 */
    public static final String WORKING_DIR = System.getProperty("user.dir");

    // region convert

    public static String getString(String key) {
        return System.getProperty(key);
    }

    public static String getString(String key, String def) {
        return System.getProperty(key, def);
    }

    public static int getInt(String key) {
        return getInt(key, 0);
    }

    public static int getInt(String key, int def) {
        String v = System.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Integer.parseInt(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static long getLong(String key) {
        return getLong(key, 0);
    }

    public static long getLong(String key, long def) {
        String v = System.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Long.parseLong(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static float getFloat(String key) {
        return getFloat(key, 0f);
    }

    public static float getFloat(String key, float def) {
        String v = System.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Float.parseFloat(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static double getDouble(String key) {
        return getDouble(key, 0d);
    }

    public static double getDouble(String key, double def) {
        String v = System.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Double.parseDouble(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static boolean getBool(String key) {
        return getBool(key, false);
    }

    public static boolean getBool(String key, boolean def) {
        String v = System.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        return toBoolean(v, def);
    }

    private static boolean toBoolean(String value, boolean def) {
        if (value == null || value.isEmpty()) {
            return def;
        }
        value = value.trim().toLowerCase(); // 固定转小写
        if (value.isEmpty()) {
            return def;
        }
        return switch (value) {
            case "true", "yes", "y", "1" -> true;
            case "false", "no", "n", "0" -> false;
            default -> def;
        };
    }
    // endregion

    // region load

    /** 从普通文件中读取原始的配置 */
    public static Properties loadPropertiesFromFile(@Nonnull String path) throws IOException {
        Objects.requireNonNull(path);
        return loadPropertiesFromFile(new File(path));
    }

    public static Properties loadPropertiesFromFile(@Nonnull File file) throws IOException {
        Objects.requireNonNull(file);
        if (file.exists() && file.isFile()) {
            try (FileInputStream fileInputStream = new FileInputStream(file);
                 InputStreamReader inputStreamReader = new InputStreamReader(fileInputStream, StandardCharsets.UTF_8)) {
                Properties properties = new Properties();
                properties.load(inputStreamReader);
                return properties;
            }
        }
        throw new FileNotFoundException(file.getPath());
    }

    /** 从jar包读取配置原始的配置 */
    public static Properties loadPropertiesFromJar(String path) throws IOException {
        return loadPropertiesFromJar(path, System.class.getClassLoader());
    }

    public static Properties loadPropertiesFromJar(String path, ClassLoader classLoader) throws IOException {
        Objects.requireNonNull(path);
        final URL resource = classLoader.getResource(path);
        if (resource == null) {
            throw new FileNotFoundException(path);
        }
        try (InputStream inputStream = resource.openStream()) {
            Properties properties = new Properties();
            properties.load(inputStream);
            return properties;
        }
    }
    // endregion
}
