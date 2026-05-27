using GameEngine.Interfaces;

namespace GameEngine.Manager
{
    public class HealthManager
    {
        private readonly IEquipmentStatsProvider _equipProvider;

        // ベース HP／DP を保持
        public int BaseHP { get; private set; }
        public int BaseDP { get; private set; }

        // 現在 HP（外部から参照・操作せず、メソッド経由で変更）
        public int CurrentHP { get; private set; }

        // 装備込みの最大 HP／防御力
        public int MaxHP => BaseHP + _equipProvider.Weapon.HP;
        public int TotalDP => BaseDP + _equipProvider.Weapon.DP;

        public bool IsAlive => CurrentHP > 0;

        public HealthManager(int baseHP, int baseDP, IEquipmentStatsProvider equipProvider)
        {
            BaseHP = baseHP;
            BaseDP = baseDP;
            _equipProvider = equipProvider;

            // 装備変更時に最大HPが減ったら CurrentHP をクリップ
            _equipProvider.EquipmentChanged += () =>
            {
                if (CurrentHP > MaxHP)
                    CurrentHP = MaxHP;
            };

            CurrentHP = MaxHP;
        }

        // ダメージ処理
        public int TakeDamage(int rawDamage)
        {
            var damage = Math.Max(rawDamage - TotalDP, 0);
            CurrentHP = Math.Max(CurrentHP - damage, 0);
            return damage;
        }

        // 回復処理
        public void Heal(int amount)
        {
            CurrentHP = Math.Min(CurrentHP + amount, MaxHP);
        }

        // レベルアップ時にベース値を増やし、同時に現在HPにも反映
        public void LevelUp(int hpIncrease, int dpIncrease)
        {
            BaseHP += hpIncrease;
            BaseDP += dpIncrease;
            CurrentHP += hpIncrease;
        }
    }

}
