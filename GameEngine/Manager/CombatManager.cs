using GameEngine.Constants;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Manager
{
    /// <summary>
    /// プレイヤーの戦闘関連の機能を管理するクラス
    /// </summary>
    public class CombatManager
    {
        private IAttackStrategy _attackStrategy;
        private readonly Func<int> _getAP;
        private readonly string _playerName;

        public CombatManager(
            IAttackStrategy initialStrategy, 
            Func<int> getAP,
            string playerName)
        {
            _attackStrategy = initialStrategy ?? throw new ArgumentNullException(nameof(initialStrategy));
            _getAP = getAP ?? throw new ArgumentNullException(nameof(getAP));
            _playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        }

        /// <summary>
        /// 攻撃を実行する
        /// </summary>
        public void ExecuteAttack(ICharacter target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            int strategyDamage = _attackStrategy.ExecuteAttack();
            int totalDamage = strategyDamage + _getAP();
            
            target.TakeDamage(totalDamage);
        }

        /// <summary>
        /// 攻撃戦略を変更する
        /// </summary>
        public void ChangeAttackStrategy(string strategyName)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
                throw new ArgumentException("Strategy name cannot be null or empty", nameof(strategyName));

            _attackStrategy = AttackStrategy.GetAttackStrategy(strategyName);
        }

        /// <summary>
        /// 現在の攻撃戦略の名前を取得する
        /// </summary>
        public string GetCurrentStrategyName()
        {
            return _attackStrategy.GetAttackStrategyName();
        }
    }

    /// <summary>
    /// プレイヤーの報酬獲得機能を管理するクラス
    /// </summary>
    public class RewardManager
    {
        private readonly InventoryManager _inventory;
        private readonly ExperienceManager _experience;
        private readonly HealthManager _health;
        private readonly Action<int> _increaseBaseAP;

        public RewardManager(
            InventoryManager inventory,
            ExperienceManager experience,
            HealthManager health,
            Action<int> increaseBaseAP)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _experience = experience ?? throw new ArgumentNullException(nameof(experience));
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _increaseBaseAP = increaseBaseAP ?? throw new ArgumentNullException(nameof(increaseBaseAP));
        }

        /// <summary>
        /// 敵を倒した時の報酬処理
        /// </summary>
        public void ProcessEnemyDefeat(IEnemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            GameMessageBus.Publish($"You defeated {enemy.Name}!", MessageType.Combat);

            // ゴールド獲得
            _inventory.GainGold(enemy.YieldGold);

            // 経験値獲得とレベルアップチェック
            int levelsGained = _experience.GainExperience(enemy.YieldExperience);
            
            if (levelsGained > 0)
            {
                ProcessLevelUp(levelsGained);
            }
        }

        /// <summary>
        /// レベルアップ処理
        /// </summary>
        private void ProcessLevelUp(int levels)
        {
            for (int i = 0; i < levels; i++)
            {
                _health.LevelUp(
                    hpIncrease: GameConstants.LevelUpHPIncrease, 
                    dpIncrease: GameConstants.LevelUpDPIncrease);
                
                _increaseBaseAP(GameConstants.LevelUpAPIncrease);
            }

            GameMessageBus.Publish($"Leveled up {levels} time(s)!", MessageType.Experience);
        }
    }
}
