using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;

namespace GameEngine.Systems
{
    public static class RestSystem
    {
        public static List<GameMessage> ProcessRestAction(IPlayer player, UseItemAction? action)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var messages = new List<GameMessage>();

            if (action == null)
            {
                return messages;
            }

            if (!PlayerActionValidator.IsValid(action, out var errorMessage))
            {
                messages.Add(GameStateMapper.CreateMessage($"Invalid rest action: {errorMessage}", MessageType.Warning));
                return messages;
            }

            if (!string.Equals(action.ItemName, "Potion", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(GameStateMapper.CreateMessage("Only Potion can be used during rest.", MessageType.Warning));
                return messages;
            }

            if (player.ReturnTotalPotions() < action.Quantity)
            {
                messages.Add(GameStateMapper.CreateMessage("Not enough potions!", MessageType.Warning));
                return messages;
            }

            player.UsePotion(action.Quantity);
            return messages;
        }
    }
}
