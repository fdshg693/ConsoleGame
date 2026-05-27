using GameEngine.Configuration;
using GameEngine.Constants;
using GameEngine.DependencyInjection;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Models;
using GameEngine.Systems;
using GameEngine.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameEngine.Tests.DependencyInjection
{
    /// <summary>
    /// フェーズ0で導入した DI 合成（AddGameEngine）の回帰テスト。
    /// 旧 Program.cs の手動合成と同じ依存グラフが DI 経由でも組み立てられることを担保する。
    /// </summary>
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddGameEngine_RegistersGameConfig_AsSingleton()
        {
            using var provider = new ServiceCollection().AddGameEngine().BuildServiceProvider();

            var first = provider.GetRequiredService<GameConfig>();
            var second = provider.GetRequiredService<GameConfig>();

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public void AddGameEngine_RegistersEnemyFactory_Resolvable()
        {
            using var provider = new ServiceCollection().AddGameEngine().BuildServiceProvider();

            var factory = provider.GetRequiredService<IEnemyFactory>();

            Assert.NotNull(factory);
            Assert.NotEmpty(factory.GetAvailableEnemyKeys());
        }

        [Fact]
        public void AddGameEngine_RegistersPhase3Services_Resolvable()
        {
            using var provider = new ServiceCollection().AddGameEngine().BuildServiceProvider();

            // フェーズ3で追加したコアサービスが解決できること
            Assert.NotNull(provider.GetRequiredService<IPlayerFactory>());
            Assert.NotNull(provider.GetRequiredService<ISessionRepository>());

            // 勝敗記録は Singleton（同一インスタンスを共有）
            var first = provider.GetRequiredService<IGameRecord>();
            var second = provider.GetRequiredService<IGameRecord>();
            Assert.Same(first, second);

            // IPlayerFactory で新規プレイヤーが生成できること
            var player = provider.GetRequiredService<IPlayerFactory>().CreateNew("Tester");
            Assert.Equal("Tester", player.Name);
        }

        [Fact]
        public void Composition_WithoutRepository_ResolvesGameSystem()
        {
            // リポジトリ未登録（MongoDB 不可相当）でも、GameSystem の
            // IPlayerRepository? 既定値（null）で合成が成立すること（旧挙動の回帰）。
            using var provider = BuildComposedProvider(repository: null);

            using var gameSystem = provider.GetRequiredService<GameSystem>();

            Assert.NotNull(gameSystem);
        }

        [Fact]
        public void Composition_WithRepository_ResolvesGameSystemAndDependencies()
        {
            using var provider = BuildComposedProvider(repository: new InMemoryPlayerRepository());

            // 進行制御まで含めた依存グラフが解決できること
            Assert.NotNull(provider.GetRequiredService<EventManager>());
            using var gameSystem = provider.GetRequiredService<GameSystem>();
            Assert.NotNull(gameSystem);

            // ホストが注入したプレイヤーが解決されること
            var player = provider.GetRequiredService<IPlayer>();
            Assert.Equal("Tester", player.Name);
        }

        /// <summary>
        /// コンソールホスト（Program.cs）の合成を再現する。
        /// UI 実装はスタブ、プレイヤー名は固定、リポジトリは任意。
        /// </summary>
        private static ServiceProvider BuildComposedProvider(IPlayerRepository? repository)
        {
            var services = new ServiceCollection();
            services.AddGameEngine();
            services.AddSingleton<IGameInput>(new StubGameInput());
            // 出力シンクはホスト責務。テストでは描画しない NullRenderer を登録する。
            services.AddSingleton<IRenderer>(new NullRenderer());
            services.AddSingleton<IPlayer>(sp =>
                CreatePlayer(
                    "Tester",
                    sp.GetRequiredService<GameConfig>(),
                    sp.GetRequiredService<IGameMessageBus>()));
            if (repository != null)
            {
                services.AddSingleton(repository);
            }
            return services.BuildServiceProvider();
        }

        private static IPlayer CreatePlayer(string name, GameConfig config, IGameMessageBus bus)
        {
            var experienceManager = new ExperienceManager(config.LevelUp.ExperienceRequired, bus);
            var inventoryManager = new InventoryManager(
                config.Player.InitialGold,
                config.Player.InitialPotions,
                config.Items.Potion.Price,
                bus);

            return new Player(
                name,
                config,
                AttackStrategy.GetAttackStrategy(AttackStrategyNames.Default),
                experienceManager,
                inventoryManager,
                bus);
        }

        /// <summary>合成の解決のみを検証するための入力スタブ（戦闘/ショップは走らせない）。</summary>
        private sealed class StubGameInput : IGameInput
        {
            public AttackAction SelectAttackAction(BattleState battleState, PlayerState playerState, EnemyState enemyState)
                => new AttackAction(AttackStrategyNames.Default);

            public ShopAction SelectShopAction(ShopState shopState, PlayerState playerState)
                => new ShopAction(ShopActionType.Exit);

            public UseItemAction? SelectRestAction(PlayerState playerState) => null;

            public GameActionChoice SelectGameAction() => GameActionChoice.Continue;
        }
    }
}
