using GameEngine.DTOs;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;

namespace GameEngine.Systems
{
    public static class ShopSystem
    {
        public static ShopState CreateShopState(int potionPrice)
        {
            var shopState = GameStateMapper.CreateInitialShopState(potionPrice);
            shopState.AvailableWeapons = new List<WeaponInfo>
            {
                WeaponFactory.CreateWeapon("SWORD").ToWeaponInfo(),
                WeaponFactory.CreateWeapon("AXE").ToWeaponInfo(),
                WeaponFactory.CreateWeapon("BOW").ToWeaponInfo()
            };

            return shopState;
        }

        public static List<GameMessage> ProcessShopAction(IPlayer player, ShopAction action, int potionPrice)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var messages = new List<GameMessage>();

            if (!PlayerActionValidator.IsValid(action, out var errorMessage))
            {
                messages.Add(GameStateMapper.CreateMessage($"Invalid shop action: {errorMessage}", MessageType.Warning));
                return messages;
            }

            switch (action.ShopType)
            {
                case ShopActionType.BuyPotion:
                    int totalCost = action.Quantity * potionPrice;
                    if (player.ReturnTotalGold() < totalCost)
                    {
                        messages.Add(GameStateMapper.CreateMessage("Not enough gold!", MessageType.Warning));
                        return messages;
                    }

                    player.BuyPotion(action.Quantity);
                    return messages;

                case ShopActionType.BuyWeapon:
                    if (string.IsNullOrWhiteSpace(action.ItemName))
                    {
                        messages.Add(GameStateMapper.CreateMessage("Weapon name is required.", MessageType.Warning));
                        return messages;
                    }

                    try
                    {
                        player.EquipWeapon(WeaponFactory.CreateWeapon(action.ItemName));
                    }
                    catch (Exception ex)
                    {
                        messages.Add(GameStateMapper.CreateMessage($"Failed to equip weapon: {ex.Message}", MessageType.Error));
                    }
                    return messages;

                case ShopActionType.Exit:
                    messages.Add(GameStateMapper.CreateMessage("You left the shop.", MessageType.Info));
                    return messages;

                default:
                    messages.Add(GameStateMapper.CreateMessage("Unknown shop action.", MessageType.Error));
                    return messages;
            }
        }
    }
}
