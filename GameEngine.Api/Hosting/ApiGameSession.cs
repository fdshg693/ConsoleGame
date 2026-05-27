using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Systems;

namespace GameEngine.Api.Hosting
{
    /// <summary>
    /// 1つの進行中ゲームに対応するサーバ常駐インスタンス。固有の object graph
    /// （メッセージバス・プレイヤー・敵ファクトリ・勝敗記録・<see cref="EventManager"/>・<see cref="GameSystem"/>）を所有する。
    /// </summary>
    /// <remarks>
    /// ステートマシンはスナップショットから再水和（rehydrate）できないため、API はセッションごとに
    /// <see cref="GameSystem"/> を「生きたまま」保持し、リクエストごとに <see cref="GameSystem.Step"/> を1回適用する。
    /// 同一セッションへの並行リクエストは <see cref="SyncRoot"/> ロックで直列化する（<see cref="GameSystem"/> はスレッドセーフではない）。
    /// 生成・破棄・TTL 失効は <see cref="GameSessionManager"/> が管理する。
    /// </remarks>
    public sealed class ApiGameSession : IDisposable
    {
        /// <summary>ステップ適用とレスポンス構築を直列化するためのロック対象。</summary>
        public object SyncRoot { get; } = new();

        public string SessionId { get; }

        /// <summary>このセッションのプレイヤー。確定セーブ（<see cref="IPlayerRepository.SaveAsync"/>）でも使う。</summary>
        public IPlayer Player { get; }

        public GameSystem GameSystem { get; }

        /// <summary>このセッションの出力シンク。<see cref="BufferingRenderer.DrainMessages"/> でメッセージを回収する。</summary>
        public BufferingRenderer Renderer { get; }

        /// <summary>最終アクセス時刻（UTC）。TTL 失効の判定に使う。</summary>
        public DateTime LastAccessUtc { get; private set; }

        public ApiGameSession(
            string sessionId,
            IPlayer player,
            GameSystem gameSystem,
            BufferingRenderer renderer,
            DateTime nowUtc)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            GameSystem = gameSystem ?? throw new ArgumentNullException(nameof(gameSystem));
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            LastAccessUtc = nowUtc;
        }

        public ExpectedInput ExpectedInput => GameSystem.ExpectedInput;
        public bool IsRunning => GameSystem.IsRunning;

        /// <summary>1行動を適用して1ステップ進める（呼び出し側は <see cref="SyncRoot"/> をロックすること）。</summary>
        public void Step(PlayerInput input) => GameSystem.Step(input);

        /// <summary>最終アクセス時刻を更新する。</summary>
        public void Touch(DateTime nowUtc) => LastAccessUtc = nowUtc;

        public void Dispose() => GameSystem.Dispose();
    }
}
