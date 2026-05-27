using GameEngine.Interfaces;

namespace GameEngine.Models
{
    /// <summary>
    /// ゲームメッセージ
    /// UI層で表示するログメッセージ
    /// </summary>
    public class GameMessage
    {
        public string Text { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// メッセージの種類
    /// </summary>
    public enum MessageType
    {
        Info,       // 一般情報
        Success,    // 成功メッセージ
        Warning,    // 警告
        Error,      // エラー
        Combat,     // 戦闘ログ
        System,     // システムメッセージ
        Experience, // 経験値・レベルアップ
        Gold        // ゴールド関連
    }

    /// <summary>
    /// ドメインメッセージの発行バス（インスタンスベース）。
    /// 旧静的実装は並行リクエストで購読が混線するため、DI スコープ単位のインスタンスとして扱う。
    /// 発行側に注入し、<c>GameSystem</c> が購読して出力シンク（<see cref="IRenderer"/>）へ流す。
    /// </summary>
    public class GameMessageBus : IGameMessageBus
    {
        public event Action<GameMessage>? MessagePublished;

        public void Publish(string text, MessageType type)
        {
            var message = new GameMessage
            {
                Text = text,
                Type = type,
                Timestamp = DateTime.UtcNow
            };
            MessagePublished?.Invoke(message);
        }

        public void Publish(GameMessage message)
        {
            MessagePublished?.Invoke(message);
        }
    }
}
