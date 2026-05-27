using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ゲーム終了状態（入力不要・自動前進して終端 <see cref="Trigger.Done"/> を返す）。
    /// </summary>
    public class GameOverState : IGameState
    {
        public string Name => "GameOver";
        public ExpectedInput ExpectedInput => ExpectedInput.None;

        public Trigger Execute(GameFlowContext context)
        {
            context.ClearScreen("GAME OVER");
            context.DisplayGameOver();
            return Trigger.Done;
        }
    }
}
