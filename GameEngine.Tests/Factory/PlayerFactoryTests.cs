using GameEngine.Configuration;
using GameEngine.DTOs;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Models;
using Xunit;

namespace GameEngine.Tests.Factory
{
    /// <summary>
    /// フェーズ3で新設した <see cref="PlayerFactory"/>（<see cref="IPlayerFactory"/>）の検証。
    /// 新規生成（設定既定値）と、セーブデータ（<see cref="PlayerSaveData"/>）からの完全復元の両経路を担保する。
    /// </summary>
    public class PlayerFactoryTests
    {
        private static IPlayerFactory CreateFactory()
        {
            var config = GameConfigLoader.Instance;
            IGameMessageBus bus = new GameMessageBus();
            return new PlayerFactory(config, bus);
        }

        [Fact]
        public void CreateNew_UsesConfigDefaults()
        {
            var config = GameConfigLoader.Instance;
            var factory = CreateFactory();

            var player = factory.CreateNew("Hero");

            Assert.Equal("Hero", player.Name);
            Assert.Equal(1, player.Level);
            Assert.Equal(config.Player.InitialGold, player.ReturnTotalGold());
            Assert.Equal(config.Player.InitialPotions, player.ReturnTotalPotions());
            Assert.True(player.IsAlive);
        }

        [Fact]
        public void Restore_RebuildsStatsFromSaveData()
        {
            var factory = CreateFactory();
            var data = new PlayerSaveData
            {
                PlayerName = "Veteran",
                CurrentHP = 40,
                MaxHP = 100,
                BaseAP = 20,
                BaseDP = 8,
                TotalGold = 200,
                TotalPotions = 4,
                Level = 3,
                TotalExperience = 15,
                EquippedWeapon = new WeaponData { Name = "SWORD", HP = 10, AP = 5, DP = 3 },
                AttackStrategy = "Magic"
            };

            var player = factory.Restore(data);

            Assert.Equal("Veteran", player.Name);
            Assert.Equal(100, player.MaxHP);          // baseHP(90) + weapon HP(10)
            Assert.Equal(40, player.HP);              // 保存時の現在 HP
            Assert.Equal(25, player.AP);              // baseAP(20) + weapon AP(5)
            Assert.Equal(11, player.DP);              // baseDP(8) + weapon DP(3)
            Assert.Equal(3, player.Level);
            Assert.Equal(15, player.TotalExperience);
            Assert.Equal(200, player.ReturnTotalGold());
            Assert.Equal(4, player.ReturnTotalPotions());
            Assert.Equal("SWORD", player.EquippedWeaponName);
        }

        [Fact]
        public void Restore_RoundTripsThroughGetSaveData()
        {
            var factory = CreateFactory();
            var original = new PlayerSaveData
            {
                PlayerName = "RoundTrip",
                CurrentHP = 55,
                MaxHP = 120,
                BaseAP = 18,
                BaseDP = 6,
                TotalGold = 333,
                TotalPotions = 7,
                Level = 4,
                TotalExperience = 22,
                EquippedWeapon = new WeaponData { Name = "AXE", HP = 20, AP = 9, DP = 1 },
                AttackStrategy = "Melee"
            };

            var restored = factory.Restore(original).GetSaveData();

            Assert.Equal(original.PlayerName, restored.PlayerName);
            Assert.Equal(original.CurrentHP, restored.CurrentHP);
            Assert.Equal(original.MaxHP, restored.MaxHP);
            Assert.Equal(original.BaseAP, restored.BaseAP);
            Assert.Equal(original.BaseDP, restored.BaseDP);
            Assert.Equal(original.TotalGold, restored.TotalGold);
            Assert.Equal(original.TotalPotions, restored.TotalPotions);
            Assert.Equal(original.Level, restored.Level);
            Assert.Equal(original.TotalExperience, restored.TotalExperience);
            Assert.Equal(original.AttackStrategy, restored.AttackStrategy);
            Assert.Equal(original.EquippedWeapon.Name, restored.EquippedWeapon.Name);
            Assert.Equal(original.EquippedWeapon.HP, restored.EquippedWeapon.HP);
            Assert.Equal(original.EquippedWeapon.AP, restored.EquippedWeapon.AP);
            Assert.Equal(original.EquippedWeapon.DP, restored.EquippedWeapon.DP);
        }
    }
}
