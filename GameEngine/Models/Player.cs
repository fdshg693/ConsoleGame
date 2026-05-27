using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Manager;

namespace GameEngine.Models
{
    /// <summary>
    /// プレイヤーを表すクラス
    /// Manager パターンを使用して責任を分離
    /// </summary>
    public class Player : IPlayer
    {
        // 基本情報
        public string Name { get; }

        // 各種マネージャー
        private readonly HealthManager _health;
        private readonly InventoryManager _inventory;
        private readonly ExperienceManager _experience;
        private readonly CombatManager _combat;
        private readonly RewardManager _reward;

        // 基礎ステータス
        private int BaseAP { get; set; } = GameConstants.PlayerBaseAP;
        
        // 攻撃戦略名を取得するためのプロパティ
        private string CurrentAttackStrategyName => _combat.GetCurrentStrategyName();

        // ステータスプロパティ
        public int HP => _health.CurrentHP;
        public int MaxHP => _health.MaxHP;
        public int DP => _health.TotalDP;
        public bool IsAlive => _health.IsAlive;
        public int AP => BaseAP + _inventory.Weapon.AP;
        public int Level => _experience.Level;
        public int TotalExperience => _experience.TotalExperience;
        public string EquippedWeaponName => _inventory.Weapon.Name;

        public Player(
            string name,
            int initialHP,
            IAttackStrategy attackStrategy,
            ExperienceManager experienceManager,
            InventoryManager inventoryManager)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Player name cannot be null or empty", nameof(name));

            Name = name;
            _experience = experienceManager ?? throw new ArgumentNullException(nameof(experienceManager));
            _inventory = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _health = new HealthManager(
                baseHP: initialHP, 
                baseDP: GameConstants.PlayerBaseDP, 
                equipProvider: _inventory);

            // 戦闘マネージャーの初期化
            _combat = new CombatManager(
                attackStrategy, 
                () => AP,
                Name);

            // 報酬マネージャーの初期化
            _reward = new RewardManager(
                _inventory,
                _experience,
                _health,
                amount => BaseAP += amount);
        }

        #region Equipment Management

        public void EquipWeapon(IWeapon weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));

            _inventory.EquipWeapon(weapon);
        }

        #endregion

        #region Combat

        public void Attack(ICharacter target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            _combat.ExecuteAttack(target);
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Damage amount cannot be negative", nameof(amount));

            int actualDamage = _health.TakeDamage(amount);
            GameMessageBus.Publish($"{Name} takes {actualDamage} damage! Remaining HP: {HP}", MessageType.Combat);
        }

        public void ChangeAttackStrategy(string strategyName)
        {
            _combat.ChangeAttackStrategy(strategyName);
        }

        #endregion

        #region Recovery

        public void Heal(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Heal amount must be positive", nameof(amount));

            _health.Heal(amount);
            GameMessageBus.Publish($"{Name} heals {amount} HP", MessageType.Success);
        }

        public void UsePotion(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Potion amount must be positive", nameof(amount));

            _inventory.UsePotion(amount);
            Heal(GameConstants.PotionHealAmount * amount);
        }

        #endregion

        #region Rewards

        public void DefeatEnemy(IEnemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            _reward.ProcessEnemyDefeat(enemy);
        }

        public void GainGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Gold amount cannot be negative", nameof(amount));

            _inventory.GainGold(amount);
        }

        #endregion

        #region Inventory

        public void BuyPotion(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Potion amount must be positive", nameof(amount));

            _inventory.BuyPotion(amount);
        }

        public int ReturnTotalPotions() => _inventory.ReturnTotalPotions();
        public int ReturnTotalGold() => _inventory.ReturnTotalGold();

        #endregion

        #region Display

        public void ShowInfo()
        {
            GameMessageBus.Publish("-------------------------------------------------------------------", MessageType.System);
            GameMessageBus.Publish($"Name: {Name}  HP: {HP}/{MaxHP}  AP: {AP}  DP: {DP}", MessageType.Info);
            _inventory.ShowInfo();
            _experience.ShowInfo();
            GameMessageBus.Publish("-------------------------------------------------------------------", MessageType.System);
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// プレイヤーの現在の状態からPlayerSaveDataを作成する
        /// </summary>
        public PlayerSaveData GetSaveData(string saveSlotName = "auto_save")
        {
            return new PlayerSaveData
            {
                PlayerName = Name,
                CurrentHP = HP,
                MaxHP = MaxHP,
                BaseAP = BaseAP,
                BaseDP = _health.BaseDP,
                TotalGold = _inventory.ReturnTotalGold(),
                TotalPotions = _inventory.ReturnTotalPotions(),
                Level = _experience.Level,
                TotalExperience = _experience.TotalExperience,
                EquippedWeapon = new WeaponData
                {
                    Name = _inventory.Weapon.Name,
                    HP = _inventory.Weapon.HP,
                    AP = _inventory.Weapon.AP,
                    DP = _inventory.Weapon.DP
                },
                AttackStrategy = _combat.GetCurrentStrategyName(),
                SavedAt = DateTime.UtcNow,
                SaveSlotName = saveSlotName
            };
        }

        #endregion
    }
}
