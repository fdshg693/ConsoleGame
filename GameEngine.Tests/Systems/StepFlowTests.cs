using GameEngine.Configuration;
using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Models;
using GameEngine.Systems;
using GameEngine.Tests.TestDoubles;
using Xunit;

namespace GameEngine.Tests.Systems
{
    /// <summary>
    /// フェーズ2のステップ駆動エンジン（<see cref="GameSystem"/> + 統一ステートマシン）の回帰テスト。
    /// 「1行動 → 1ステップ → 状態返却」で外部駆動でき、戦闘/ショップ/休憩/続行確認の遷移が
    /// 正しく進むことを担保する。決定性のため敵を固定（<see cref="FakeEnemyFactory"/>）し、
    /// エンカウント種別はシード探索で固定する。
    /// </summary>
    public class StepFlowTests
    {
        private static IAttackStrategy DefaultStrategy => AttackStrategy.GetAttackStrategy(AttackStrategyNames.Default);

        private static AttackAction DefaultAttack => new AttackAction(AttackStrategyNames.Default);

        private static IPlayer CreatePlayer(GameConfig config, IGameMessageBus bus)
        {
            var exp = new ExperienceManager(config.LevelUp.ExperienceRequired, bus);
            var inv = new InventoryManager(config.Player.InitialGold, config.Player.InitialPotions, config.Items.Potion.Price, bus);
            return new Player("Hero", config, DefaultStrategy, exp, inv, bus);
        }

        /// <summary>
        /// <c>EventManager.DetermineEventType</c> と同じ判定で、最初の抽選が目的の種別になるシードを探す。
        /// </summary>
        private static int FindSeed(GameConfig config, GameEventType wanted)
        {
            for (int s = 0; s < 100000; s++)
            {
                int roll = new Random(s).Next(0, config.Events.TotalWeight);
                var type = roll < config.Events.ShopEventWeight ? GameEventType.Shop : GameEventType.Battle;
                if (type == wanted)
                {
                    return s;
                }
            }
            throw new InvalidOperationException($"No seed found for {wanted}");
        }

        private static GameSystem BuildGame(GameEventType firstEvent, Func<IEnemy> enemyFactory, IGameInput input, out IPlayer player)
        {
            var config = GameConfigLoader.Instance;
            IGameMessageBus bus = new GameMessageBus();
            player = CreatePlayer(config, bus);
            var eventManager = new EventManager(player, config, new FakeEnemyFactory(enemyFactory), new GameRecord(), new Random(FindSeed(config, firstEvent)));
            return new GameSystem(player, input, eventManager, new NullRenderer(), bus, playerRepository: null);
        }

        [Fact]
        public void Start_BattleEncounter_StopsAwaitingAttack()
        {
            using var game = BuildGame(GameEventType.Battle, () => new TestEnemy("Weakling", 1, 0, DefaultStrategy), new ScriptedGameInput(), out _);

            game.Start();

            Assert.True(game.IsRunning);
            Assert.Equal(ExpectedInput.Attack, game.ExpectedInput);
            Assert.Equal("Battle", game.CurrentStateName);
            Assert.NotNull(game.CurrentBattleState);
            Assert.NotNull(game.CurrentEnemyState);
        }

        [Fact]
        public void BattleVictory_AdvancesToRestThenPostEncounter()
        {
            using var game = BuildGame(GameEventType.Battle, () => new TestEnemy("Weakling", 1, 0, DefaultStrategy), new ScriptedGameInput(), out _);
            game.Start();

            // 攻撃1回で弱い敵を倒す → 勝利 → 休憩へ
            game.Step(PlayerInput.ForAttack(DefaultAttack));
            Assert.True(game.IsRunning);
            Assert.Equal(ExpectedInput.Rest, game.ExpectedInput);
            Assert.Equal("Rest", game.CurrentStateName);

            // 休憩スキップ → 続行確認へ
            game.Step(PlayerInput.ForRest(null));
            Assert.Equal(ExpectedInput.GameAction, game.ExpectedInput);
            Assert.Equal("PostEncounter", game.CurrentStateName);

            // 終了 → ゲームオーバー（自動前進）で停止
            game.Step(PlayerInput.ForProgress(GameActionChoice.Quit));
            Assert.False(game.IsRunning);
        }

        [Fact]
        public void BattleDefeat_EndsGame()
        {
            using var game = BuildGame(GameEventType.Battle, () => new TestEnemy("Boss", 100000, 999999, DefaultStrategy), new ScriptedGameInput(), out var player);
            game.Start();
            Assert.Equal(ExpectedInput.Attack, game.ExpectedInput);

            game.Step(PlayerInput.ForAttack(DefaultAttack));

            Assert.False(game.IsRunning);
            Assert.False(player.IsAlive);
        }

        [Fact]
        public void Shop_RepeatsUntilExit_ThenRest()
        {
            using var game = BuildGame(GameEventType.Shop, () => new TestEnemy("Unused", 1, 0, DefaultStrategy), new ScriptedGameInput(), out _);
            game.Start();

            Assert.Equal(ExpectedInput.Shop, game.ExpectedInput);
            Assert.Equal("Shop", game.CurrentStateName);
            Assert.NotNull(game.CurrentShopState);

            // ポーション購入 → まだショップ（自己ループ）
            game.Step(PlayerInput.ForShop(new ShopAction(ShopActionType.BuyPotion, quantity: 1)));
            Assert.Equal(ExpectedInput.Shop, game.ExpectedInput);
            Assert.Equal("Shop", game.CurrentStateName);

            // 退店 → 休憩へ
            game.Step(PlayerInput.ForShop(new ShopAction(ShopActionType.Exit)));
            Assert.Equal(ExpectedInput.Rest, game.ExpectedInput);
            Assert.Equal("Rest", game.CurrentStateName);
        }

        [Fact]
        public void PostEncounterContinue_ReturnsToNextEncounter()
        {
            using var game = BuildGame(GameEventType.Battle, () => new TestEnemy("Weakling", 1, 0, DefaultStrategy), new ScriptedGameInput(), out _);
            game.Start();

            game.Step(PlayerInput.ForAttack(DefaultAttack));   // 勝利 → Rest
            game.Step(PlayerInput.ForRest(null));              // → PostEncounter
            game.Step(PlayerInput.ForProgress(GameActionChoice.Continue)); // → Explore → 次のエンカウント

            Assert.True(game.IsRunning);
            Assert.True(game.ExpectedInput == ExpectedInput.Attack || game.ExpectedInput == ExpectedInput.Shop);
        }

        [Fact]
        public void RunGameLoop_FullScriptedBattle_Terminates()
        {
            var input = new ScriptedGameInput(
                attacks: new[] { DefaultAttack },
                rests: new UseItemAction?[] { null },
                gameActions: new[] { GameActionChoice.Quit });
            using var game = BuildGame(GameEventType.Battle, () => new TestEnemy("Weakling", 1, 0, DefaultStrategy), input, out _);

            game.RunGameLoop(); // Quit で必ず停止する（無限ループしない）

            Assert.False(game.IsRunning);
        }
    }
}
