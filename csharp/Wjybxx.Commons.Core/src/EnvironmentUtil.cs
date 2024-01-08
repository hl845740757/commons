#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

using System;

namespace Wjybxx.Commons;

/// <summary>
/// 环境变量工具类
/// </summary>
public class EnvironmentUtil
{
    /// <summary>
    /// 当前工作目录
    /// </summary>
    public static string WorkingDir => Environment.CurrentDirectory;

    #region 获取环境变量

    /// <summary>
    /// 获取原始的String类型环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <returns></returns>
    public static string? GetStringVar(string varName) {
        return Environment.GetEnvironmentVariable(varName);
    }

    /// <summary>
    /// 获取原始的String类型环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static string GetStringVar(string varName, string def) {
        string rawValue = Environment.GetEnvironmentVariable(varName);
        return rawValue == null ? def : rawValue;
    }

    /// <summary>
    /// 获取int类型的环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static int GetIntVar(string varName, int def = 0) {
        string rawValue = Environment.GetEnvironmentVariable(varName);
        if (string.IsNullOrEmpty(rawValue)) {
            return def;
        }
        if (int.TryParse(rawValue, out int value)) {
            return value;
        }
        return def;
    }

    /// <summary>
    /// 获取long类型的环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static long GetLongVar(string varName, long def = 0) {
        string rawValue = Environment.GetEnvironmentVariable(varName);
        if (string.IsNullOrEmpty(rawValue)) {
            return def;
        }
        if (long.TryParse(rawValue, out long value)) {
            return value;
        }
        return def;
    }

    /// <summary>
    /// 获取float类型的环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static float GetFloatVar(string varName, float def = 0) {
        string rawValue = Environment.GetEnvironmentVariable(varName);
        if (string.IsNullOrEmpty(rawValue)) {
            return def;
        }
        if (float.TryParse(rawValue, out float value)) {
            return value;
        }
        return def;
    }


    /// <summary>
    /// 获取double类型的环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static double GetDoubleVar(string varName, double def = 0) {
        string rawValue = Environment.GetEnvironmentVariable(varName);
        if (string.IsNullOrEmpty(rawValue)) {
            return def;
        }
        if (double.TryParse(rawValue, out double value)) {
            return value;
        }
        return def;
    }

    /// <summary>
    /// 获取bool类型的环境变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static bool GetBoolVar(string varName, bool def = false) {
        string rawValue = Environment.GetEnvironmentVariable(varName);
        return ToBoolean(rawValue, def);
    }

    /// <summary>
    /// 将环境变量的字符串值转换为bool类型
    /// 应尽量避免大写字符。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="def"></param>
    /// <returns></returns>
    public static bool ToBoolean(string? value, bool def) {
        if (string.IsNullOrEmpty(value)) {
            return def;
        }
        value = value.Trim().ToLower(); // 固定转小写
        if (string.IsNullOrEmpty(value)) {
            return def;
        }
        return value switch
        {
            "true" => true,
            "yes" => true,
            "Y" => true,
            "1" => true,
            "false" => false,
            "no" => false,
            "n" => false,
            "0" => false,
            _ => def
        };
    }

    #endregion
}