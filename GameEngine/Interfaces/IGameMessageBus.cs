using GameEngine.Models;

namespace GameEngine.Interfaces
{
    /// <summary>
    /// ドメインメッセージの発行/購読バス。
    /// 静的イベントだと並行リクエスト時に購読が混線するため、インスタンス（DI スコープ単位）で扱う。
    /// 発行側（<c>Player</c>・各 Manager・<c>Enemy</c>）に注入し、購読側（出力シンク）は <c>GameSystem</c> が接続する。
    /// </summary>
    public interface IGameMessageBus
    {
        /// <summary>メッセージが発行されたときに発火する。</summary>
        event Action<GameMessage>? MessagePublished;

        /// <summary>テキストと種別からメッセージを発行する。</summary>
        void Publish(string text, MessageType type);

        /// <summary>構築済みのメッセージを発行する。</summary>
        void Publish(GameMessage message);
    }
}
