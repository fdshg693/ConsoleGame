using GameEngine.Models;

namespace GameEngine.Interfaces
{
    /// <summary>
    /// 勝敗記録の抽象。静的なプロセス共有状態だと並行リクエスト（API ホスト）で
    /// 競合するため、DI スコープ/シングルトン単位のインスタンスとして扱う。
    /// </summary>
    public interface IGameRecord
    {
        int TotalWins { get; }
        int TotalLosses { get; }
        int TotalGames { get; }

        /// <summary>勝利を1件記録する。</summary>
        void RecordWin();

        /// <summary>敗北を1件記録する。</summary>
        void RecordLoss();

        /// <summary>
        /// 保存済みの勝敗数で記録を上書き復元する（セッション復元用）。
        /// </summary>
        void Restore(int wins, int losses);

        /// <summary>表示用の集計メッセージ（勝敗数・勝率）を生成する。</summary>
        List<GameMessage> GetRecordMessages();
    }
}
