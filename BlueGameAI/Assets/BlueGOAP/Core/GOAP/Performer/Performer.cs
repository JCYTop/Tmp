namespace BlueGOAP
{
    public class Performer<TAction, TGoal> : IPerformer
    {
        private IPlanHandler<TAction> planHandler;
        private IPlanner<TAction, TGoal> planner;
        private IGoalManager<TGoal> goalManager;
        private IActionManager<TAction> actionManager;

        public Performer(IAgent<TAction, TGoal> agent)
        {
            planHandler = new PlanHandler<TAction>();
            planHandler.AddCompleteCallBack(() =>
            {
                //计划完成
                DebugMsg.Log("计划完成");
                actionManager.IsPerformAction = false;
            });
            planner = new Planner<TAction, TGoal>(agent);
            goalManager = agent.GoalManager;
            actionManager = agent.ActionManager;
            actionManager.AddActionCompleteListener((actionLabel) =>
            {
                //计划完成了当前动作
                DebugMsg.Log("下一步");
                if (planHandler.GetCurrentHandler().Label.ToString() == actionLabel.ToString())
                    planHandler.NextAction();
            });
        }

        public void UpdateData()
        {
            //检测是否需要重新制定计划
            //当前计划是否完成
            if (planHandler.IsComplete)
            {
                DebugMsg.Log("制定新计划");
                BuildPlanAndStart();
            }

            //制定计划并开始计划
            void BuildPlanAndStart()
            {
                if (goalManager.CurrentGoal != null)
                    DebugMsg.Log("----------------新的目标：" + goalManager.CurrentGoal.Label.ToString());
                //若目标完成则重新寻找目标
                var plan = planner.BuildPlan(goalManager.CurrentGoal);
                if (plan != null && plan.Count > 0)
                {
                    planHandler.Init(actionManager, plan);
                    planHandler.NextAction();
                    actionManager.IsPerformAction = true;
                }
            }
        }

        public void Interruptible()
        {
            DebugMsg.Log("打断计划");
            planHandler.Interruptible();
        }
    }
}