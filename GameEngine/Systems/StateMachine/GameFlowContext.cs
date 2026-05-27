using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 状態機械の実行コンテキスト
    /// </summary>
    public class GameFlowContext
    {
        private readonly Action<IEnumerable<GameMessage>> _renderMessages;

        public IPlayer Player { get; }
        public EventManager EventManager { get; }
        public IGameInput Input { get; }
        public IPlayerRepository? PlayerRepository { get; }

        public GameFlowContext(
            IPlayer player,
            EventManager eventManager,
            IGameInput input,
            IPlayerRepository? playerRepository,
            Action<IEnumerable<GameMessage>> renderMessages)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            EventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            PlayerRepository = playerRepository;
            _renderMessages = renderMessages ?? throw new ArgumentNullException(nameof(renderMessages));
        }

        public bool IsPlayerAlive => Player.IsAlive;

        public void ShowPlayerInfo()
        {
            Player.ShowInfo();
        }

        public void RenderMessages(IEnumerable<GameMessage> messages)
        {
            _renderMessages(messages);
        }

        public EventResult TriggerRandomEvent()
        {
            return EventManager.TriggerRandomEvent();
        }

        public void WriteLine(string text)
        {
            ConsoleRenderer.WriteInfo(text);
        }

        public void LogTransition(string fromState, string? toState)
        {
            var next = string.IsNullOrWhiteSpace(toState) ? "End" : toState;
            ConsoleRenderer.WriteSystem($"[State] {fromState} -> {next}");
        }

        /// <summary>
        /// 続行確認（継続・停止・一時保存）
        /// </summary>
        public bool ConfirmContinue()
        {
            string action = UserInteraction.SelectGameAction();

            switch (action)
            {
                case "continue":
                    return true;

                case "save_continue":
                    SaveGameAsync().Wait();
                    return true;

                case "save_quit":
                    SaveGameAsync().Wait();
                    return false;

                case "quit":
                    ConsoleRenderer.WriteInfo("\nExiting the game.");
                    return false;

                default:
                    ConsoleRenderer.WriteWarning("\nInvalid selection. Continuing the game.");
                    return true;
            }
        }

        /// <summary>
        /// ゲームデータを保存する
        /// </summary>
        private async Task SaveGameAsync()
        {
            if (PlayerRepository == null)
            {
                ConsoleRenderer.WriteError("\nSave feature is unavailable.");
                ConsoleRenderer.WriteInfo("  Please check the MongoDB connection.");
                return;
            }

            try
            {
                bool success = await PlayerRepository.SaveAsync(Player, "auto_save");

                if (success)
                {
                    ConsoleRenderer.WriteSuccess("Game data saved successfully.");
                }
            }
            catch (Exception ex)
            {
                ConsoleRenderer.WriteError($"\nFailed to save: {ex.Message}");
            }
        }

        /// <summary>
        /// ゲームオーバー画面を表示
        /// </summary>
        public void DisplayGameOver()
        {
            string title = Player.IsAlive ? "Thank you for playing!" : "GAME OVER";
            bool isVictory = Player.IsAlive;

            ConsoleRenderer.WriteResultBox(title, new[]
            {
                $"Gold Earned: {Player.ReturnTotalGold()}",
                $"Potions Remaining: {Player.ReturnTotalPotions()}"
            }, isVictory);

            Player.ShowInfo();
            RenderMessages(GameRecord.GetRecordMessages());
        }
    }
}