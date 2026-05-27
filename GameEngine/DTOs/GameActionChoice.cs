namespace GameEngine.DTOs
{
    /// <summary>
    /// エンカウント後にプレイヤーが選択する進行アクション。
    /// コンソール/API いずれの入力実装からも返せるよう、UI 非依存の列挙型として定義する。
    /// </summary>
    public enum GameActionChoice
    {
        /// <summary>セーブせず続行する。</summary>
        Continue,

        /// <summary>セーブして続行する。</summary>
        SaveAndContinue,

        /// <summary>セーブして終了する。</summary>
        SaveAndQuit,

        /// <summary>セーブせず終了する。</summary>
        Quit
    }
}
