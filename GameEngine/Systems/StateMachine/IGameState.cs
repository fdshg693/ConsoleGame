namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ゲームの状態を表すインターフェース
    /// </summary>
    public interface IGameState
    {
        string Name { get; }

        /// <summary>
        /// 状態の処理を実行し、トリガーを返す。
        /// 遷移マップによって次の状態が決定される。
        /// </summary>
        Trigger Execute(GameFlowContext context);
    }
}