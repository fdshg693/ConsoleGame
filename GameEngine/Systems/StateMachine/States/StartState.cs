using GameEngine.Systems;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ゲーム開始状態
    /// </summary>
    public class StartState : IGameState
    {
        public string Name => "Start";

        public Trigger Execute(GameFlowContext context)
        {
            ConsoleRenderer.ClearScreen("GAME START");
            context.ShowPlayerInfo();

            return Trigger.Continue;
        }
    }
}