using GameEngine.DTOs;

namespace GameEngine.Interfaces
{
    /// <summary>
    /// 進行中セッション（<see cref="GameSessionState"/>）の保存/復元を抽象化する。
    /// 確定セーブの <see cref="IPlayerRepository"/> とは責務を分離する
    /// （セーブ＝確定スナップショット、セッション＝進行中の揮発状態）。
    /// 既定実装はインメモリ + TTL（<c>InMemorySessionRepository</c>）。スケール要件次第で Redis/DB へ拡張する。
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>セッション状態を保存（同一 <see cref="GameSessionState.SessionId"/> は上書き）。</summary>
        Task<bool> SaveAsync(GameSessionState state);

        /// <summary>セッション状態を復元する。未存在/失効時は null。</summary>
        Task<GameSessionState?> LoadAsync(string sessionId);

        /// <summary>セッションを削除する。削除できたら true。</summary>
        Task<bool> DeleteAsync(string sessionId);
    }
}
