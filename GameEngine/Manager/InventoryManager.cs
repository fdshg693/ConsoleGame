using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Manager
{
    public class InventoryManager : IEquipmentStatsProvider
    {
        private readonly int _potionPrice;

        public int TotalGold { get; private set; }
        public IWeapon Weapon { get; private set; }
        public event Action? EquipmentChanged;
        public int TotalPotions { get; private set; }
        public InventoryManager(int initialGold, int initialPotions, int potionPrice)
        {
            if (initialGold < 0)
                throw new ArgumentOutOfRangeException(nameof(initialGold), "Initial gold cannot be negative");
            if (initialPotions < 0)
                throw new ArgumentOutOfRangeException(nameof(initialPotions), "Initial potions cannot be negative");
            if (potionPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(potionPrice), "Potion price cannot be negative");

            TotalGold = initialGold;
            TotalPotions = initialPotions;
            _potionPrice = potionPrice;
            Weapon = new Weapon(0, 0, 0, "Default");
        }
        public void EquipWeapon(IWeapon newWeapon)
        {
            Weapon = newWeapon;
            EquipmentChanged?.Invoke();
            GameMessageBus.Publish($"You equipped a {newWeapon.Name}", MessageType.Info);
        }
        public void GainGold(int amount)
        {
            TotalGold += amount;
            GameMessageBus.Publish($"You gain {amount} gold", MessageType.Gold);
        }
        public void BuyPotion(int amount)
        {
            if (TotalGold >= amount * _potionPrice)
            {
                TotalGold -= amount * _potionPrice;
                TotalPotions += amount;
                GameMessageBus.Publish($"You bought {amount} potions", MessageType.Success);
            }
            else
            {
                GameMessageBus.Publish("Not enough gold!", MessageType.Warning);

            }
        }
        public void UsePotion(int amount)
        {
            if (TotalPotions >= amount)
            {
                TotalPotions -= amount;
                GameMessageBus.Publish($"You used {amount} potions", MessageType.Info);
            }
            else
            {
                GameMessageBus.Publish("Not enough potions!", MessageType.Warning);
            }
        }
        public int ReturnTotalPotions()
        {
            return TotalPotions;
        }
        public int ReturnTotalGold()
        {
            return TotalGold;
        }
        public void ShowInfo()
        {
            GameMessageBus.Publish($"Total Gold: {TotalGold}", MessageType.Info);
            GameMessageBus.Publish($"Total Potions: {TotalPotions}", MessageType.Info);
            GameMessageBus.Publish($"Equipped Weapon: {Weapon.Name}", MessageType.Info);
        }
    }
}
