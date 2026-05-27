using GameEngine.DTOs;
using GameEngine.Models;

namespace GameEngine.Api.Contracts
{
    /// <summary>
    /// 各 API ステップのレスポンス。既存 <see cref="GameState"/> DTO に、API 駆動で必要な
    /// セッション識別子・ステップ駆動メタ情報（次に必要な入力種別・状態名・稼働中フラグ）を加えたもの。
    /// クライアントは <see cref="ExpectedInput"/> を見て次に叩くエンドポイントを決める。
    /// </summary>
    public sealed class GameStateResponse
    {
        /// <summary>このゲーム進行を識別するセッション ID。</summary>
        public string SessionId { get; set; } = string.Empty;

        public PlayerState Player { get; set; } = null!;
        public EnemyState? CurrentEnemy { get; set; }
        public BattleState? CurrentBattle { get; set; }
        public ShopState? CurrentShop { get; set; }

        /// <summary>このステップで発生したメッセージ（前回ステップ以降に蓄積された分）。</summary>
        public List<GameMessage> Messages { get; set; } = new();

        public GamePhase Phase { get; set; }

        /// <summary>ステートマシンの現在ステート名（Start/Explore/Battle/Shop/Rest/PostEncounter/GameOver）。終了後は null。</summary>
        public string? CurrentStateName { get; set; }

        /// <summary>
        /// 次の操作で必要な入力種別。<c>Attack</c>→<c>battle/turn</c>、<c>Shop</c>→<c>shop/action</c>、
        /// <c>Rest</c>→<c>rest</c>、<c>GameAction</c>→<c>continue</c>。<c>None</c> はゲーム終了。
        /// </summary>
        public ExpectedInput ExpectedInput { get; set; }

        /// <summary>ゲームが進行中か（ステートマシンが稼働中か）。</summary>
        public bool IsRunning { get; set; }

        /// <summary>ゲームが終了したか。</summary>
        public bool IsGameOver { get; set; }
    }
}
