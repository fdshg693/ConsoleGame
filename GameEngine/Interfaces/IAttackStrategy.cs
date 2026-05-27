namespace GameEngine.Interfaces
{
    public interface IAttackStrategy
    {
        int ExecuteAttack();
        string GetAttackStrategyName();
    }
}