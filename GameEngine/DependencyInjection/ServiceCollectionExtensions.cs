using GameEngine.Configuration;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Models;
using GameEngine.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace GameEngine.DependencyInjection
{
    /// <summary>
    /// ゲームエンジンのコア依存を DI コンテナへ登録する拡張。
    /// コンソール / API いずれのホストからも呼び出し、合成を一元化する。
    /// </summary>
    /// <remarks>
    /// ここで登録するのは UI に依存せず、プレイヤー名のような実行時入力を必要としない
    /// コア依存（設定・敵生成ファクトリ・進行制御）のみ。
    /// ホスト固有の実装は各ホストが登録する:
    /// <list type="bullet">
    ///   <item><see cref="IGameInput"/> … コンソール/API それぞれの入力実装</item>
    ///   <item><see cref="IRenderer"/> … 出力（描画）実装。コンソールは ANSI、API はバッファ/DTO へ蓄積</item>
    ///   <item><see cref="IPlayer"/> … 実行時のプレイヤー名から生成（セッション単位）</item>
    ///   <item><see cref="IPlayerRepository"/> … 任意。未登録なら <see cref="GameSystem"/> はセーブ無効で動作</item>
    /// </list>
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGameEngine(this IServiceCollection services)
        {
            // 設定は GameConfigLoader（シングルトン）から1度だけ解決して Singleton 登録する。
            // 直アクセスはここ（DI 合成）に閉じ込め、各コンシューマは GameConfig 注入で受け取る。
            services.AddSingleton(_ => GameConfigLoader.Instance);

            // ドメインメッセージバス（インスタンスベース）。発行側（Player/Manager/Enemy）と
            // 購読側（GameSystem → IRenderer）が同一インスタンスを共有するよう Singleton 登録する。
            services.AddSingleton<IGameMessageBus, GameMessageBus>();

            // 敵生成ファクトリ（設定由来の不変オブジェクト）。生成する Enemy にメッセージバスを伝播する。
            services.AddSingleton<IEnemyFactory>(sp =>
                new EnemyFactory(
                    sp.GetRequiredService<GameConfig>().Enemy,
                    sp.GetRequiredService<IGameMessageBus>()));

            // 進行制御。IPlayer / IGameInput はホストが登録するため、
            // 解決時（ホストの登録完了後）に依存が満たされる。
            services.AddSingleton<EventManager>();
            services.AddSingleton<GameSystem>();

            return services;
        }
    }
}
