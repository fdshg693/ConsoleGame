using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;
using GameEngine.Systems.StateMachine;

namespace GameEngine.Systems
{
    /// <summary>
    /// ゲーム全体の進行を司るステップ駆動エンジン。内部にブロッキングループを持たず、
    /// <see cref="Start"/> / <see cref="Step"/> で1行動ずつ外部から駆動できる。
    /// コンソールホストは <see cref="RunGameLoop"/>（薄い駆動ループ）から、API ホストは
    /// <see cref="Step"/> をリクエスト単位で呼び出す。
    /// </summary>
    public class GameSystem : IDisposable
    {
        private readonly IPlayer _player;
        private readonly EventManager _eventManager;
        private readonly IGameInput _input;
        private readonly IRenderer _renderer;
        private readonly IGameMessageBus _bus;
        private readonly IPlayerRepository? _playerRepository;

        private GameStateMachine? _machine;
        private bool _disposed;

        public GameSystem(IPlayer player, IGameInput input, EventManager eventManager, IRenderer renderer, IGameMessageBus bus, IPlayerRepository? playerRepository = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _playerRepository = playerRepository;

            // 注入されたバスの発行を注入された出力シンク（IRenderer）へ流す。
            _bus.MessagePublished += OnMessagePublished;
        }

        // ─────────────────────────────────────────────
        // ステップ駆動 API（API ホスト・テスト・薄い駆動ループ共通）
        // ─────────────────────────────────────────────

        /// <summary>ゲームが進行中か（状態機械が稼働中か）。</summary>
        public bool IsRunning => _machine?.IsRunning ?? false;

        /// <summary>次の <see cref="Step"/> で必要な入力種別。</summary>
        public ExpectedInput ExpectedInput => _machine?.ExpectedInput ?? ExpectedInput.None;

        /// <summary>現在の状態名（探索/戦闘/ショップ/休憩/続行確認/終了）。終了後は null。</summary>
        public string? CurrentStateName => _machine?.CurrentStateName;

        public PlayerState CurrentPlayerState => _player.ToPlayerState();
        public BattleState? CurrentBattleState => _eventManager.CurrentBattleResult?.Battle;
        public EnemyState? CurrentEnemyState => _eventManager.CurrentBattleResult?.Enemy;
        public ShopState? CurrentShopState => _eventManager.CurrentShopState;

        /// <summary>
        /// 状態機械を生成して開始し、最初に入力を要する状態（または終端）まで前進させる。
        /// </summary>
        public void Start()
        {
            var context = new GameFlowContext(_player, _eventManager, _playerRepository, _renderer);
            _machine = new GameStateMachine(new StartState(), context, BuildTransitions());
            _machine.Start();
        }

        /// <summary>
        /// 1行動を適用して1ステップ進める。<see cref="Start"/> 未呼び出し時は何もしない。
        /// </summary>
        public void Step(PlayerInput input)
        {
            _machine?.Step(input);
        }

        // ─────────────────────────────────────────────
        // コンソール向けの薄い駆動ループ
        // ─────────────────────────────────────────────

        /// <summary>
        /// ゲームのメインループを実行する（コンソールホスト用）。
        /// <see cref="ExpectedInput"/> に応じて <see cref="IGameInput"/> から行動を取得し、
        /// <see cref="Step"/> に渡すだけの薄いアダプタ。進行順序の制御はコアの State 群が持つ。
        /// </summary>
        public void RunGameLoop()
        {
            Start();

            while (IsRunning)
            {
                var input = ExpectedInput switch
                {
                    ExpectedInput.Attack => PlayerInput.ForAttack(
                        _input.SelectAttackAction(CurrentBattleState!, CurrentPlayerState, CurrentEnemyState!)),
                    ExpectedInput.Shop => PlayerInput.ForShop(
                        _input.SelectShopAction(CurrentShopState!, CurrentPlayerState)),
                    ExpectedInput.Rest => PlayerInput.ForRest(
                        _input.SelectRestAction(CurrentPlayerState)),
                    ExpectedInput.GameAction => PlayerInput.ForProgress(
                        _input.SelectGameAction()),
                    _ => PlayerInput.None
                };

                Step(input);
            }
        }

        /// <summary>
        /// 遷移マップ: (現在のステート, トリガー) → 次のステートを生成するファクトリ（context 参照可）。
        /// </summary>
        private static Dictionary<(Type, Trigger), Func<GameFlowContext, IGameState>?> BuildTransitions()
        {
            return new Dictionary<(Type, Trigger), Func<GameFlowContext, IGameState>?>
            {
                { (typeof(StartState),         Trigger.Continue), _   => new ExploreState() },

                // 探索 → 種別に応じて戦闘 or ショップへ分岐
                { (typeof(ExploreState),       Trigger.Continue), ctx => ctx.CurrentEventType == GameEventType.Shop
                                                                            ? new ShoppingState()
                                                                            : (IGameState)new BattleTurnState() },
                { (typeof(ExploreState),       Trigger.EndGame),  _   => new GameOverState() },

                // 戦闘: 継続=自己ループ / 勝利=休憩 / 敗北=ゲームオーバー
                { (typeof(BattleTurnState),    Trigger.Repeat),   _   => new BattleTurnState() },
                { (typeof(BattleTurnState),    Trigger.Continue), _   => new RestState() },
                { (typeof(BattleTurnState),    Trigger.EndGame),  _   => new GameOverState() },

                // ショップ: 継続=自己ループ / 退店=休憩
                { (typeof(ShoppingState),      Trigger.Repeat),   _   => new ShoppingState() },
                { (typeof(ShoppingState),      Trigger.Continue), _   => new RestState() },

                // 休憩 → 続行確認
                { (typeof(RestState),          Trigger.Continue), _   => new PostEncounterState() },

                // 続行確認: 続行=探索へ / 終了=ゲームオーバー
                { (typeof(PostEncounterState), Trigger.Continue), _   => new ExploreState() },
                { (typeof(PostEncounterState), Trigger.EndGame),  _   => new GameOverState() },

                // 終端
                { (typeof(GameOverState),      Trigger.Done),     null },
            };
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
