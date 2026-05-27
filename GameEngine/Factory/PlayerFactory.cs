using GameEngine.Configuration;
using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Models;

namespace GameEngine.Factory
{
    /// <summary>
    /// <see cref="IPlayerFactory"/> の実装。<see cref="GameConfig"/> と <see cref="IGameMessageBus"/> を
    /// 注入され、新規プレイヤーの生成とセーブデータからの復元を一元化する。
    /// </summary>
    public class PlayerFactory : IPlayerFactory
    {
        private readonly GameConfig _config;
        private readonly IGameMessageBus _bus;

        public PlayerFactory(GameConfig config, IGameMessageBus bus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public IPlayer CreateNew(string name)
        {
            var experience = new ExperienceManager(_config.LevelUp.ExperienceRequired, _bus);
            var inventory = new InventoryManager(
                _config.Player.InitialGold,
                _config.Player.InitialPotions,
                _config.Items.Potion.Price,
                _bus);

            return new Player(
                name,
                _config,
                AttackStrategy.GetAttackStrategy(AttackStrategyNames.Default),
                experience,
                inventory,
                _bus);
        }

        public IPlayer Restore(PlayerSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // レベル・経験値を復元（マネージャー側の復元コンストラクタ）
            var experience = new ExperienceManager(
                _config.LevelUp.ExperienceRequired,
                _bus,
                initialLevel: data.Level,
                initialExperience: data.TotalExperience);

            // ゴールド・ポーションを復元し、装備武器を再装着
            var inventory = new InventoryManager(
                data.TotalGold,
                data.TotalPotions,
                _config.Items.Potion.Price,
                _bus);

            var weapon = new Weapon(
                data.EquippedWeapon.HP,
                data.EquippedWeapon.AP,
                data.EquippedWeapon.DP,
                data.EquippedWeapon.Name);
            inventory.EquipWeapon(weapon);

            // 基礎ステータス（武器ボーナスを除いた値）で HealthManager を再構築する。
            // 保存 MaxHP は武器込みのため、基礎 HP = MaxHP − 装備武器 HP。
            var restoreState = new PlayerRestoreState
            {
                BaseAP = data.BaseAP,
                BaseHP = data.MaxHP - weapon.HP,
                BaseDP = data.BaseDP,
                CurrentHP = data.CurrentHP
            };

            var strategy = AttackStrategy.GetAttackStrategy(data.AttackStrategy);

            return new Player(
                data.PlayerName,
                _config,
                strategy,
                experience,
                inventory,
                _bus,
                restoreState);
        }
    }
}
