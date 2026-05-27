namespace GameEngine.Interfaces
{
    /// <summary>
    /// 敵を生成するファクトリの抽象。
    /// テスト時はインメモリ実装やモックに差し替え可能にするための継ぎ目（seam）。
    /// </summary>
    public interface IEnemyFactory
    {
        /// <summary>
        /// キー指定で敵を生成する。
        /// </summary>
        IEnemy Create(string key);

        /// <summary>
        /// 登録済みの敵からランダムに1体生成する。
        /// </summary>
        IEnemy CreateRandomEnemy();

        /// <summary>
        /// 利用可能な敵のキー一覧を取得する。
        /// </summary>
        IReadOnlyCollection<string> GetAvailableEnemyKeys();
    }
}
