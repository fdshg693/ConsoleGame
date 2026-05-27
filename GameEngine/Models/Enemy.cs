using GameEngine.Interfaces;

namespace GameEngine.Models
{
    public class Enemy : IEnemy
    {
        public string Name { get; private set; }
        public int HP { get; private set; }

        public int BaseHP { get; private set; }
        public int BaseAP { get; private set; }
        public int BaseDP { get; private set; }
        public IAttackStrategy AttackStrategy { get; private set; }
        public int MaxHP => BaseHP;

        public bool IsAlive => HP > 0;
        public int YieldExperience { get; private set; } = 0;
        public int YieldGold { get; private set; } = 0;

        private readonly IGameMessageBus _bus;

        public Enemy(string name, int hp, IAttackStrategy attackStrategy, int experience, int aP, int dP, int yieldGold, IGameMessageBus bus)
        {
            Name = name;
            HP = hp;
            BaseHP = hp;
            AttackStrategy = attackStrategy;
            YieldExperience = experience;
            YieldGold = yieldGold;
            BaseAP = aP;
            BaseDP = dP;
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }
        public void Attack(ICharacter character)
        {
            character.TakeDamage(AttackStrategy.ExecuteAttack() + BaseAP);
        }
        public void TakeDamage(int amount)
        {
            int damage = Math.Max(amount - BaseDP, 0);
            HP -= damage;
            if (HP < 0) HP = 0;
            _bus.Publish($"{Name} takes {damage} damage! Remaining HP: {HP}", MessageType.Combat);
        }
        public void Heal(int amount)
        {
            HP += amount;
            if (HP > BaseHP) HP = BaseHP;
        }
        public void ChangeAttackStrategy(string AttackStrategyName)
        {
            AttackStrategy = global::GameEngine.Models.AttackStrategy.GetAttackStrategy(AttackStrategyName);
        }
    }
}