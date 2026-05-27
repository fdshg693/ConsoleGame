using GameEngine.Configuration;
using GameEngine.Constants;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Models;
using GameEngine.Systems;

namespace CliRpgGame
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== CLI RPG Game ===");
                Console.WriteLine("Loading game configuration...");

                // 設定を一度だけ読み込み、以降は明示的に引き回す
                // （静的シングルトンへの直アクセスはこの Composition Root に集約する）
                var config = GameConfigLoader.Instance;
                Console.WriteLine("Configuration loaded successfully!\n");

                // プレイヤー名の入力
                Console.Write("Enter your name: ");
                string? input = Console.ReadLine();
                string playerName = string.IsNullOrWhiteSpace(input)
                    ? "Hero"
                    : input.Trim();

                // 依存の組み立て（Composition Root）
                IPlayer player = CreatePlayer(playerName, config);

                // リポジトリの初期化（MongoDBが利用できない場合はnull）
                IPlayerRepository? playerRepository = CreatePlayerRepository(config);

                var gameInput = new ConsoleGameInput(
                    config.Items.Potion.Price,
                    config.Items.Potion.HealAmount);

                // 敵生成ファクトリ（設定を注入し、戦闘フローへ引き回す継ぎ目）
                IEnemyFactory enemyFactory = new EnemyFactory(config.Enemy);

                var eventManager = new EventManager(player, gameInput, config, enemyFactory);

                // ゲームシステムの初期化と実行（IDisposable で GameMessageBus の購読を解除）
                using var gameSystem = new GameSystem(player, gameInput, eventManager, playerRepository);
                gameSystem.RunGameLoop();

                Console.WriteLine("\nThank you for playing! Press any key to exit.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFatal Error: {ex.Message}");
                Console.WriteLine("The game cannot continue. Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private static IPlayerRepository? CreatePlayerRepository(GameConfig config)
        {
            try
            {
                return new MongoPlayerRepository(
                    config.MongoDB.ConnectionString,
                    config.MongoDB.DatabaseName,
                    config.MongoDB.CollectionName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: セーブ機能を初期化できませんでした: {ex.Message}");
                Console.WriteLine("ゲームはセーブ機能なしで続行されます。");
                return null;
            }
        }

        /// <summary>
        /// プレイヤーオブジェクトを作成する
        /// </summary>
        private static IPlayer CreatePlayer(string name, GameConfig config)
        {
            var experienceManager = new ExperienceManager(config.LevelUp.ExperienceRequired);
            var inventoryManager = new InventoryManager(
                config.Player.InitialGold,
                config.Player.InitialPotions,
                config.Items.Potion.Price);

            return new Player(
                name,
                config,
                AttackStrategy.GetAttackStrategy(AttackStrategyNames.Default),
                experienceManager,
                inventoryManager);
        }
    }
}
