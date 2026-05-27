using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ステップ駆動ステートマシンの1状態を表すインターフェース。
    /// </summary>
    public interface IGameState
    {
        string Name { get; }

        /// <summary>
        /// この状態が次の <c>Step</c> で必要とする入力種別。
        /// <see cref="ExpectedInput.None"/> の状態はマシンが自動前進する（入力を待たない）。
        /// </summary>
        ExpectedInput ExpectedInput { get; }

        /// <summary>
        /// 入力取得の直前に呼ばれ、入力前に見せる画面を描画する（HP パネル等）。
        /// 既定は何もしない。入力不要（<see cref="ExpectedInput.None"/>）の状態では呼ばれない。
        /// </summary>
        void Prepare(GameFlowContext context) { }

        /// <summary>
        /// 状態の処理を実行し、トリガーを返す。
        /// 入力を要する状態は <see cref="GameFlowContext.CurrentInput"/> から行動を読み取る。
        /// 遷移マップによって次の状態が決定される。
        /// </summary>
        Trigger Execute(GameFlowContext context);
    }
}
