using Xunit;
using GameEngine.Factory;

namespace GameEngine.Tests.Factory
{
    /// <summary>
    /// EnemyFactoryのテスト
    /// </summary>
    public class EnemyFactoryTests
    {
        [Fact]
        public void GetAvailableEnemyKeys_IncludesGoblin()
        {
            // Act
            var keys = EnemyFactory.GetAvailableEnemyKeys();

            // Assert
            Assert.Contains("Goblin", keys);
        }

        [Fact]
        public void Create_Goblin_ReturnsEnemyWithExpectedStrategy()
        {
            // Act
            var enemy = EnemyFactory.Create("Goblin");

            // Assert
            Assert.Equal("Goblin", enemy.Name);
            Assert.Equal(30, enemy.MaxHP);
            Assert.Equal("Melee", enemy.AttackStrategy.GetAttackStrategyName());
        }
    }
}