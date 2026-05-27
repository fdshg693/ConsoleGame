using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 探索（エンカウント開始）状態。入力不要で、<see cref="EventManager.BeginEncounter"/> により
    /// 種別を決定する。次状態（戦闘/ショップ）は遷移マップが <see cref="GameFlowContext.CurrentEventType"/>
    /// を見て分岐する。
    /// </summary>
    public class ExploreState : IGameState
    {
        public string Name => "Explore";
        public ExpectedInput ExpectedInput => ExpectedInput.None;

        public Trigger Execute(GameFlowContext context)
        {
            context.ClearScreen("NEW ENCOUNTER");

            var start = context.EventManager.BeginEncounter();
            context.RenderMessages(start.Messages);

            // 戦闘開始に失敗した場合はゲームオーバーへ退避する
            if (start.IsBattle && start.Battle != null && start.Battle.IsError)
            {
                return Trigger.EndGame;
            }

            return Trigger.Continue;
        }
    }
}
