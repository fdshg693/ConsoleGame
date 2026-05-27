using Xunit;
using GameEngine.Configuration;
using GameEngine.Factory;

namespace GameEngine.Tests.Factory
{
    /// <summary>
    /// EnemyFactoryのテスト
    /// </summary>
    public class EnemyFactoryTests
    {
        // 設定と乱数源を注入してインスタンス化する（静的依存を排除した seam）。
        // 乱数はシード固定で決定的にし、ゴールド計算のゼロ除算も避ける。
        private static EnemyFactory CreateFactory() =>
            new EnemyFactory(
                new EnemyConfig { GoldBaseMultiplier = 2, GoldRandomMin = 1, GoldRandomMax = 10 },
                new Random(12345));

        [Fact]
        public void GetAvailableEnemyKeys_IncludesGoblin()
        {
            // Arrange
            var factory = CreateFactory();

            // Act
            var keys = factory.GetAvailableEnemyKeys();

            // Assert
            Assert.Contains("Goblin", keys);
        }

        [Fact]
        public void Create_Goblin_ReturnsEnemyWithExpectedStrategy()
        {
            // Arrange
            var factory = CreateFactory();

            // Act
            var enemy = factory.Create("Goblin");

            // Assert
            Assert.Equal("Goblin", enemy.Name);
            Assert.Equal(30, enemy.MaxHP);
            Assert.Equal("Melee", enemy.AttackStrategy.GetAttackStrategyName());
        }
    }
}