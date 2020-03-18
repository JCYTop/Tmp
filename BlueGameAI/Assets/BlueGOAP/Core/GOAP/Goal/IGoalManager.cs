
namespace BlueGOAP
{
    public interface IGoalManager<TGoal>
    {
        IGoal<TGoal> CurrentGoal { get; }
        IGoal<TGoal> GetGoal(TGoal goalLabel);
        void AddGoal(TGoal goalLabel);
        void RemoveGoal(TGoal goalLabel);
        IGoal<TGoal> FindGoal();
        void UpdateData();
    }
}
