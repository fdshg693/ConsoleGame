using GameEngine.Constants;
using GameEngine.Interfaces;

namespace GameEngine.Models
{
    public static class AttackStrategy
    {
        public static IAttackStrategy GetAttackStrategy(string attackType)
        {
            return attackType switch
            {
                AttackStrategyNames.Melee => new MeleeAttackStrategy(),
                AttackStrategyNames.Magic => new MagicAttackStrategy(),
                _ => new DefaultAttackStrategy()
            };
        }
    }

    public class DefaultAttackStrategy : IAttackStrategy
    {
        public int ExecuteAttack() => Random.Shared.Next(GameConstants.AttackDamage.DefaultMin, GameConstants.AttackDamage.DefaultMax);
        public string GetAttackStrategyName() => AttackStrategyNames.Default;
    }

    public class MeleeAttackStrategy : IAttackStrategy
    {
        public int ExecuteAttack() => Random.Shared.Next(GameConstants.AttackDamage.MeleeMin, GameConstants.AttackDamage.MeleeMax);
        public string GetAttackStrategyName() => AttackStrategyNames.Melee;
    }

    public class MagicAttackStrategy : IAttackStrategy
    {
        public int ExecuteAttack() => Random.Shared.Next(GameConstants.AttackDamage.MagicMin, GameConstants.AttackDamage.MagicMax);
        public string GetAttackStrategyName() => AttackStrategyNames.Magic;
    }
}
