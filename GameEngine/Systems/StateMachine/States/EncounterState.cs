namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// エンカウント（イベント）状態
    /// </summary>
    public class EncounterState : IGameState
    {
        public string Name => "Encounter";

        public Trigger Execute(GameFlowContext context)
        {
            context.ClearScreen("NEW ENCOUNTER");

            var eventResult = context.TriggerRandomEvent();
            context.RenderMessages(eventResult.Messages);

            if (!eventResult.ContinueGame || !context.IsPlayerAlive)
            {
                return Trigger.EndGame;
            }

            return Trigger.Continue;
        }
    }
}