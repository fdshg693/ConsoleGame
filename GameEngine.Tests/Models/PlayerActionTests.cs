using Xunit;
using GameEngine.DTOs;
using System;

namespace GameEngine.Tests.Models
{
    /// <summary>
    /// PlayerAction関連クラスのテスト
    /// </summary>
    public class PlayerActionTests
    {
        [Fact]
        public void AttackAction_Constructor_SetsTypeAndStrategy()
        {
            // Arrange & Act
            var action = new AttackAction("Melee");

            // Assert
            Assert.Equal(ActionType.Attack, action.Type);
            Assert.Equal("Melee", action.StrategyName);
        }

        [Fact]
        public void AttackAction_DefaultConstructor_SetsType()
        {
            // Arrange & Act
            var action = new AttackAction();

            // Assert
            Assert.Equal(ActionType.Attack, action.Type);
            Assert.Equal(string.Empty, action.StrategyName);
        }

        [Fact]
        public void UseItemAction_Constructor_SetsProperties()
        {
            // Arrange & Act
            var action = new UseItemAction("Potion", 3);

            // Assert
            Assert.Equal(ActionType.UseItem, action.Type);
            Assert.Equal("Potion", action.ItemName);
            Assert.Equal(3, action.Quantity);
        }

        [Fact]
        public void ShopAction_BuyPotion_SetsProperties()
        {
            // Arrange & Act
            var action = new ShopAction(ShopActionType.BuyPotion, quantity: 5);

            // Assert
            Assert.Equal(ActionType.Shop, action.Type);
            Assert.Equal(ShopActionType.BuyPotion, action.ShopType);
            Assert.Equal(5, action.Quantity);
        }

        [Fact]
        public void ShopAction_BuyWeapon_SetsWeaponName()
        {
            // Arrange & Act
            var action = new ShopAction(ShopActionType.BuyWeapon, "Sword", 1);

            // Assert
            Assert.Equal(ActionType.Shop, action.Type);
            Assert.Equal(ShopActionType.BuyWeapon, action.ShopType);
            Assert.Equal("Sword", action.ItemName);
        }

        [Theory]
        [InlineData(ActionType.Continue)]
        [InlineData(ActionType.Quit)]
        [InlineData(ActionType.Save)]
        [InlineData(ActionType.Load)]
        public void GameControlAction_ValidActionType_DoesNotThrow(ActionType actionType)
        {
            // Arrange & Act
            var action = new GameControlAction(actionType);

            // Assert
            Assert.Equal(actionType, action.Type);
        }

        [Theory]
        [InlineData(ActionType.Attack)]
        [InlineData(ActionType.UseItem)]
        [InlineData(ActionType.Shop)]
        [InlineData(ActionType.Rest)]
        public void GameControlAction_InvalidActionType_ThrowsArgumentException(ActionType actionType)
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new GameControlAction(actionType));
            Assert.Contains("Invalid action type", exception.Message);
        }

        [Fact]
        public void RestAction_Constructor_SetsType()
        {
            // Arrange & Act
            var action = new RestAction();

            // Assert
            Assert.Equal(ActionType.Rest, action.Type);
        }
    }

    /// <summary>
    /// PlayerActionValidatorのテスト
    /// </summary>
    public class PlayerActionValidatorTests
    {
        [Theory]
        [InlineData("Default")]
        [InlineData("Melee")]
        [InlineData("Magic")]
        [InlineData("default")] // 大文字小文字を区別しない
        [InlineData("MELEE")]
        public void IsValid_AttackAction_ValidStrategy_ReturnsTrue(string strategyName)
        {
            // Arrange
            var action = new AttackAction(strategyName);

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void IsValid_AttackAction_EmptyStrategy_ReturnsFalse(string? strategyName)
        {
            // Arrange
            var action = new AttackAction { StrategyName = strategyName ?? string.Empty };

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("cannot be empty", errorMessage);
        }

        [Fact]
        public void IsValid_AttackAction_InvalidStrategy_ReturnsFalse()
        {
            // Arrange
            var action = new AttackAction("InvalidStrategy");

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid strategy name", errorMessage);
        }

        [Fact]
        public void IsValid_UseItemAction_ValidItem_ReturnsTrue()
        {
            // Arrange
            var action = new UseItemAction("Potion", 2);

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void IsValid_UseItemAction_EmptyItemName_ReturnsFalse(string? itemName)
        {
            // Arrange
            var action = new UseItemAction { ItemName = itemName ?? string.Empty, Quantity = 1 };

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("cannot be empty", errorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IsValid_UseItemAction_InvalidQuantity_ReturnsFalse(int quantity)
        {
            // Arrange
            var action = new UseItemAction("Potion", quantity);

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("must be greater than 0", errorMessage);
        }

        [Fact]
        public void IsValid_ShopAction_BuyWeapon_ValidData_ReturnsTrue()
        {
            // Arrange
            var action = new ShopAction(ShopActionType.BuyWeapon, "Sword", 1);

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void IsValid_ShopAction_BuyWeapon_NoWeaponName_ReturnsFalse()
        {
            // Arrange
            var action = new ShopAction(ShopActionType.BuyWeapon, null, 1);

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Weapon name is required", errorMessage);
        }

        [Fact]
        public void IsValid_ShopAction_Exit_ReturnsTrue()
        {
            // Arrange
            var action = new ShopAction(ShopActionType.Exit);

            // Act
            var isValid = PlayerActionValidator.IsValid(action, out var errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
    }
}
