using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 遷移マップに基づくステップ駆動の状態機械。
    /// 内部に while ループを持たず、<see cref="Start"/> で開始し <see cref="Step"/> を外部から
    /// 繰り返し呼ぶことで進行する。入力不要（<see cref="ExpectedInput.None"/>）の状態は
    /// 自動で連続実行し、次に入力を要する状態か終端まで前進する。
    /// </summary>
    public class GameStateMachine
    {
        private IGameState? _currentState;
        private readonly GameFlowContext _context;
        private readonly Dictionary<(Type State, Trigger Trigger), Func<GameFlowContext, IGameState>?> _transitions;

        public GameStateMachine(
            IGameState initialState,
            GameFlowContext context,
            Dictionary<(Type, Trigger), Func<GameFlowContext, IGameState>?> transitions)
        {
            _currentState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
        }

        /// <summary>現在の状態（終了後は null）。</summary>
        public IGameState? CurrentState => _currentState;

        /// <summary>現在の状態名（終了後は null）。</summary>
        public string? CurrentStateName => _currentState?.Name;

        /// <summary>ゲームが進行中か（現在状態が存在するか）。</summary>
        public bool IsRunning => _currentState != null;

        /// <summary>次の <see cref="Step"/> で必要な入力種別。</summary>
        public ExpectedInput ExpectedInput => _currentState?.ExpectedInput ?? ExpectedInput.None;

        /// <summary>
        /// マシンを開始する。初期状態から入力不要な状態を自動前進させ、
        /// 最初に入力を要する状態（または終端）まで進めて停止する。
        /// </summary>
        public void Start()
        {
            AdvanceAutoStatesAndPrepare();
        }

        /// <summary>
        /// 1ステップ進める。現在の入力待ち状態に <paramref name="input"/> を適用して実行し、
        /// 続く入力不要状態を自動前進させて次の入力待ち状態（または終端）まで進める。
        /// </summary>
        public void Step(PlayerInput input)
        {
            if (_currentState == null)
            {
                return;
            }

            _context.CurrentInput = input ?? PlayerInput.None;
            ExecuteCurrent();
            AdvanceAutoStatesAndPrepare();
        }

        /// <summary>入力不要状態を連続実行し、停止先の入力待ち状態に対して <c>Prepare</c> を呼ぶ。</summary>
        private void AdvanceAutoStatesAndPrepare()
        {
            while (_currentState != null && _currentState.ExpectedInput == ExpectedInput.None)
            {
                _context.CurrentInput = PlayerInput.None;
                ExecuteCurrent();
            }

            _currentState?.Prepare(_context);
        }

        /// <summary>現在状態を1回実行し、トリガーから次状態を解決する。</summary>
        private void ExecuteCurrent()
        {
            var current = _currentState!;
            var trigger = current.Execute(_context);
            var next = ResolveNextState(current, trigger);
            _context.LogTransition(current.Name, next?.Name);
            _currentState = next;
        }

        private IGameState? ResolveNextState(IGameState current, Trigger trigger)
        {
            var key = (current.GetType(), trigger);

            if (!_transitions.TryGetValue(key, out var factory))
            {
                throw new InvalidOperationException(
                    $"Undefined transition: {current.Name} + {trigger}");
            }

            return factory?.Invoke(_context);
        }
    }
}
