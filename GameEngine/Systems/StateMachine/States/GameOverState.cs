using GameEngine.Systems;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ゲーム終了状態
    /// </summary>
    public class GameOverState : IGameState
    {
        public string Name => "GameOver";

        public Trigger Execute(GameFlowContext context)
        {
            ConsoleRenderer.ClearScreen("GAME OVER");
            context.DisplayGameOver();
            return Trigger.Done;
        }
    }
}