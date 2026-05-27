using GameEngine.Interfaces;

namespace GameEngine.Tests.TestDoubles
{
    /// <summary>
    /// 指定した敵を決定的に返す <see cref="IEnemyFactory"/> 実装。
    /// 戦闘ステップのテストで、敵の HP・火力を固定するための継ぎ目（seam）。
    /// </summary>
    public sealed class FakeEnemyFactory : IEnemyFactory
    {
        private readonly Func<IEnemy> _factory;

        public FakeEnemyFactory(Func<IEnemy> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IEnemy Create(string key) => _factory();

        public IEnemy CreateRandomEnemy() => _factory();

        public IReadOnlyCollection<string> GetAvailableEnemyKeys() => new[] { "Test" };
    }
}
