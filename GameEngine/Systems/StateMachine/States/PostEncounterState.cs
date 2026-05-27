namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// エンカウント後の確認状態
    /// </summary>
    public class PostEncounterState : IGameState
    {
        public string Name => "PostEncounter";

        public Trigger Execute(GameFlowContext context)
        {
            context.ClearScreen("ENCOUNTER RESULTS");
            context.ShowPlayerInfo();

            if (!context.ConfirmContinue())
            {
                context.WriteLine("\nGame ended by player choice.");
                return Trigger.EndGame;
            }

            return Trigger.Continue;
        }
    }
}