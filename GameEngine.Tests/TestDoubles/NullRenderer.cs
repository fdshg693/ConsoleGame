using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Tests.TestDoubles
{
    /// <summary>
    /// 何も描画しない <see cref="IRenderer"/> 実装。
    /// 合成（DI）テストやロジックテストで出力に依存しない検証を行うためのテストダブル。
    /// </summary>
    public sealed class NullRenderer : IRenderer
    {
        public void ClearScreen(string title) { }
        public void WaitForKeyPress(string prompt = "Press any key to continue...") { }
        public void RenderMessage(GameMessage message) { }
        public void RenderMessages(IEnumerable<GameMessage> messages) { }
        public void WriteInfo(string text) { }
        public void WriteSuccess(string text) { }
        public void WriteWarning(string text) { }
        public void WriteError(string text) { }
        public void WriteSystem(string text) { }
        public void RenderHPBar(string name, int current, int max, int barWidth = 20) { }
        public void RenderStatusPanel(PlayerState player, EnemyState? enemy = null) { }
        public void WriteSeparator() { }
        public void WriteResultBox(string title, string[] details, bool isVictory) { }
    }
}
