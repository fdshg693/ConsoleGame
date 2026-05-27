using GameEngine.Constants;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Manager
{
    public class InventoryManager : IEquipmentStatsProvider
    {
        public int TotalGold { get; private set; } = GameConstants.InitialGold;
        public IWeapon Weapon { get; private set; }
        public event Action? EquipmentChanged;
        public int TotalPotions { get; private set; } = GameConstants.InitialPotions;
        public InventoryManager()
        {
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
            if (TotalGold >= amount * GameConstants.PotionPrice)
            {
                TotalGold -= amount * GameConstants.PotionPrice;
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
