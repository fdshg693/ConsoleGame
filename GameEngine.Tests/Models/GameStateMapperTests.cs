using Xunit;
using GameEngine.DTOs;
using GameEngine.Mappers;
using GameEngine.Models;
using GameEngine.Interfaces;
using System;

namespace GameEngine.Tests.Models
{
    /// <summary>
    /// GameStateMapperのテスト
    /// </summary>
    public class GameStateMapperTests
    {
        [Fact]
        public void CreateEmptyGameState_ReturnsValidGameState()
        {
            // Act
            var gameState = GameStateMapper.CreateEmptyGameState();

            // Assert
            Assert.NotNull(gameState);
            Assert.NotNull(gameState.Player);
            Assert.Null(gameState.CurrentEnemy);
            Assert.Null(gameState.CurrentBattle);
            Assert.Null(gameState.CurrentShop);
            Assert.NotNull(gameState.Messages);
            Assert.Empty(gameState.Messages);
            Assert.Equal(GamePhase.Initialization, gameState.Phase);
            Assert.False(gameState.IsGameOver);
        }

        [Fact]
        public void CreateInitialBattleState_ReturnsValidBattleState()
        {
            // Act
            var battleState = GameStateMapper.CreateInitialBattleState();

            // Assert
            Assert.NotNull(battleState);
            Assert.Equal(0, battleState.TurnNumber);
            Assert.NotNull(battleState.AvailableStrategies);
            Assert.Equal(3, battleState.AvailableStrategies.Count);
            Assert.Contains("Default", battleState.AvailableStrategies);
            Assert.Contains("Melee", battleState.AvailableStrategies);
            Assert.Contains("Magic", battleState.AvailableStrategies);
            Assert.Null(battleState.LastPlayerAction);
            Assert.Equal(0, battleState.LastDamageDealt);
            Assert.Equal(0, battleState.LastDamageTaken);
            Assert.False(battleState.PlayerWon);
            Assert.False(battleState.BattleEnded);
        }

        [Fact]
        public void CreateInitialShopState_DefaultPrice_ReturnsValidShopState()
        {
            // Act
            var shopState = GameStateMapper.CreateInitialShopState();

            // Assert
            Assert.NotNull(shopState);
            Assert.Equal(50, shopState.PotionPrice);
            Assert.NotNull(shopState.AvailableItems);
            Assert.Single(shopState.AvailableItems);
            Assert.Equal("Potion", shopState.AvailableItems[0].Name);
            Assert.Equal(50, shopState.AvailableItems[0].Price);
            Assert.NotNull(shopState.AvailableWeapons);
            Assert.Empty(shopState.AvailableWeapons);
        }

        [Fact]
        public void CreateInitialShopState_CustomPrice_ReturnsCorrectPrice()
        {
            // Act
            var shopState = GameStateMapper.CreateInitialShopState(potionPrice: 75);

            // Assert
            Assert.Equal(75, shopState.PotionPrice);
            Assert.Equal(75, shopState.AvailableItems[0].Price);
        }

        [Fact]
        public void CreateMessage_CreatesMessageWithCorrectProperties()
        {
            // Arrange
            var beforeTime = DateTime.UtcNow;

            // Act
            var message = GameStateMapper.CreateMessage("Test message", MessageType.Info);

            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.NotNull(message);
            Assert.Equal("Test message", message.Text);
            Assert.Equal(MessageType.Info, message.Type);
            Assert.InRange(message.Timestamp, beforeTime, afterTime);
        }

        [Fact]
        public void CreateMessages_MultipleMessages_CreatesCorrectList()
        {
            // Act
            var messages = GameStateMapper.CreateMessages(
                ("Message 1", MessageType.Info),
                ("Message 2", MessageType.Combat),
                ("Message 3", MessageType.Success)
            );

            // Assert
            Assert.NotNull(messages);
            Assert.Equal(3, messages.Count);
            Assert.Equal("Message 1", messages[0].Text);
            Assert.Equal(MessageType.Info, messages[0].Type);
            Assert.Equal("Message 2", messages[1].Text);
            Assert.Equal(MessageType.Combat, messages[1].Type);
            Assert.Equal("Message 3", messages[2].Text);
            Assert.Equal(MessageType.Success, messages[2].Type);
        }

        [Fact]
        public void CreateMessages_EmptyArray_ReturnsEmptyList()
        {
            // Act
            var messages = GameStateMapper.CreateMessages();

            // Assert
            Assert.NotNull(messages);
            Assert.Empty(messages);
        }

        [Fact]
        public void ToWeaponInfo_WithDefaultPrice_ReturnsCorrectInfo()
        {
            // Arrange
            var weapon = new Weapon(100, 20, 5, "Test Sword");

            // Act
            var weaponInfo = weapon.ToWeaponInfo();

            // Assert
            Assert.NotNull(weaponInfo);
            Assert.Equal("Test Sword", weaponInfo.Name);
            Assert.Equal(20, weaponInfo.AttackPower);
            Assert.Equal(5, weaponInfo.DefensePower);
            Assert.Equal(0, weaponInfo.Price); // デフォルト価格
        }

        [Fact]
        public void ToWeaponInfo_WithCustomPrice_ReturnsCorrectInfo()
        {
            // Arrange
            var weapon = new Weapon(100, 20, 5, "Test Sword");

            // Act
            var weaponInfo = weapon.ToWeaponInfo(price: 250);

            // Assert
            Assert.NotNull(weaponInfo);
            Assert.Equal("Test Sword", weaponInfo.Name);
            Assert.Equal(20, weaponInfo.AttackPower);
            Assert.Equal(5, weaponInfo.DefensePower);
            Assert.Equal(250, weaponInfo.Price);
        }

        [Fact]
        public void ToWeaponInfo_NullWeapon_ThrowsArgumentNullException()
        {
            // Arrange
            IWeapon? weapon = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => weapon!.ToWeaponInfo());
        }
    }
}
