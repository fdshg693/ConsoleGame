namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 遷移マップに基づく状態機械。
    /// 各ステートが返す Trigger を遷移マップで解決し、次のステートを決定する。
    /// </summary>
    public class GameStateMachine
    {
        private IGameState? _currentState;
        private readonly GameFlowContext _context;
        private readonly Dictionary<(Type State, Trigger Trigger), Func<IGameState>?> _transitions;

        public GameStateMachine(
            IGameState initialState,
            GameFlowContext context,
            Dictionary<(Type, Trigger), Func<IGameState>?> transitions)
        {
            _currentState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
        }

        public void Run()
        {
            while (_currentState != null)
            {
                var current = _currentState;
                var trigger = current.Execute(_context);

                _currentState = ResolveNextState(current, trigger);
                _context.LogTransition(current.Name, _currentState?.Name);
            }
        }

        private IGameState? ResolveNextState(IGameState current, Trigger trigger)
        {
            var key = (current.GetType(), trigger);

            if (!_transitions.TryGetValue(key, out var factory))
            {
                throw new InvalidOperationException(
                    $"Undefined transition: {current.Name} + {trigger}");
            }

            return factory?.Invoke();
        }
    }
}
