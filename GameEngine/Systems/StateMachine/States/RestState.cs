using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 休憩状態。エンカウント後に回復アイテム使用の機会を1アクション提供する
    /// （入力が null の場合はスキップ）。処理後は続行確認（<see cref="PostEncounterState"/>）へ遷移する。
    /// 休憩画面の描画はホスト側 <c>IGameInput.SelectRestAction</c> が担う。
    /// </summary>
    public class RestState : IGameState
    {
        public string Name => "Rest";
        public ExpectedInput ExpectedInput => ExpectedInput.Rest;

        public Trigger Execute(GameFlowContext context)
        {
            var messages = context.EventManager.SubmitRestAction(context.CurrentInput.Rest);
            context.RenderMessages(messages);
            return Trigger.Continue; // → PostEncounterState
        }
    }
}
