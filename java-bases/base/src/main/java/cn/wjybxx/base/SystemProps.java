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

/**
 * 系统属性类
 * (不直接定义为Utils类，CommonsLang3那种支持的内容太多了...)
 *
 * @author wjybxx
 * date - 2024/1/5
 */
public class SystemProps {

    /** 工作目录 */
    public static final String WORKING_DIR = System.getProperty("user.dir");

    public static String getString(String key) {
        return PropertiesUtils.getString(System.getProperties(), key);
    }

    public static String getString(String key, String def) {
        return PropertiesUtils.getString(System.getProperties(), def);
    }

    public static int getInt(String key) {
        return PropertiesUtils.getInt(System.getProperties(), key, 0);
    }

    public static int getInt(String key, int def) {
        return PropertiesUtils.getInt(System.getProperties(), key, def);
    }

    public static long getLong(String key) {
        return PropertiesUtils.getLong(System.getProperties(), key, 0);
    }

    public static long getLong(String key, long def) {
        return PropertiesUtils.getLong(System.getProperties(), key, def);
    }

    public static float getFloat(String key) {
        return PropertiesUtils.getFloat(System.getProperties(), key, 0f);
    }

    public static float getFloat(String key, float def) {
        return PropertiesUtils.getFloat(System.getProperties(), key, def);
    }

    public static double getDouble(String key) {
        return PropertiesUtils.getDouble(System.getProperties(), key, 0d);
    }

    public static double getDouble(String key, double def) {
        return PropertiesUtils.getDouble(System.getProperties(), key, def);
    }

    public static boolean getBool(String key) {
        return PropertiesUtils.getBool(System.getProperties(), key, false);
    }

    public static boolean getBool(String key, boolean def) {
        return PropertiesUtils.getBool(System.getProperties(), key, def);
    }
}
