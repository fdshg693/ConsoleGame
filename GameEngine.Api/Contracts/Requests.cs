using GameEngine.DTOs;

namespace GameEngine.Api.Contracts
{
    /// <summary>新規ゲーム開始リクエスト。名前未指定なら "Hero"。</summary>
    public sealed class CreateSessionRequest
    {
        public string? PlayerName { get; set; }
    }

    /// <summary>確定セーブリクエスト。スロット未指定なら "auto_save"。</summary>
    public sealed class SaveRequest
    {
        public string? SlotName { get; set; }
    }

    /// <summary>ロードして新規セッションを開始するリクエスト。</summary>
    public sealed class LoadRequest
    {
        public string PlayerName { get; set; } = string.Empty;
        public string? SlotName { get; set; }
    }

    /// <summary>
    /// エンカウント後の進行選択リクエスト（<c>ExpectedInput=GameAction</c> のとき）。
    /// 値は <see cref="GameActionChoice"/>（Continue / SaveAndContinue / SaveAndQuit / Quit）。
    /// </summary>
    public sealed class ContinueRequest
    {
        public GameActionChoice Action { get; set; } = GameActionChoice.Continue;
    }
}
