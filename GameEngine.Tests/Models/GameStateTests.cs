using Xunit;
using GameEngine.DTOs;
using GameEngine.Models;
using System;

namespace GameEngine.Tests.Models
{
    /// <summary>
    /// GameState関連クラスのテスト
    /// </summary>
    public class GameStateTests
    {
        [Fact]
        public void GameState_DefaultConstructor_InitializesProperties()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.NotNull(gameState.Messages);
            Assert.Empty(gameState.Messages);
            Assert.Equal(GamePhase.Initialization, gameState.Phase);
            Assert.False(gameState.IsGameOver);
        }

        [Fact]
        public void PlayerState_PropertiesCanBeSet()
        {
            // Arrange & Act
            var playerState = new PlayerState
            {
                Name = "TestHero",
                HP = 100,
                MaxHP = 150,
                Level = 5,
                Experience = 250,
                Gold = 500,
                Potions = 3,
                EquippedWeapon = "Sword",
                IsAlive = true,
                AttackPower = 25,
                DefensePower = 10
            };

            // Assert
            Assert.Equal("TestHero", playerState.Name);
            Assert.Equal(100, playerState.HP);
            Assert.Equal(150, playerState.MaxHP);
            Assert.Equal(5, playerState.Level);
            Assert.Equal(250, playerState.Experience);
            Assert.Equal(500, playerState.Gold);
            Assert.Equal(3, playerState.Potions);
            Assert.Equal("Sword", playerState.EquippedWeapon);
            Assert.True(playerState.IsAlive);
            Assert.Equal(25, playerState.AttackPower);
            Assert.Equal(10, playerState.DefensePower);
        }

        [Fact]
        public void EnemyState_PropertiesCanBeSet()
        {
            // Arrange & Act
            var enemyState = new EnemyState
            {
                Name = "Goblin",
                HP = 50,
                MaxHP = 50,
                IsAlive = true,
                AttackStrategy = "Melee"
            };

            // Assert
            Assert.Equal("Goblin", enemyState.Name);
            Assert.Equal(50, enemyState.HP);
            Assert.Equal(50, enemyState.MaxHP);
            Assert.True(enemyState.IsAlive);
            Assert.Equal("Melee", enemyState.AttackStrategy);
        }

        [Fact]
        public void BattleState_InitialState_HasCorrectDefaults()
        {
            // Arrange & Act
            var battleState = new BattleState
            {
                TurnNumber = 1,
                AvailableStrategies = new System.Collections.Generic.List<string> { "Default", "Melee", "Magic" }
            };

            // Assert
            Assert.Equal(1, battleState.TurnNumber);
            Assert.NotNull(battleState.AvailableStrategies);
            Assert.Equal(3, battleState.AvailableStrategies.Count);
            Assert.Contains("Default", battleState.AvailableStrategies);
            Assert.Contains("Melee", battleState.AvailableStrategies);
            Assert.Contains("Magic", battleState.AvailableStrategies);
        }

        [Fact]
        public void ShopState_PropertiesCanBeSet()
        {
            // Arrange & Act
            var shopState = new ShopState
            {
                PotionPrice = 50,
                AvailableItems = new System.Collections.Generic.List<ShopItem>
                {
                    new ShopItem { Name = "Potion", Price = 50, Type = "Consumable", Description = "Heals HP" }
                },
                AvailableWeapons = new System.Collections.Generic.List<WeaponInfo>
                {
                    new WeaponInfo { Name = "Sword", AttackPower = 20, DefensePower = 5, Price = 100 }
                }
            };

            // Assert
            Assert.Equal(50, shopState.PotionPrice);
            Assert.NotNull(shopState.AvailableItems);
            Assert.Single(shopState.AvailableItems);
            Assert.NotNull(shopState.AvailableWeapons);
            Assert.Single(shopState.AvailableWeapons);
        }

        [Fact]
        public void GameMessage_CreatedWithCorrectProperties()
        {
            // Arrange
            var beforeTime = DateTime.UtcNow;
            
            // Act
            var message = new GameMessage
            {
                Text = "Test message",
                Type = MessageType.Combat
            };
            
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.Equal("Test message", message.Text);
            Assert.Equal(MessageType.Combat, message.Type);
            Assert.InRange(message.Timestamp, beforeTime, afterTime);
        }

        [Theory]
        [InlineData(MessageType.Info)]
        [InlineData(MessageType.Success)]
        [InlineData(MessageType.Warning)]
        [InlineData(MessageType.Error)]
        [InlineData(MessageType.Combat)]
        [InlineData(MessageType.System)]
        [InlineData(MessageType.Experience)]
        [InlineData(MessageType.Gold)]
        public void MessageType_AllTypesAreValid(MessageType messageType)
        {
            // Arrange & Act
            var message = new GameMessage
            {
                Text = "Test",
                Type = messageType
            };

            // Assert
            Assert.Equal(messageType, message.Type);
        }

        [Theory]
        [InlineData(GamePhase.Initialization)]
        [InlineData(GamePhase.Exploration)]
        [InlineData(GamePhase.Battle)]
        [InlineData(GamePhase.Shop)]
        [InlineData(GamePhase.Rest)]
        [InlineData(GamePhase.GameOver)]
        public void GamePhase_AllPhasesAreValid(GamePhase phase)
        {
            // Arrange & Act
            var gameState = new GameState
            {
                Phase = phase
            };

            // Assert
            Assert.Equal(phase, gameState.Phase);
        }

        [Fact]
        public void GameState_CompleteSetup_AllPropertiesWork()
        {
            // Arrange & Act
            var gameState = new GameState
            {
                Player = new PlayerState { Name = "Hero", HP = 100, MaxHP = 100, IsAlive = true },
                CurrentEnemy = new EnemyState { Name = "Goblin", HP = 50, MaxHP = 50, IsAlive = true },
                CurrentBattle = new BattleState { TurnNumber = 1 },
                CurrentShop = null,
                Messages = new System.Collections.Generic.List<GameMessage>
                {
                    new GameMessage { Text = "Battle started!", Type = MessageType.Combat }
                },
                Phase = GamePhase.Battle,
                IsGameOver = false
            };

            // Assert
            Assert.NotNull(gameState.Player);
            Assert.Equal("Hero", gameState.Player.Name);
            Assert.NotNull(gameState.CurrentEnemy);
            Assert.Equal("Goblin", gameState.CurrentEnemy.Name);
            Assert.NotNull(gameState.CurrentBattle);
            Assert.Equal(1, gameState.CurrentBattle.TurnNumber);
            Assert.Null(gameState.CurrentShop);
            Assert.Single(gameState.Messages);
            Assert.Equal("Battle started!", gameState.Messages[0].Text);
            Assert.Equal(GamePhase.Battle, gameState.Phase);
            Assert.False(gameState.IsGameOver);
        }
    }
}
