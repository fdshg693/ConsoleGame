namespace GameEngine.Interfaces
{
    public interface IEnemy : ICharacter
    {
        int YieldExperience { get; }
        int YieldGold { get; }
        int MaxHP { get; }
        IAttackStrategy AttackStrategy { get; }
    }
}
