using Xunit;
using GameEngine.Manager;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Tests.Manager
{
    /// <summary>
    /// HealthManagerのテスト
    /// </summary>
    public class HealthManagerTests
    {
        private sealed class TestEquipmentProvider : IEquipmentStatsProvider
        {
            public IWeapon Weapon { get; private set; }
            public event Action? EquipmentChanged;

            public TestEquipmentProvider(IWeapon weapon)
            {
                Weapon = weapon;
            }

            public void SetWeapon(IWeapon weapon)
            {
                Weapon = weapon;
                EquipmentChanged?.Invoke();
            }
        }

        [Fact]
        public void MaxHpAndTotalDp_IncludeWeaponStats_AndClipOnDecrease()
        {
            // Arrange
            var equip = new TestEquipmentProvider(new Weapon(10, 0, 5, "Starter"));
            var health = new HealthManager(baseHP: 100, baseDP: 3, equipProvider: equip);

            // Assert initial equipment
            Assert.Equal(110, health.MaxHP);
            Assert.Equal(8, health.TotalDP);
            Assert.Equal(110, health.CurrentHP);

            // Act: equip lower HP/DP weapon
            equip.SetWeapon(new Weapon(0, 0, 0, "None"));

            // Assert: stats update and HP is clipped to new MaxHP
            Assert.Equal(100, health.MaxHP);
            Assert.Equal(3, health.TotalDP);
            Assert.Equal(100, health.CurrentHP);
        }
    }
}