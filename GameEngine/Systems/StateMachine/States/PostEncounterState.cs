using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// エンカウント後の続行確認状態。続行/セーブ/終了の進行選択（<see cref="GameActionChoice"/>）を1つ受け取る。
    /// </summary>
    public class PostEncounterState : IGameState
    {
        public string Name => "PostEncounter";
        public ExpectedInput ExpectedInput => ExpectedInput.GameAction;

        /// <summary>選択前にプレイヤー情報を提示する。</summary>
        public void Prepare(GameFlowContext context)
        {
            context.ClearScreen("ENCOUNTER RESULTS");
            context.ShowPlayerInfo();
        }

        public Trigger Execute(GameFlowContext context)
        {
            var choice = context.CurrentInput.Progress ?? GameActionChoice.Continue;

            switch (choice)
            {
                case GameActionChoice.Continue:
                    return Trigger.Continue;

                case GameActionChoice.SaveAndContinue:
                    context.SaveGame();
                    return Trigger.Continue;

                case GameActionChoice.SaveAndQuit:
                    context.SaveGame();
                    return Trigger.EndGame;

                case GameActionChoice.Quit:
                    context.WriteLine("\nExiting the game.");
                    return Trigger.EndGame;

                default:
                    context.Renderer.WriteWarning("\nInvalid selection. Continuing the game.");
                    return Trigger.Continue;
            }
        }
    }
}
