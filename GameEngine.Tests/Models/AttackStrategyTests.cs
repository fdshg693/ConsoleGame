using Xunit;
using GameEngine.Models;

namespace GameEngine.Tests.Models
{
    /// <summary>
    /// AttackStrategyのテスト
    /// </summary>
    public class AttackStrategyTests
    {
        [Theory]
        [InlineData("Default", "Default")]
        [InlineData("Melee", "Melee")]
        [InlineData("Magic", "Magic")]
        public void GetAttackStrategy_ValidType_ReturnsMatchingStrategy(string input, string expectedName)
        {
            // Act
            var strategy = AttackStrategy.GetAttackStrategy(input);

            // Assert
            Assert.Equal(expectedName, strategy.GetAttackStrategyName());
        }

        [Fact]
        public void GetAttackStrategy_UnknownType_ReturnsDefaultStrategy()
        {
            // Act
            var strategy = AttackStrategy.GetAttackStrategy("Unknown");

            // Assert
            Assert.Equal("Default", strategy.GetAttackStrategyName());
        }
    }
}