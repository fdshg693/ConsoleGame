using GameEngine.Interfaces;
using GameEngine.Models;
using GameEngine.Systems.StateMachine;

namespace GameEngine.Systems
{
    /// <summary>
    /// ゲームのメインループと全体進行を管理するクラス
    /// </summary>
    public class GameSystem
    {
        private readonly IPlayer _player;
        private readonly EventManager _eventManager;
        private readonly IGameInput _input;
        private readonly IPlayerRepository? _playerRepository;

        public GameSystem(IPlayer player, IGameInput input, IPlayerRepository? playerRepository = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _playerRepository = playerRepository;
            _eventManager = new EventManager(_player, _input);

            GameMessageBus.MessagePublished += OnMessagePublished;
        }

        /// <summary>
        /// ランダムイベントを発生させる（後方互換性のため残されているメソッド）
        /// </summary>
        public void Encounter(IPlayer player)
        {
            var result = _eventManager.TriggerRandomEvent();
            RenderMessages(result.Messages);
        }

        /// <summary>
        /// ゲームのメインループを実行する
        /// </summary>
        public void RunGameLoop()
        {
            var context = new GameFlowContext(
                _player,
                _eventManager,
                _input,
                _playerRepository,
                RenderMessages);

            // 遷移マップ: (現在のステート, トリガー) → 次のステート
            var transitions = new Dictionary<(Type, Trigger), Func<IGameState>?>
            {
                { (typeof(StartState),         Trigger.Continue), () => new EncounterState() },
                { (typeof(EncounterState),     Trigger.Continue), () => new PostEncounterState() },
                { (typeof(EncounterState),     Trigger.EndGame),  () => new GameOverState() },
                { (typeof(PostEncounterState), Trigger.Continue), () => new EncounterState() },
                { (typeof(PostEncounterState), Trigger.EndGame),  () => new GameOverState() },
                { (typeof(GameOverState),      Trigger.Done),     null },
            };

            var stateMachine = new GameStateMachine(new StartState(), context, transitions);
            stateMachine.Run();
        }

        private void OnMessagePublished(GameMessage message)
        {
            ConsoleRenderer.RenderMessage(message);
        }

        private static void RenderMessages(IEnumerable<GameMessage> messages)
        {
            ConsoleRenderer.RenderMessages(messages);
        }
    }
}
