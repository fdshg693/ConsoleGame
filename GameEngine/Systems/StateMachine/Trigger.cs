namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ステートが返すトリガー。遷移マップで次ステートに解決される
    /// </summary>
    public enum Trigger
    {
        /// <summary>通常の次ステートへ進む</summary>
        Continue,

        /// <summary>ゲーム終了（死亡 or プレイヤーの選択）</summary>
        EndGame,

        /// <summary>終端ステートの処理完了（マシン停止）</summary>
        Done
    }
}
