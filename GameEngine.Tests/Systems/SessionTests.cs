using GameEngine.Configuration;
using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Models;
using GameEngine.Systems;
using GameEngine.Tests.TestDoubles;
using Xunit;

namespace GameEngine.Tests.Systems
{
    /// <summary>
    /// フェーズ3のセッション層の回帰テスト。
    /// 戦闘途中を含む進行状態を <see cref="GameSystem.CaptureSession"/> でスナップショット化し、
    /// <see cref="ISessionRepository"/> 経由で往復保存・復元できること、
    /// および <see cref="IPlayerFactory.Restore"/> / <see cref="IGameRecord.Restore"/> で
    /// プレイヤー・勝敗記録を再構築できることを担保する。
    /// </summary>
    public class SessionTests
    {
        private static IAttackStrategy DefaultStrategy => AttackStrategy.GetAttackStrategy(AttackStrategyNames.Default);
        private static AttackAction DefaultAttack => new AttackAction(AttackStrategyNames.Default);

        /// <summary>最初の抽選が戦闘になるシードを探す（StepFlowTests と同じ判定）。</summary>
        private static int FindBattleSeed(GameConfig config)
        {
            for (int s = 0; s < 100000; s++)
            {
                int roll = new Random(s).Next(0, config.Events.TotalWeight);
                if (roll >= config.Events.ShopEventWeight)
                {
                    return s;
                }
            }
            throw new InvalidOperationException("No battle seed found");
        }

        private static GameSystem BuildBattleGame(GameConfig config, IGameMessageBus bus, IGameRecord record, Func<IEnemy> enemy, out IPlayer player)
        {
            player = new PlayerFactory(config, bus).CreateNew("Hero");
            var eventManager = new EventManager(player, config, new FakeEnemyFactory(enemy), record, new Random(FindBattleSeed(config)));
            return new GameSystem(player, new ScriptedGameInput(), eventManager, new NullRenderer(), bus, playerRepository: null);
        }

        [Fact]
        public void CaptureSession_MidBattle_SnapshotsEnemyTurnAndPhase()
        {
            var config = GameConfigLoader.Instance;
            IGameMessageBus bus = new GameMessageBus();
            IGameRecord record = new GameRecord();
            // 高HP・無害（攻撃力0）の敵で戦闘を継続させ、途中状態を捕捉する
            using var game = BuildBattleGame(config, bus, record, () => new TestEnemy("Tank", 1000, 0, DefaultStrategy), out _);

            game.Start();
            game.Step(PlayerInput.ForAttack(DefaultAttack)); // 1ターン進める（敵は生存・戦闘継続）

            var snapshot = game.CaptureSession("session-1");

            Assert.Equal("session-1", snapshot.SessionId);
            Assert.Equal("Hero", snapshot.PlayerName);
            Assert.Equal("Battle", snapshot.CurrentStateName);
            Assert.Equal(GamePhase.Battle, snapshot.Phase);
            Assert.Equal(ExpectedInput.Attack, snapshot.ExpectedInput);

            // 戦闘途中の敵状態（現在HP）・ターン数が捕捉される
            Assert.NotNull(snapshot.Enemy);
            Assert.Equal("Tank", snapshot.Enemy!.Name);
            Assert.True(snapshot.Enemy.HP < 1000);
            Assert.NotNull(snapshot.Battle);
            Assert.Equal(1, snapshot.Battle!.TurnNumber);

            // プレイヤースナップショットも含まれる
            Assert.Equal("Hero", snapshot.Player.PlayerName);
        }

        [Fact]
        public async Task Session_RoundTripsThroughRepository()
        {
            var config = GameConfigLoader.Instance;
            IGameMessageBus bus = new GameMessageBus();
            IGameRecord record = new GameRecord();
            using var game = BuildBattleGame(config, bus, record, () => new TestEnemy("Tank", 1000, 0, DefaultStrategy), out _);

            game.Start();
            game.Step(PlayerInput.ForAttack(DefaultAttack));

            var snapshot = game.CaptureSession("session-2");

            ISessionRepository repo = new InMemorySessionRepository();
            await repo.SaveAsync(snapshot);
            var loaded = await repo.LoadAsync("session-2");

            Assert.NotNull(loaded);
            Assert.Equal(snapshot.CurrentStateName, loaded!.CurrentStateName);
            Assert.Equal(snapshot.Enemy!.HP, loaded.Enemy!.HP);
            Assert.Equal(snapshot.Battle!.TurnNumber, loaded.Battle!.TurnNumber);
            Assert.Equal(snapshot.Player.CurrentHP, loaded.Player.CurrentHP);
        }

        [Fact]
        public void RestoreFromSnapshot_RebuildsPlayerAndRecord()
        {
            var config = GameConfigLoader.Instance;
            IGameMessageBus bus = new GameMessageBus();
            IGameRecord record = new GameRecord();
            record.RecordWin();
            record.RecordWin();
            record.RecordLoss();

            using var game = BuildBattleGame(config, bus, record, () => new TestEnemy("Tank", 1000, 0, DefaultStrategy), out var original);
            game.Start();
            game.Step(PlayerInput.ForAttack(DefaultAttack));

            var snapshot = game.CaptureSession("session-3");

            // 別の合成（新しいインスタンス）へ復元する
            IGameMessageBus restoreBus = new GameMessageBus();
            var restoredPlayer = new PlayerFactory(config, restoreBus).Restore(snapshot.Player);
            IGameRecord restoredRecord = new GameRecord();
            restoredRecord.Restore(snapshot.TotalWins, snapshot.TotalLosses);

            // プレイヤーステータスが一致
            Assert.Equal(original.Name, restoredPlayer.Name);
            Assert.Equal(original.HP, restoredPlayer.HP);
            Assert.Equal(original.MaxHP, restoredPlayer.MaxHP);
            Assert.Equal(original.AP, restoredPlayer.AP);
            Assert.Equal(original.DP, restoredPlayer.DP);
            Assert.Equal(original.Level, restoredPlayer.Level);
            Assert.Equal(original.ReturnTotalGold(), restoredPlayer.ReturnTotalGold());
            Assert.Equal(original.ReturnTotalPotions(), restoredPlayer.ReturnTotalPotions());

            // 勝敗記録が一致
            Assert.Equal(2, restoredRecord.TotalWins);
            Assert.Equal(1, restoredRecord.TotalLosses);
        }
    }
}
