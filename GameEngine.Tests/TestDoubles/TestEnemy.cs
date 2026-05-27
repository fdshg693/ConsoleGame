using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Tests.TestDoubles
{
    /// <summary>
    /// 戦闘フローを決定的に検証するための <see cref="IEnemy"/> 実装。
    /// HP と与ダメージを固定で制御できる（弱い敵=即死で勝利、強い敵=高火力で敗北、を再現可能）。
    /// </summary>
    public sealed class TestEnemy : IEnemy
    {
        private readonly int _attackDamage;

        public TestEnemy(string name, int hp, int attackDamage, IAttackStrategy strategy, int yieldExperience = 10, int yieldGold = 10)
        {
            Name = name;
            MaxHP = hp;
            HP = hp;
            _attackDamage = attackDamage;
            AttackStrategy = strategy;
            YieldExperience = yieldExperience;
            YieldGold = yieldGold;
        }

        public string Name { get; }
        public int HP { get; private set; }
        public int MaxHP { get; }
        public bool IsAlive => HP > 0;
        public int YieldExperience { get; }
        public int YieldGold { get; }
        public IAttackStrategy AttackStrategy { get; }

        public void Attack(ICharacter character) => character.TakeDamage(_attackDamage);

        public void TakeDamage(int amount) => HP = Math.Max(0, HP - amount);

        public void Heal(int amount) => HP = Math.Min(MaxHP, HP + amount);

        public void ChangeAttackStrategy(string attackStrategyName) { }
    }
}
