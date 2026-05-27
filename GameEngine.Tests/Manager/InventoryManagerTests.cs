using Xunit;
using GameEngine.Manager;
using GameEngine.Models;

namespace GameEngine.Tests.Manager
{
    /// <summary>
    /// InventoryManagerのテスト
    /// </summary>
    public class InventoryManagerTests
    {
        [Fact]
        public void EquipWeapon_UpdatesWeaponAndAffectsHealthManager()
        {
            // Arrange
            var inventory = new InventoryManager();
            var health = new HealthManager(baseHP: 100, baseDP: 2, equipProvider: inventory);

            Assert.Equal(100, health.MaxHP);
            Assert.Equal(2, health.TotalDP);

            // Act
            var weapon = new Weapon(20, 5, 3, "Iron Sword");
            inventory.EquipWeapon(weapon);

            // Assert
            Assert.Equal("Iron Sword", inventory.Weapon.Name);
            Assert.Equal(120, health.MaxHP);
            Assert.Equal(5, health.TotalDP);
        }
    }
}