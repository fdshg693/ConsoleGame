using System.Collections.Concurrent;
using GameEngine.Configuration;
using GameEngine.DTOs;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Models;
using GameEngine.Systems;

namespace GameEngine.Api.Hosting
{
    /// <summary>
    /// 進行中の <see cref="ApiGameSession"/> 群を保持・生成・破棄するサーバ常駐マネージャ（Singleton）。
    /// </summary>
    /// <remarks>
    /// エンジンのステートフルなサービス（メッセージバス・プレイヤー・勝敗記録・<see cref="EventManager"/>・<see cref="GameSystem"/>）は
    /// 本来「1ゲーム=1グラフ」であり、複数セッションを捌く API では <c>AddGameEngine</c> の Singleton 登録をそのまま使えない。
    /// そのため、セッションごとに専用の object graph を手組みして並行リクエスト間の状態混線を防ぐ:
    /// <para>
    /// 共有（Singleton）するのは設定（<see cref="GameConfig"/>）と任意の確定セーブ用 <see cref="IPlayerRepository"/> のみ。
    /// 1セッション専用に新規生成するのは <see cref="GameMessageBus"/> → それに紐づく <see cref="PlayerFactory"/> 由来のプレイヤー →
    /// 同じバスに紐づく <see cref="EnemyFactory"/> → <see cref="GameRecord"/> → <see cref="EventManager"/> → <see cref="BufferingRenderer"/> → <see cref="GameSystem"/>。
    /// </para>
    /// セッションは TTL（最終アクセスからの経過、既定 30 分）で失効し、参照/生成時に遅延クリーンアップする。
    /// </remarks>
    public sealed class GameSessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ApiGameSession> _sessions = new(StringComparer.Ordinal);
        private readonly GameConfig _config;
        private readonly IPlayerRepository? _playerRepository;
        private readonly TimeSpan _ttl;
        private readonly Func<DateTime> _clock;

        /// <param name="config">共有設定。</param>
        /// <param name="playerRepository">確定セーブ用リポジトリ。未登録（null）ならセーブ/ロード系 API は利用不可。</param>
        /// <param name="ttl">セッションの有効期限（最終アクセス基準）。省略時は 30 分。</param>
        /// <param name="clock">現在時刻の供給源（テストで失効を決定的に再現するための継ぎ目）。省略時は UTC 現在時刻。</param>
        public GameSessionManager(
            GameConfig config,
            IPlayerRepository? playerRepository = null,
            TimeSpan? ttl = null,
            Func<DateTime>? clock = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _playerRepository = playerRepository;
            _ttl = ttl ?? TimeSpan.FromMinutes(30);
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        /// <summary>確定セーブ/ロードが利用可能か（<see cref="IPlayerRepository"/> が登録されているか）。</summary>
        public bool SaveLoadEnabled => _playerRepository != null;

        /// <summary>新規プレイヤーでセッションを開始し、最初のエンカウントまで前進させる。</summary>
        public ApiGameSession CreateNew(string? playerName)
        {
            Sweep();
            var bus = new GameMessageBus();
            var player = new PlayerFactory(_config, bus).CreateNew(Sanitize(playerName));
            return Build(bus, player);
        }

        /// <summary>
        /// セーブデータからプレイヤーを復元して新規セッションを開始する。
        /// 復元できるのはプレイヤーステータスのみのため、戦闘途中ではなく探索（最初のエンカウント）から再開する。
        /// </summary>
        /// <returns>リポジトリ未登録、または該当セーブが無い場合は null。</returns>
        public async Task<ApiGameSession?> CreateFromSaveAsync(string playerName, string saveSlotName)
        {
            if (_playerRepository == null)
            {
                return null;
            }

            PlayerSaveData? data = await _playerRepository.LoadAsync(playerName, saveSlotName);
            if (data == null)
            {
                return null;
            }

            Sweep();
            var bus = new GameMessageBus();
            var player = new PlayerFactory(_config, bus).Restore(data);
            return Build(bus, player);
        }

        /// <summary>セッションを取得する。未存在・TTL 失効時は null（失効分は破棄する）。</summary>
        public ApiGameSession? Get(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || !_sessions.TryGetValue(sessionId, out var session))
            {
                return null;
            }

            if (IsExpired(session))
            {
                Remove(sessionId);
                return null;
            }

            return session;
        }

        /// <summary>セッションを破棄する（<see cref="GameSystem"/> の購読解除を含む）。削除できたら true。</summary>
        public bool Remove(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId) && _sessions.TryRemove(sessionId, out var session))
            {
                session.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>セッションのプレイヤーを確定セーブする。リポジトリ未登録時は null（呼び出し側が 503 を返す）。</summary>
        public async Task<bool?> SaveAsync(ApiGameSession session, string saveSlotName)
        {
            if (_playerRepository == null)
            {
                return null;
            }
            return await _playerRepository.SaveAsync(session.Player, saveSlotName);
        }

        /// <summary>指定プレイヤーのセーブ一覧。リポジトリ未登録時は null。</summary>
        public async Task<List<PlayerSaveData>?> GetSaveListAsync(string playerName)
        {
            if (_playerRepository == null)
            {
                return null;
            }
            return await _playerRepository.GetSaveListAsync(playerName);
        }

        /// <summary>セーブを削除する。リポジトリ未登録時は null。</summary>
        public async Task<bool?> DeleteSaveAsync(string playerName, string saveSlotName)
        {
            if (_playerRepository == null)
            {
                return null;
            }
            return await _playerRepository.DeleteAsync(playerName, saveSlotName);
        }

        /// <summary>
        /// セッション専用の object graph を手組みして <see cref="GameSystem.Start"/> まで進め、ストアへ登録する。
        /// </summary>
        private ApiGameSession Build(GameMessageBus bus, IPlayer player)
        {
            var renderer = new BufferingRenderer();
            var enemyFactory = new EnemyFactory(_config.Enemy, bus);
            var gameRecord = new GameRecord();
            var eventManager = new EventManager(player, _config, enemyFactory, gameRecord);
            var gameSystem = new GameSystem(player, new ApiGameInput(), eventManager, renderer, bus, _playerRepository);

            // 最初の入力待ち状態（最初のエンカウント）まで前進させる。
            gameSystem.Start();

            var sessionId = Guid.NewGuid().ToString("N")[..12];
            var session = new ApiGameSession(sessionId, player, gameSystem, renderer, _clock());
            _sessions[sessionId] = session;
            return session;
        }

        private bool IsExpired(ApiGameSession session) => _clock() >= session.LastAccessUtc + _ttl;

        /// <summary>TTL 失効済みセッションを破棄する（遅延クリーンアップ）。</summary>
        private void Sweep()
        {
            foreach (var pair in _sessions)
            {
                if (IsExpired(pair.Value))
                {
                    Remove(pair.Key);
                }
            }
        }

        private static string Sanitize(string? name)
            => string.IsNullOrWhiteSpace(name) ? "Hero" : name.Trim();

        public void Dispose()
        {
            foreach (var pair in _sessions)
            {
                if (_sessions.TryRemove(pair.Key, out var session))
                {
                    session.Dispose();
                }
            }
        }
    }
}
