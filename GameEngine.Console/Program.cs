using CliRpgGame.UI;
using GameEngine.Configuration;
using GameEngine.DependencyInjection;
using GameEngine.Interfaces;
using GameEngine.Manager;
using GameEngine.Systems;
using Microsoft.Extensions.DependencyInjection;

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

                // 設定を一度だけ読み込み、起動時バリデーションを通す。
                // GameConfigLoader.Instance への直アクセスはこの合成起点に集約する。
                var config = GameConfigLoader.Instance;
                Console.WriteLine("Configuration loaded successfully!\n");

                // プレイヤー名の入力
                Console.Write("Enter your name: ");
                string? input = Console.ReadLine();
                string playerName = string.IsNullOrWhiteSpace(input)
                    ? "Hero"
                    : input.Trim();

                // 依存の組み立て（Composition Root）— DI コンテナへ移行
                var services = new ServiceCollection();

                // コア依存（設定・敵生成ファクトリ・進行制御・メッセージバス）をまとめて登録
                services.AddGameEngine();

                // 出力（描画）のコンソール実装。GameSystem / EventManager（IRenderer 経由）と
                // ConsoleGameInput（メニュー描画）が同一インスタンスを共有するよう単一の ConsoleRenderer を登録する。
                services.AddSingleton<ConsoleRenderer>();
                services.AddSingleton<IRenderer>(sp => sp.GetRequiredService<ConsoleRenderer>());

                // コンソール固有の入力実装（描画は ConsoleRenderer を共有）
                services.AddSingleton<IGameInput>(sp =>
                {
                    var c = sp.GetRequiredService<GameConfig>();
                    return new ConsoleGameInput(
                        sp.GetRequiredService<ConsoleRenderer>(),
                        c.Items.Potion.Price,
                        c.Items.Potion.HealAmount);
                });

                // プレイヤー（名前は実行時入力。将来はセッション単位で生成する）。
                // 生成は AddGameEngine が登録する IPlayerFactory に委譲する（新規/復元の経路を一元化）。
                services.AddSingleton<IPlayer>(sp =>
                    sp.GetRequiredService<IPlayerFactory>().CreateNew(playerName));

                // セーブ用リポジトリ。MongoDB が利用できない場合は登録せず、
                // GameSystem は IPlayerRepository? の既定値（null）でセーブ無効のまま続行する。
                IPlayerRepository? playerRepository = CreatePlayerRepository(config);
                if (playerRepository != null)
                {
                    services.AddSingleton<IPlayerRepository>(playerRepository);
                }

                using var provider = services.BuildServiceProvider();

                // GameSystem は IDisposable（GameMessageBus 購読解除）。
                // provider 破棄時にも解除されるが、明示的な using でスコープ終了を分かりやすくする。
                using var gameSystem = provider.GetRequiredService<GameSystem>();
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
    }
}
