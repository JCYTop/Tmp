using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueGOAP
{
    public abstract class ActionManagerBase<TAction, TGoal> : IActionManager<TAction>
    {
        /// <summary>
        /// 动作字典
        /// </summary>
        private Dictionary<TAction, IActionHandler<TAction>> actionHandlerDic;

        /// <summary>
        /// 动作状态字典
        /// </summary>
        private Dictionary<TAction, IActionHandler<TAction>> actionStateHandlers;

        /// <summary>
        /// 能够打断计划的动作
        /// </summary>
        private List<IActionHandler<TAction>> interruptibleHandlers;

        /// <summary>
        /// 动作 状态机
        /// </summary>
        private IFSM<TAction> actionFsm;

        /// <summary>
        /// 动作状态 状态机
        /// </summary>
        private IFSM<TAction> actionStateFsm;

        private IAgent<TAction, TGoal> agent;

        /// <summary>
        /// 当前是否在执行动的标志位（用于避免动作已经结束，却还在执行帧函数的情况）
        /// </summary>
        public bool IsPerformAction { get; set; }

        /// <summary>
        /// 效果的键值和动作的映射关系
        /// </summary>
        public Dictionary<string, HashSet<IActionHandler<TAction>>> EffectsAndActionMap { get; private set; }

        //动作完成的回调
        private Action<TAction> onActionComplete;

        public ActionManagerBase(IAgent<TAction, TGoal> agent)
        {
            this.agent = agent;
            actionHandlerDic = new Dictionary<TAction, IActionHandler<TAction>>();
            actionStateHandlers = new Dictionary<TAction, IActionHandler<TAction>>();
            interruptibleHandlers = new List<IActionHandler<TAction>>();
            actionFsm = new ActionFSM<TAction>();
            actionStateFsm = new ActionStateFSM<TAction>();
            InitActionHandlers();
            InitActionStateHandlers();
            InitFsm();
            InitActionStateFSM();
            InitEffectsAndActionMap();
            InitInterruptiblers();
        }

        /// <summary>
        /// 初始化当前代理的动作处理器
        /// </summary>
        protected abstract void InitActionHandlers();

        /// <summary>
        /// 初始化当前可叠加执行动作处理器
        /// </summary>
        protected abstract void InitActionStateHandlers();

        /// <summary>
        /// 初始化动作和动作影响的映射
        /// </summary>
        private void InitEffectsAndActionMap()
        {
            EffectsAndActionMap = new Dictionary<string, HashSet<IActionHandler<TAction>>>();
            foreach (var handler in actionHandlerDic)
            {
                IState state = handler.Value.Action.Effects;
                if (state == null)
                    continue;
                foreach (string key in state.GetKeys())
                {
                    if (!EffectsAndActionMap.ContainsKey(key) || EffectsAndActionMap[key] == null)
                        EffectsAndActionMap[key] = new HashSet<IActionHandler<TAction>>();
                    EffectsAndActionMap[key].Add(handler.Value);
                }
            }
        }

        /// <summary>
        /// 初始化能够打断计划的动作缓存
        /// </summary>
        private void InitInterruptiblers()
        {
            foreach (KeyValuePair<TAction, IActionHandler<TAction>> handler in actionHandlerDic)
            {
                if (handler.Value.Action.CanInterruptiblePlan)
                {
                    interruptibleHandlers.Add(handler.Value);
                }
            }

            //按照优先级排序
            interruptibleHandlers = interruptibleHandlers.OrderByDescending(u => u.Action.Priority).ToList();
        }

        public abstract TAction GetDefaultActionLabel();

        public void AddActionHandler(TAction label)
        {
            AddHandler(label, actionHandlerDic);
        }

        public void AddActionStateHandler(TAction label)
        {
            AddHandler(label, actionStateHandlers);
        }

        private void AddHandler(TAction label, Dictionary<TAction, IActionHandler<TAction>> dic)
        {
            var handler = agent.Maps.GetActionHandler(label);
            if (handler != null)
            {
                dic.Add(label, handler);
                //这里写拉姆达表达式，是为了避免初始化的时候_onActionComplete还是null的
                handler.AddFinishCallBack(() =>
                {
                    DebugMsg.Log("动作完成：   " + label);
                    onActionComplete(label);
                });
            }
            else
            {
                DebugMsg.LogError("映射文件中未找到对应Handler,标签为:" + label);
            }
        }

        public void RemoveHandler(TAction actionLabel)
        {
            actionHandlerDic.Remove(actionLabel);
        }

        public IActionHandler<TAction> GetHandler(TAction actionLabel)
        {
            if (actionHandlerDic.ContainsKey(actionLabel))
            {
                return actionHandlerDic[actionLabel];
            }

            DebugMsg.LogError("Not add action name:" + actionLabel);
            return null;
        }

        //判断是否有能够打断计划的动作执行
        private void JudgeInterruptibleHandler()
        {
            foreach (var handler in interruptibleHandlers)
            {
                if (handler.CanPerformAction())
                {
                    DebugMsg.Log(handler.Label + "打断计划");
                    agent.Performer.Interruptible();
                    break;
                }
            }
        }

        //判断是否有满足条件的可叠加动作
        private void JudgeConformActionState()
        {
            foreach (KeyValuePair<TAction, IActionHandler<TAction>> pair in actionStateHandlers)
            {
                if (agent.AgentState.ContainState(pair.Value.Action.Preconditions))
                {
                    if (pair.Value.ExcuteState == ActionExcuteState.INIT || pair.Value.ExcuteState == ActionExcuteState.EXIT)
                        ExcuteHandler(pair.Key);
                }
            }
        }

        public virtual void ExcuteHandler(TAction label)
        {
            if (actionHandlerDic.ContainsKey(label))
            {
                actionFsm.ExcuteNewState(label);
            }
            else if (actionStateHandlers.ContainsKey(label))
            {
                actionStateFsm.ExcuteNewState(label);
            }
            else
            {
                DebugMsg.LogError("动作" + label + "不在当前动作缓存内");
            }
        }

        public void AddActionCompleteListener(Action<TAction> actionComplete)
        {
            onActionComplete = actionComplete;
        }

        private void InitFsm()
        {
            foreach (KeyValuePair<TAction, IActionHandler<TAction>> handler in actionHandlerDic)
            {
                actionFsm.AddState(handler.Key, handler.Value);
            }
        }

        private void InitActionStateFSM()
        {
            foreach (var handler in actionStateHandlers)
            {
                actionStateFsm.AddState(handler.Key, handler.Value);
            }
        }

        public void Update()
        {
            if (IsPerformAction)
                actionFsm.FrameFun();
            actionStateFsm.FrameFun();
        }

        public void UpdateData()
        {
            JudgeInterruptibleHandler();
            JudgeConformActionState();
        }
    }
}