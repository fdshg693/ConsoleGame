using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ゲーム開始状態（入力不要・自動前進）。
    /// </summary>
    public class StartState : IGameState
    {
        public string Name => "Start";
        public ExpectedInput ExpectedInput => ExpectedInput.None;

        public Trigger Execute(GameFlowContext context)
        {
            context.ClearScreen("GAME START");
            context.ShowPlayerInfo();
            return Trigger.Continue;
        }
    }
}
