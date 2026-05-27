using System.Collections.Concurrent;
using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace GameEngine.Manager
{
    /// <summary>
    /// 進行中セッションをサーバメモリに保持する <see cref="ISessionRepository"/> 実装。
    /// 各エントリは TTL（既定 30 分）で失効し、参照時に期限切れを検出して破棄する。
    /// 単一インスタンス・PoC 向けの軽量実装で、並行アクセスに備え <see cref="ConcurrentDictionary{TKey,TValue}"/> を用いる。
    /// 複数インスタンス/永続再開が必要になったら Redis/DB 実装へ差し替える。
    /// </summary>
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);
        private readonly TimeSpan _ttl;
        private readonly Func<DateTime> _clock;

        /// <param name="ttl">セッションの有効期限。省略時は 30 分。</param>
        /// <param name="clock">現在時刻の供給源（テストで失効を決定的に再現するための継ぎ目）。省略時は UTC 現在時刻。</param>
        public InMemorySessionRepository(TimeSpan? ttl = null, Func<DateTime>? clock = null)
        {
            _ttl = ttl ?? TimeSpan.FromMinutes(30);
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public Task<bool> SaveAsync(GameSessionState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(state.SessionId))
                throw new ArgumentException("SessionId is required", nameof(state));

            var expiresAt = _clock() + _ttl;
            _store[state.SessionId] = new Entry(state, expiresAt);
            return Task.FromResult(true);
        }

        public Task<GameSessionState?> LoadAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.FromResult<GameSessionState?>(null);

            if (!_store.TryGetValue(sessionId, out var entry))
                return Task.FromResult<GameSessionState?>(null);

            if (_clock() >= entry.ExpiresAt)
            {
                // 失効分は参照時に破棄する（遅延クリーンアップ）
                _store.TryRemove(sessionId, out _);
                return Task.FromResult<GameSessionState?>(null);
            }

            return Task.FromResult<GameSessionState?>(entry.State);
        }

        public Task<bool> DeleteAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.FromResult(false);

            return Task.FromResult(_store.TryRemove(sessionId, out _));
        }

        private readonly struct Entry
        {
            public GameSessionState State { get; }
            public DateTime ExpiresAt { get; }

            public Entry(GameSessionState state, DateTime expiresAt)
            {
                State = state;
                ExpiresAt = expiresAt;
            }
        }
    }
}
