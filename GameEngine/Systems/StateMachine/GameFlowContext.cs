using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 状態機械の実行コンテキスト
    /// </summary>
    public class GameFlowContext
    {
        private readonly IRenderer _renderer;

        public IPlayer Player { get; }
        public EventManager EventManager { get; }
        public IGameInput Input { get; }
        public IPlayerRepository? PlayerRepository { get; }

        public GameFlowContext(
            IPlayer player,
            EventManager eventManager,
            IGameInput input,
            IPlayerRepository? playerRepository,
            IRenderer renderer)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            EventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            PlayerRepository = playerRepository;
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public bool IsPlayerAlive => Player.IsAlive;

        public void ShowPlayerInfo()
        {
            Player.ShowInfo();
        }

        public void RenderMessages(IEnumerable<GameMessage> messages)
        {
            _renderer.RenderMessages(messages);
        }

        /// <summary>画面をクリアする（各 State から利用）。</summary>
        public void ClearScreen(string title)
        {
            _renderer.ClearScreen(title);
        }

        public EventResult TriggerRandomEvent()
        {
            return EventManager.TriggerRandomEvent();
        }

        public void WriteLine(string text)
        {
            _renderer.WriteInfo(text);
        }

        public void LogTransition(string fromState, string? toState)
        {
            var next = string.IsNullOrWhiteSpace(toState) ? "End" : toState;
            _renderer.WriteSystem($"[State] {fromState} -> {next}");
        }

        /// <summary>
        /// 続行確認（継続・停止・一時保存）
        /// </summary>
        public bool ConfirmContinue()
        {
            GameActionChoice action = Input.SelectGameAction();

            switch (action)
            {
                case GameActionChoice.Continue:
                    return true;

                case GameActionChoice.SaveAndContinue:
                    SaveGameAsync().Wait();
                    return true;

                case GameActionChoice.SaveAndQuit:
                    SaveGameAsync().Wait();
                    return false;

                case GameActionChoice.Quit:
                    _renderer.WriteInfo("\nExiting the game.");
                    return false;

                default:
                    _renderer.WriteWarning("\nInvalid selection. Continuing the game.");
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
                _renderer.WriteError("\nSave feature is unavailable.");
                _renderer.WriteInfo("  Please check the MongoDB connection.");
                return;
            }

            try
            {
                bool success = await PlayerRepository.SaveAsync(Player, "auto_save");

                if (success)
                {
                    _renderer.WriteSuccess("Game data saved successfully.");
                }
            }
            catch (Exception ex)
            {
                _renderer.WriteError($"\nFailed to save: {ex.Message}");
            }
        }

        /// <summary>
        /// ゲームオーバー画面を表示
        /// </summary>
        public void DisplayGameOver()
        {
            string title = Player.IsAlive ? "Thank you for playing!" : "GAME OVER";
            bool isVictory = Player.IsAlive;

            _renderer.WriteResultBox(title, new[]
            {
                $"Gold Earned: {Player.ReturnTotalGold()}",
                $"Potions Remaining: {Player.ReturnTotalPotions()}"
            }, isVictory);

            Player.ShowInfo();
            RenderMessages(GameRecord.GetRecordMessages());
        }
    }
}