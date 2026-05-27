using GameEngine.Configuration;
using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Models;
using GameEngine.Systems.BattleSystem;
using GameEngine.Tests.TestDoubles;
using Xunit;

namespace GameEngine.Tests.Systems
{
    /// <summary>
    /// フェーズ2でステップ駆動化した <see cref="BattleManager"/> の回帰テスト。
    /// 旧 <c>ExecuteBattle</c> の while ループを <c>StartBattle</c> + <c>SubmitPlayerTurn</c> に
    /// 分解しても、勝敗判定・ダメージ・メッセージが保たれることを担保する。
    /// </summary>
    public class BattleManagerTests
    {
        private static IAttackStrategy DefaultStrategy => AttackStrategy.GetAttackStrategy(AttackStrategyNames.Default);

        private static IPlayer CreatePlayer()
        {
            var config = GameConfigLoader.Instance;
            IGameMessageBus bus = new GameMessageBus();
            var exp = new ExperienceManager(config.LevelUp.ExperienceRequired, bus);
            var inv = new InventoryManager(config.Player.InitialGold, config.Player.InitialPotions, config.Items.Potion.Price, bus);
            return new Player("Hero", config, DefaultStrategy, exp, inv, bus);
        }

        [Fact]
        public void StartBattle_ReturnsInProgressWithEnemy()
        {
            var player = CreatePlayer();
            var bm = new BattleManager(player, new FakeEnemyFactory(() => new TestEnemy("Slime", 30, 0, DefaultStrategy)));

            var result = bm.StartBattle();

            Assert.Equal(BattleOutcome.InProgress, result.Outcome);
            Assert.False(result.IsOver);
            Assert.NotNull(result.Enemy);
            Assert.Equal("Slime", result.Enemy!.Name);
            Assert.Equal(30, result.Enemy.HP);
            Assert.NotNull(bm.CurrentEnemy);
            Assert.Contains(result.Messages, m => m.Text.Contains("Slime"));
        }

        [Fact]
        public void SubmitPlayerTurn_DealsDamage_StaysInProgress()
        {
            var player = CreatePlayer();
            var bm = new BattleManager(player, new FakeEnemyFactory(() => new TestEnemy("Slime", 30, 0, DefaultStrategy)));
            bm.StartBattle();

            var result = bm.SubmitPlayerTurn(new AttackAction(AttackStrategyNames.Default));

            Assert.Equal(BattleOutcome.InProgress, result.Outcome);
            Assert.NotNull(result.Enemy);
            Assert.True(result.Enemy!.HP < 30);
            Assert.True(result.Battle!.LastDamageDealt > 0);
            Assert.Equal(1, result.Battle.TurnNumber);
        }

        [Fact]
        public void SubmitPlayerTurn_KillsWeakEnemy_Victory()
        {
            var player = CreatePlayer();
            var bm = new BattleManager(player, new FakeEnemyFactory(() => new TestEnemy("Weakling", 1, 0, DefaultStrategy)));
            bm.StartBattle();

            var result = bm.SubmitPlayerTurn(new AttackAction(AttackStrategyNames.Default));

            Assert.Equal(BattleOutcome.Victory, result.Outcome);
            Assert.True(result.IsOver);
            Assert.True(result.Battle!.BattleEnded);
            Assert.True(result.Battle.PlayerWon);
            Assert.Null(bm.CurrentEnemy);
            Assert.Contains(result.Messages, m => m.Text.Contains("defeated"));
        }

        [Fact]
        public void SubmitPlayerTurn_StrongEnemy_Defeat()
        {
            var player = CreatePlayer();
            var bm = new BattleManager(player, new FakeEnemyFactory(() => new TestEnemy("Boss", 100000, 999999, DefaultStrategy)));
            bm.StartBattle();

            var result = bm.SubmitPlayerTurn(new AttackAction(AttackStrategyNames.Default));

            Assert.Equal(BattleOutcome.Defeat, result.Outcome);
            Assert.True(result.IsOver);
            Assert.False(player.IsAlive);
            Assert.Null(bm.CurrentEnemy);
            Assert.Contains(result.Messages, m => m.Text.Contains("fallen"));
        }

        [Fact]
        public void SubmitPlayerTurn_WithoutActiveBattle_ReturnsError()
        {
            var player = CreatePlayer();
            var bm = new BattleManager(player, new FakeEnemyFactory(() => new TestEnemy("Slime", 30, 0, DefaultStrategy)));

            var result = bm.SubmitPlayerTurn(new AttackAction(AttackStrategyNames.Default));

            Assert.Equal(BattleOutcome.Error, result.Outcome);
            Assert.True(result.IsError);
        }
    }
}
