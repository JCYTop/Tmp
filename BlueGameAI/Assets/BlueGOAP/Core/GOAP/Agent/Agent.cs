using System;

namespace BlueGOAP
{
    public abstract class AgentBase<TAction, TGoal> : IAgent<TAction, TGoal>
        where TAction : struct
        where TGoal : struct
    {
        public abstract bool IsAgentOver { get; }
        public IState AgentState { get; private set; }
        public IMaps<TAction, TGoal> Maps { get; protected set; }
        public IActionManager<TAction> ActionManager { get; protected set; }
        public IGoalManager<TGoal> GoalManager { get; private set; }
        public IPerformer Performer { get; private set; }

        private ITriggerManager _triggerManager;
        protected Action<IAgent<TAction, TGoal>, IMaps<TAction, TGoal>> _onInitGameData;

        public AgentBase(Action<IAgent<TAction, TGoal>, IMaps<TAction, TGoal>> onInitGameData)
        {
            _onInitGameData = onInitGameData;
            DebugMsgBase.Instance = InitDebugMsg();
            AgentState = InitAgentState();
            ActionManager = InitActionManager();
            GoalManager = InitGoalManager();
            Maps = InitMaps();
            _triggerManager = InitTriggerManager();
            Performer = new Performer<TAction, TGoal>(this);
            AgentState.AddStateChangeListener(UpdateData);
        }

        protected abstract IState InitAgentState();
        protected abstract IMaps<TAction, TGoal> InitMaps();
        protected abstract IActionManager<TAction> InitActionManager();
        protected abstract IGoalManager<TGoal> InitGoalManager();
        protected abstract ITriggerManager InitTriggerManager();
        protected abstract DebugMsgBase InitDebugMsg();

        public void UpdateData()
        {
            if (IsAgentOver)
                return;

            if (ActionManager != null)
                ActionManager.UpdateData();

            if (GoalManager != null)
                GoalManager.UpdateData();

            Performer.UpdateData();
        }

        public virtual void Update()
        {
            if (IsAgentOver)
                return;

            if (_triggerManager != null)
                _triggerManager.FrameFun();

            if (ActionManager != null)
                ActionManager.Update();
        }
    }
}