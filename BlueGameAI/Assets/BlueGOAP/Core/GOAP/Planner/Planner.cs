using System.Collections.Generic;
using System.Linq;

namespace BlueGOAP
{
    public class Planner<TAction, TGoal> : IPlanner<TAction, TGoal>
    {
        private IAgent<TAction, TGoal> agent;

        public Planner(IAgent<TAction, TGoal> agent)
        {
            this.agent = agent;
        }

        public Queue<IActionHandler<TAction>> BuildPlan(IGoal<TGoal> goal)
        {
            DebugMsg.Log("制定计划");
            DebugMsg.Log("---------------当前代理状态------------");
            DebugMsg.Log(agent.AgentState.ToString());
            DebugMsg.Log("---------------------------");
            var plan = new Queue<IActionHandler<TAction>>();
            if (goal == null)
                return plan;
            var currentNode = Plan(goal);
            if (currentNode == null)
            {
                plan.Enqueue(agent.ActionManager.GetHandler(agent.ActionManager.GetDefaultActionLabel()));
                DebugMsg.LogError("当前节点为空，设置当前动作为默认动作");
                return plan;
            }

            while (currentNode.ID != TreeNode<TAction>.DEFAULT_ID)
            {
                plan.Enqueue(currentNode.ActionHandler);
                currentNode = currentNode.ParentNode;
            }

            DebugMsg.Log("---------------最终生成计划------------");
            foreach (var handler in plan)
            {
                DebugMsg.Log("计划项：" + handler.Label);
            }

            DebugMsg.Log("---------------当前代理状态------------");
            DebugMsg.Log(agent.AgentState.ToString());
            DebugMsg.Log("---------------------------");
            DebugMsg.Log("计划结束");
            return plan;
        }

        private TreeNode<TAction> Plan(IGoal<TGoal> goal)
        {
            var tree = new Tree<TAction>();
            //初始化树的头节点
            var topNode = CreateTopNode(tree, goal);
            //获取最优节点
            TreeNode<TAction> cheapestNode = null;
            var currentNode = topNode;
            while (!IsEnd(currentNode))
            {
                //获取所有的子行为
                var handlers = GetSubHandlers(currentNode);
                foreach (IActionHandler<TAction> handler in handlers)
                {
                    var subNode = tree.CreateNode(handler);
//                    SetNodeState(currentNode, subNode);
                    subNode.Cost = GetCost(subNode);
                    subNode.ParentNode = currentNode;
                    cheapestNode = GetCheapestNode(subNode, cheapestNode);
                }

                currentNode = cheapestNode;
                cheapestNode = null;
            }

            return currentNode;
        }

        private TreeNode<TAction> CreateTopNode(Tree<TAction> tree, IGoal<TGoal> goal)
        {
            TreeNode<TAction> topNode = tree.CreateTopNode();
            topNode.GoalState.Set(goal.GetEffects());
            topNode.Cost = GetCost(topNode);
            SetNodeCurrentState(topNode);
            return topNode;
        }

        private TreeNode<TAction> GetCheapestNode(TreeNode<TAction> left, TreeNode<TAction> right)
        {
            if (left == null || left.ActionHandler == null)
                return right;
            if (right == null || right.ActionHandler == null)
                return left;
            if (left.Cost > right.Cost)
            {
                return right;
            }
            else if (left.Cost < right.Cost)
            {
                return left;
            }
            else
            {
                if (left.ActionHandler.Action.Priority > right.ActionHandler.Action.Priority)
                {
                    return left;
                }
                else
                {
                    return right;
                }
            }
        }

        private bool IsEnd(TreeNode<TAction> currentNode)
        {
            if (currentNode == null)
                return true;
            if (GetStateDifferecnceNum(currentNode) == 0)
                return true;
            return false;
        }

        private void SetNodeState(TreeNode<TAction> currentNode, TreeNode<TAction> subNode)
        {
            if (subNode.ID > TreeNode<TAction>.DEFAULT_ID)
            {
                var subAction = subNode.ActionHandler.Action;
                //首先复制当前节点的状态
                subNode.CopyState(currentNode);
                //查找action的effects，和goal中也存在
                var data = subNode.GoalState.GetSameData(subAction.Effects);
                //那么就把这个状态添加到节点的当前状态中
                subNode.CurrentState.Set(data);
                //把action的先决条件存在goalState中不存在的键值添加进去
                foreach (var key in subAction.Preconditions.GetKeys())
                {
                    if (!subNode.GoalState.ContainKey(key))
                    {
                        subNode.GoalState.Set(key, subAction.Preconditions.Get(key));
                    }
                }

                SetNodeCurrentState(subNode);
            }
        }

        private void SetNodeCurrentState(TreeNode<TAction> node)
        {
            //把GoalState中有且CurrentState没有的添加到CurrentState中
            //数据从agent的当前状态中获取
            var keys = node.CurrentState.GetNotExistKeys(node.GoalState);
            foreach (string key in keys)
            {
                node.CurrentState.Set(key, agent.AgentState.Get(key));
            }
        }

        private int GetCost(TreeNode<TAction> node)
        {
            int configCost = 0;
            if (node.ActionHandler != null)
                configCost = node.ActionHandler.Action.Cost;
            return node.Cost + configCost + GetStateDifferecnceNum(node);
        }

        private int GetStateDifferecnceNum(TreeNode<TAction> node)
        {
            return node.CurrentState.GetValueDifferences(node.GoalState).Count;
        }

        /// <summary>
        /// 获取所有的子节点行为
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<IActionHandler<TAction>> GetSubHandlers(TreeNode<TAction> node)
        {
            var handlers = new List<IActionHandler<TAction>>();
            if (node == null)
                return handlers;
            //获取状态差异
            var currkeys = node.CurrentState.GetValueDifferences(node.GoalState);
            var map = agent.ActionManager.EffectsAndActionMap;
            foreach (string key in currkeys)
            {
                if (map.ContainsKey(key))
                {
                    foreach (var handler in map[key])
                    {
                        //筛选能够执行的动作
                        if (!handlers.Contains(handler) && handler.Action.Effects.Get(key) == node.GoalState.Get(key))
                        {
                            handlers.Add(handler);
                        }
                    }
                }
                else
                {
                    DebugMsg.LogError("当前没有动作能够实现从当前状态切换到目标状态，无法实现的键值为：" + key);
                }
            }

            //进行优先级排序
            handlers = handlers.OrderByDescending(u => u.Action.Priority).ToList();
            return handlers;
        }
    }
}