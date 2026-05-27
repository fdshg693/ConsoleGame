using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;
using GameEngine.Systems.BattleSystem;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ステップ駆動ステートマシンの実行コンテキスト。全状態が共有する依存（プレイヤー・進行制御・描画・永続化）と、
    /// 1ステップ分の入力（<see cref="CurrentInput"/>）を保持する。状態クラス自体はステートレスに保ち、
    /// 進行中データ（敵・ターン・ショップ状態・種別）は <see cref="EventManager"/> に集約する。
    /// </summary>
    public class GameFlowContext
    {
        private readonly IRenderer _renderer;

        public IPlayer Player { get; }
        public EventManager EventManager { get; }
        public IPlayerRepository? PlayerRepository { get; }

        /// <summary>マシンが現在のステップ実行前に設定する入力。状態はここから行動を読む。</summary>
        public PlayerInput CurrentInput { get; set; } = PlayerInput.None;

        public GameFlowContext(
            IPlayer player,
            EventManager eventManager,
            IPlayerRepository? playerRepository,
            IRenderer renderer)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            EventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            PlayerRepository = playerRepository;
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>状態が描画に使う出力シンク。</summary>
        public IRenderer Renderer => _renderer;

        public bool IsPlayerAlive => Player.IsAlive;

        /// <summary>直近に決定したエンカウント種別（ショップ/戦闘）。</summary>
        public GameEventType CurrentEventType => EventManager.CurrentEventType;

        public PlayerState CurrentPlayerState => Player.ToPlayerState();
        public BattleState? CurrentBattleState => EventManager.CurrentBattleResult?.Battle;
        public EnemyState? CurrentEnemyState => EventManager.CurrentBattleResult?.Enemy;
        public ShopState? CurrentShopState => EventManager.CurrentShopState;

        public void ShowPlayerInfo() => Player.ShowInfo();

        public void RenderMessages(IEnumerable<GameMessage> messages) => _renderer.RenderMessages(messages);

        public void ClearScreen(string title) => _renderer.ClearScreen(title);

        public void WriteLine(string text) => _renderer.WriteInfo(text);

        public void LogTransition(string fromState, string? toState)
        {
            var next = string.IsNullOrWhiteSpace(toState) ? "End" : toState;
            _renderer.WriteSystem($"[State] {fromState} -> {next}");
        }

        /// <summary>
        /// ゲームデータを保存する（同期ラッパー）。リポジトリ未登録時は「利用不可」を通知して続行する。
        /// </summary>
        public void SaveGame() => SaveGameAsync().GetAwaiter().GetResult();

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
        /// ゲームオーバー画面を表示する。
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
            RenderMessages(EventManager.GameRecord.GetRecordMessages());
        }
    }
}
