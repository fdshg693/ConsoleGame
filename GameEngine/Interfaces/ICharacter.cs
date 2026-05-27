namespace GameEngine.Interfaces
{
    public interface ICharacter
    {
        string Name { get; }
        int HP { get; }
        bool IsAlive { get; }
        void Attack(ICharacter character);
        void TakeDamage(int amount);
        void Heal(int amount);
        void ChangeAttackStrategy(string AttackStrategyName);
    }
}