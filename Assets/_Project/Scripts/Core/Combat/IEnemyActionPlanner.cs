namespace SlotRogue.Core.Combat
{
    public interface IEnemyActionPlanner
    {
        EnemyActionPlan PlanNext(EnemyActionContext context);
    }
}
