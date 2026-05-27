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
    /// ドメインメッセージの発行バス
    /// </summary>
    public static class GameMessageBus
    {
        public static event Action<GameMessage>? MessagePublished;

        public static void Publish(string text, MessageType type)
        {
            var message = new GameMessage
            {
                Text = text,
                Type = type,
                Timestamp = DateTime.UtcNow
            };
            MessagePublished?.Invoke(message);
        }

        public static void Publish(GameMessage message)
        {
            MessagePublished?.Invoke(message);
        }
    }
}
