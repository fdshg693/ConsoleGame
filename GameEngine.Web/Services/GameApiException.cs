using System.Net;

namespace GameEngine.Web.Services
{
    /// <summary>
    /// API が非成功ステータスを返したときに送出する例外。HTTP ステータス別ハンドリング
    /// （404→新規誘導 / 409→最新状態を再取得 / 400→入力エラー表示 / 503→セーブ UI 無効化）の
    /// ために <see cref="StatusCode"/> を保持する。本文に <c>{ "error": "..." }</c> があれば
    /// <see cref="Exception.Message"/> に載せる（無ければステータス相当の既定文言）。
    /// </summary>
    public sealed class GameApiException : Exception
    {
        public GameApiException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
