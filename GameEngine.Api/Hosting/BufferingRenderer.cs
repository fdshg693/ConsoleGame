using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Api.Hosting
{
    /// <summary>
    /// API ホスト向けの <see cref="IRenderer"/> 実装。コンソールのように即時描画せず、
    /// ユーザー向けメッセージをバッファに蓄積し、1ステップ完了後に <see cref="DrainMessages"/> で
    /// レスポンス（<c>GameState.Messages</c>）へまとめて引き渡す。
    /// </summary>
    /// <remarks>
    /// 取り込むのは「ユーザーに見せる意味のあるメッセージ」のみ:
    /// <list type="bullet">
    ///   <item><see cref="RenderMessage"/> … メッセージバス経由のドメインイベント（<c>GameSystem</c> が購読して流す）</item>
    ///   <item><see cref="RenderMessages"/> … 戦闘/ショップ/休憩/エンカウントの結果メッセージ（State が直接描画）</item>
    ///   <item><see cref="WriteInfo"/>/<see cref="WriteSuccess"/>/<see cref="WriteWarning"/>/<see cref="WriteError"/> … セーブ結果・無効選択など</item>
    ///   <item><see cref="WriteResultBox"/> … 勝利/敗北/ゲームオーバーの結果ボックス</item>
    /// </list>
    /// 一方、コンソール固有の装飾（画面クリア・HP バー・ステータスパネル・区切り線・キー待ち）と、
    /// <see cref="WriteSystem"/>（状態遷移ログ <c>[State] X -&gt; Y</c> の診断出力）は no-op として捨てる。
    /// 構造化された現在状態は <c>PlayerState</c>/<c>BattleState</c>/<c>EnemyState</c>/<c>ShopState</c> DTO が保持するため、
    /// 描画系の取り込みは不要。
    /// </remarks>
    public sealed class BufferingRenderer : IRenderer
    {
        private readonly object _sync = new();
        private readonly List<GameMessage> _buffer = new();

        /// <summary>
        /// 蓄積済みメッセージを取り出してバッファをクリアする。各ステップのレスポンス構築時に1度だけ呼ぶ。
        /// </summary>
        public List<GameMessage> DrainMessages()
        {
            lock (_sync)
            {
                var drained = new List<GameMessage>(_buffer);
                _buffer.Clear();
                return drained;
            }
        }

        private void Add(GameMessage message)
        {
            lock (_sync)
            {
                _buffer.Add(message);
            }
        }

        private void Add(string text, MessageType type)
        {
            Add(new GameMessage { Text = text, Type = type, Timestamp = DateTime.UtcNow });
        }

        // ── 取り込む: ユーザー向けメッセージ ─────────────────────────

        public void RenderMessage(GameMessage message)
        {
            if (message != null)
            {
                Add(message);
            }
        }

        public void RenderMessages(IEnumerable<GameMessage> messages)
        {
            if (messages == null)
            {
                return;
            }

            foreach (var message in messages)
            {
                if (message != null)
                {
                    Add(message);
                }
            }
        }

        public void WriteInfo(string text) => Add(text?.Trim() ?? string.Empty, MessageType.Info);
        public void WriteSuccess(string text) => Add(text?.Trim() ?? string.Empty, MessageType.Success);
        public void WriteWarning(string text) => Add(text?.Trim() ?? string.Empty, MessageType.Warning);
        public void WriteError(string text) => Add(text?.Trim() ?? string.Empty, MessageType.Error);

        public void WriteResultBox(string title, string[] details, bool isVictory)
        {
            Add(title?.Trim() ?? string.Empty, isVictory ? MessageType.Success : MessageType.Error);
            if (details != null)
            {
                foreach (var detail in details)
                {
                    Add(detail?.Trim() ?? string.Empty, MessageType.Info);
                }
            }
        }

        // ── 捨てる: コンソール固有の装飾・診断 ─────────────────────────

        /// <summary>状態遷移ログ（<c>[State] X -&gt; Y</c>）の診断出力。API レスポンスには載せない。</summary>
        public void WriteSystem(string text) { /* no-op: diagnostic transition log */ }

        public void ClearScreen(string title) { /* no-op */ }
        public void WaitForKeyPress(string prompt = "Press any key to continue...") { /* no-op: API は待たない */ }
        public void RenderHPBar(string name, int current, int max, int barWidth = 20) { /* no-op */ }
        public void RenderStatusPanel(PlayerState player, EnemyState? enemy = null) { /* no-op */ }
        public void WriteSeparator() { /* no-op */ }
    }
}
