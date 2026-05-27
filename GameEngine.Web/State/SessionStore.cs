using GameEngine.Contracts;
using GameEngine.Models;
using Microsoft.JSInterop;

namespace GameEngine.Web.State
{
    /// <summary>
    /// 進行中ゲームのクライアント側状態を保持する Scoped サービス（WASM では実質アプリ単位の単一インスタンス）。
    /// 保持するのは ①最新の <see cref="GameStateResponse"/> ②累積メッセージログ ③localStorage に永続化する
    /// <see cref="SessionId"/> のみ。サーバが差分（前ステップ以降の分）だけ返す <c>Messages</c> を
    /// クライアントで累積するのが本サービスの主目的。状態変化は <see cref="OnChange"/> で購読側へ通知する。
    /// </summary>
    public sealed class SessionStore
    {
        private const string StorageKey = "rpg.sessionId";

        private readonly IJSRuntime _js;
        private readonly List<GameMessage> _log = new();

        public SessionStore(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>現在のセッション ID（localStorage に永続化）。未開始なら null。</summary>
        public string? SessionId { get; private set; }

        /// <summary>直近の API レスポンス（最新状態）。</summary>
        public GameStateResponse? Current { get; private set; }

        /// <summary>セッション開始以降に蓄積したメッセージの累積ログ。</summary>
        public IReadOnlyList<GameMessage> Log => _log;

        /// <summary>状態が変化したときに発火する（コンポーネントは購読して再描画する）。</summary>
        public event Action? OnChange;

        /// <summary>起動時に localStorage から sessionId を読み出す（リロード復帰の起点。復帰自体は W4）。</summary>
        public async Task LoadSessionIdAsync()
        {
            SessionId = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }

        /// <summary>
        /// 新しいレスポンスを取り込む。別セッション（ID 変更）なら累積ログをリセットしてから差分を足す。
        /// 取り込み後に sessionId を localStorage へ保存し、購読側へ変更を通知する。
        /// </summary>
        public async Task ApplyAsync(GameStateResponse state)
        {
            bool isNewSession = !string.Equals(SessionId, state.SessionId, StringComparison.Ordinal);
            if (isNewSession)
            {
                _log.Clear();
            }

            Current = state;
            SessionId = state.SessionId;

            if (state.Messages is { Count: > 0 })
            {
                _log.AddRange(state.Messages);
            }

            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, SessionId);
            OnChange?.Invoke();
        }

        /// <summary>セッションを破棄して localStorage をクリアする（メニューへ戻るとき）。</summary>
        public async Task ClearAsync()
        {
            SessionId = null;
            Current = null;
            _log.Clear();
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            OnChange?.Invoke();
        }
    }
}
