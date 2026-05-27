using GameEngine.Interfaces;
using GameEngine.Models;
using GameEngine.Systems.StateMachine;

namespace GameEngine.Systems
{
    /// <summary>
    /// ゲームのメインループと全体進行を管理するクラス
    /// </summary>
    public class GameSystem : IDisposable
    {
        private readonly IPlayer _player;
        private readonly EventManager _eventManager;
        private readonly IGameInput _input;
        private readonly IRenderer _renderer;
        private readonly IGameMessageBus _bus;
        private readonly IPlayerRepository? _playerRepository;
        private bool _disposed;

        public GameSystem(IPlayer player, IGameInput input, EventManager eventManager, IRenderer renderer, IGameMessageBus bus, IPlayerRepository? playerRepository = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _playerRepository = playerRepository;

            // 注入されたバスの発行を注入された出力シンク（IRenderer）へ流す（旧: 静的バスへの固定購読）。
            _bus.MessagePublished += OnMessagePublished;
        }

        /// <summary>
        /// ランダムイベントを発生させる（後方互換性のため残されているメソッド）
        /// </summary>
        public void Encounter(IPlayer player)
        {
            var result = _eventManager.TriggerRandomEvent();
            _renderer.RenderMessages(result.Messages);
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
                _renderer);

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
            _renderer.RenderMessage(message);
        }

        /// <summary>
        /// 注入されたメッセージバスの購読を解除する。
        /// 複数インスタンス生成時の重複購読・テスト間のイベント漏れを防ぐ。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _bus.MessagePublished -= OnMessagePublished;
            _disposed = true;
        }
    }
}
