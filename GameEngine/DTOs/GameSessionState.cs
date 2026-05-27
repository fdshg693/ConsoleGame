namespace GameEngine.DTOs
{
    /// <summary>
    /// 進行中ゲームの完全な揮発状態（セッション）を表すスナップショット DTO。
    /// 確定セーブ（<see cref="PlayerSaveData"/>）が「プレイヤーステータスの確定スナップショット」であるのに対し、
    /// 本クラスは「戦闘途中を含む進行中の状態」を識別子（<see cref="SessionId"/>）単位で保持・復元するための
    /// 揮発状態を表す。HTTP のようにリクエスト間で制御が切れる環境で、次リクエストまで状態を運ぶために使う。
    /// </summary>
    public class GameSessionState
    {
        /// <summary>セッションを識別する ID（プレイヤー/接続ごとに分離）。</summary>
        public required string SessionId { get; set; }

        public required string PlayerName { get; set; }

        /// <summary>現在のフェーズ（探索/戦闘/ショップ/休憩/ゲームオーバー等）。</summary>
        public GamePhase Phase { get; set; }

        /// <summary>状態機械の現在ステート名（Start/Explore/Battle/Shop/Rest/PostEncounter/GameOver）。終了後は null。</summary>
        public string? CurrentStateName { get; set; }

        /// <summary>次の Step で必要な入力種別。クライアントはこれを見て次の行動を組み立てる。</summary>
        public ExpectedInput ExpectedInput { get; set; }

        /// <summary>プレイヤーステータスのスナップショット（復元に用いる確定情報）。</summary>
        public required PlayerSaveData Player { get; set; }

        /// <summary>戦闘中の敵状態（現在 HP 含む）。戦闘外は null。</summary>
        public EnemyState? Enemy { get; set; }

        /// <summary>戦闘の状態（ターン数等）。戦闘外は null。</summary>
        public BattleState? Battle { get; set; }

        /// <summary>ショップの状態（品揃え・価格）。ショップ外は null。</summary>
        public ShopState? Shop { get; set; }

        /// <summary>勝敗記録（勝利数）。復元時に <c>IGameRecord.Restore</c> へ渡す。</summary>
        public int TotalWins { get; set; }

        /// <summary>勝敗記録（敗北数）。</summary>
        public int TotalLosses { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
