﻿using System;
using System.Collections.Generic;

namespace BlueGOAP
{
    public interface IPlanHandler<TAction>
    {
        /// <summary>
        /// 当前计划是否完成
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="plan"></param>
        void Init(IActionManager<TAction> actionManager, Queue<IActionHandler<TAction>> plan);

        void AddCompleteCallBack(Action onComplete);

        /// <summary>
        /// 执行下一个动作
        /// </summary>
        void HandlerAction();

        /// <summary>
        /// 中断计划
        /// </summary>
        void Interruptible();

        /// <summary>
        /// 获取当前动作处理器
        /// </summary>
        /// <returns></returns>
        IActionHandler<TAction> GetCurrentHandler();
    }
}