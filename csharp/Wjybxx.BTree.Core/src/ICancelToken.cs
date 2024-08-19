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

using System;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BTree
{
/// <summary>
/// 行为树模块使用的取消令牌
/// 1.行为树模块需要的功能不多，且需要进行一些特殊的优化，因此去除对Concurrent模块的依赖。
/// 2.关于取消码的设计，可查看<see cref="CancelCodes"/>类。
/// 3.继承<see cref="ICancelTokenListener"/>是为了方便通知子Token。
/// 4.在行为树模块，Task在运行期间最多只应该添加一次监听。
/// 5.Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
///
/// </summary>
public interface ICancelToken : ICancelTokenListener
{
    /// <summary>
    /// 重置状态(行为树模块取消令牌需要复用)
    /// </summary>
    void Reset();

    #region code

    /// <summary>
    /// 取消码
    /// 1.按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
    /// 2.低20位为取消原因；高12位为特殊信息 <see cref="CancelCodes.MASK_REASON"/>
    /// 3.不为0表示已发起取消请求
    /// 4.取消时至少赋值一个信息，reason通常应该赋值
    /// </summary>
    /// <value></value>
    int CancelCode { get; }

    /// <summary>
    /// 是否已收到取消信号
    /// 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
    /// </summary>
    /// <value></value>
    bool IsCancelling => CancelCode != 0;

    /// <summary>
    /// 取消的原因
    /// </summary>
    int Reason => CancelCodes.GetReason(CancelCode);

    /// <summary>
    /// 取消的紧急程度
    /// </summary>
    int Degree => CancelCodes.GetDegree(CancelCode);

    #endregion

    #region 取消操作

    /// <summary>
    /// 将Token置为取消状态
    /// </summary>
    /// <param name="cancelCode">取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}</param>
    /// <exception cref="ArgumentException">如果code小于等于0；或reason部分为0</exception>
    /// <returns>Token的当前值；如果Token已被取消，则非0；如果Token尚未被取消，则返回0。</returns>
    int Cancel(int cancelCode = CancelCodes.REASON_DEFAULT);

    #endregion

    #region 监听器

    /// <summary>
    /// 添加监听器
    /// </summary>
    /// <param name="listener"></param>
    void AddListener(ICancelTokenListener listener);

    /// <summary>
    /// 删除监听器
    /// 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
    /// </summary>
    /// <param name="listener">要删除的监听器</param>
    /// <param name="firstOccurrence">是否强制正向查找删除</param>
    /// <returns>存在匹配的监听器则返回true</returns>
    bool RemListener(ICancelTokenListener listener, bool firstOccurrence = false);

    /// <summary>
    /// 查询是否存在给定的监听器
    /// </summary>
    /// <param name="listener"></param>
    /// <returns>如果存在则返回true，否则返回false</returns>
    bool HasListener(ICancelTokenListener listener);

    #endregion

    #region 其它

    /// <summary>
    /// 创建一个同类型实例(默认只拷贝环境数据)
    /// </summary>
    /// <param name="copyCode">是否拷贝当前取消码</param>
    ICancelToken NewInstance(bool copyCode = false);

    #endregion
}
}