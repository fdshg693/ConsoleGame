using GameEngine.Configuration;

namespace GameEngine.Constants
{
    /// <summary>
    /// Game-wide constant values for balancing and configuration
    /// DEPRECATED: Use GameConfigLoader.Instance for runtime configuration instead.
    /// This class is kept for backward compatibility and contains default fallback values.
    /// </summary>
    public static class GameConstants
    {
        private static GameConfig Config => GameConfigLoader.Instance;

        // Player initial stats
        public static int PlayerInitialHP => Config.Player.InitialHP;
        public static int PlayerBaseDP => Config.Player.BaseDP;
        public static int PlayerBaseAP => Config.Player.BaseAP;

        // Level up bonuses
        public static int LevelUpHPIncrease => Config.LevelUp.HPIncrease;
        public static int LevelUpDPIncrease => Config.LevelUp.DPIncrease;
        public static int LevelUpAPIncrease => Config.LevelUp.APIncrease;

        // Experience system
        public static int ExperienceRequiredForLevelUp => Config.LevelUp.ExperienceRequired;

        // Item prices and effects
        public static int PotionPrice => Config.Items.Potion.Price;
        public static int PotionHealAmount => Config.Items.Potion.HealAmount;

        // Shop rewards
        public static int ShopGoldRewardMin => Config.Shop.GoldRewardMin;
        public static int ShopGoldRewardMax => Config.Shop.GoldRewardMax;

        // Event probabilities
        public static int ShopEventProbability => Config.Events.TotalWeight / Config.Events.ShopEventWeight;

        // Enemy gold calculation
        public static int EnemyGoldBaseMultiplier => Config.Enemy.GoldBaseMultiplier;
        public static int EnemyGoldRandomMin => Config.Enemy.GoldRandomMin;
        public static int EnemyGoldRandomMax => Config.Enemy.GoldRandomMax;

        // Initial inventory
        public static int InitialGold => Config.Player.InitialGold;
        public static int InitialPotions => Config.Player.InitialPotions;

        // Weapon stats (backward compatibility)
        public static class Weapons
        {
            public static int SwordHP => GetWeaponStat("Sword", w => w.HP, 100);
            public static int SwordAP => GetWeaponStat("Sword", w => w.AP, 20);
            public static int SwordDP => GetWeaponStat("Sword", w => w.DP, 5);

            public static int AxeHP => GetWeaponStat("Axe", w => w.HP, 80);
            public static int AxeAP => GetWeaponStat("Axe", w => w.AP, 30);
            public static int AxeDP => GetWeaponStat("Axe", w => w.DP, 3);

            public static int BowHP => GetWeaponStat("Bow", w => w.HP, 70);
            public static int BowAP => GetWeaponStat("Bow", w => w.AP, 35);
            public static int BowDP => GetWeaponStat("Bow", w => w.DP, 2);

            private static int GetWeaponStat(string weaponName, Func<WeaponStats, int> selector, int defaultValue)
            {
                if (Config.Weapons.TryGetValue(weaponName, out var stats))
                    return selector(stats);
                return defaultValue;
            }
        }

        // Attack strategy damage ranges
        public static class AttackDamage
        {
            public const int DefaultMin = 8;
            public const int DefaultMax = 10;

            public const int MeleeMin = 10;
            public const int MeleeMax = 16;

            public const int MagicMin = 0;
            public const int MagicMax = 25;
        }
    }
}
