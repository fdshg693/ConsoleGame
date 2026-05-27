using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace GameEngine.Systems
{
    /// <summary>
    /// Console向けの入力実装
    /// </summary>
    public class ConsoleGameInput : IGameInput
    {
        public AttackAction SelectAttackAction(BattleState battleState, PlayerState playerState, EnemyState enemyState)
        {
            var strategyName = UserInteraction.SelectAttackStrategy(battleState.AvailableStrategies);
            return new AttackAction(strategyName);
        }

        public ShopAction SelectShopAction(ShopState shopState, PlayerState playerState)
        {
            ConsoleRenderer.ClearScreen("SHOP");
            ConsoleRenderer.WriteInfo($"  Gold: {playerState.Gold}    Potions: {playerState.Potions}    HP: {playerState.HP}/{playerState.MaxHP}");
            Console.WriteLine();
            ConsoleRenderer.WriteSeparator();
            Console.WriteLine();

            var mainOptions = new[] { "Buy Potion", "Buy Weapon", "Exit Shop" };
            var descriptions = new[]
            {
                $"{GameConstants.PotionPrice}g each, heals {GameConstants.PotionHealAmount} HP",
                "Equip a new weapon",
                ""
            };

            int choice = ConsoleRenderer.SelectFromMenu(mainOptions, 0, ConsoleRenderer.MenuOrientation.Vertical, false, descriptions);

            if (choice == 0) // Buy Potion
            {
                Console.WriteLine();
                int maxAffordable = playerState.Gold / shopState.PotionPrice;
                if (maxAffordable <= 0)
                {
                    ConsoleRenderer.WriteWarning("  Not enough gold to buy potions!");
                    ConsoleRenderer.WaitForKeyPress();
                    return new ShopAction(ShopActionType.Exit);
                }

                int optionCount = Math.Min(maxAffordable, 5);
                var potionOptions = new string[optionCount + 1];
                potionOptions[0] = "Cancel";
                for (int i = 1; i <= optionCount; i++)
                {
                    int cost = i * shopState.PotionPrice;
                    potionOptions[i] = $"{i} Potion{(i > 1 ? "s" : "")} (cost: {cost}g)";
                }

                Console.WriteLine();
                ConsoleRenderer.WriteInfo("  How many potions?");
                int potionChoice = ConsoleRenderer.SelectFromMenu(potionOptions, 0, ConsoleRenderer.MenuOrientation.Vertical);

                if (potionChoice <= 0)
                {
                    return new ShopAction(ShopActionType.Exit);
                }

                return new ShopAction(ShopActionType.BuyPotion, quantity: potionChoice);
            }
            else if (choice == 1) // Buy Weapon
            {
                Console.WriteLine();
                var weaponOptions = new string[shopState.AvailableWeapons.Count + 1];
                var weaponDescriptions = new string[shopState.AvailableWeapons.Count + 1];
                for (int i = 0; i < shopState.AvailableWeapons.Count; i++)
                {
                    var w = shopState.AvailableWeapons[i];
                    weaponOptions[i] = w.Name;
                    weaponDescriptions[i] = $"AP:+{w.AttackPower}  DP:+{w.DefensePower}";
                }
                weaponOptions[^1] = "Back";
                weaponDescriptions[^1] = "";

                ConsoleRenderer.WriteInfo("  Choose a weapon:");
                int weaponChoice = ConsoleRenderer.SelectFromMenu(weaponOptions, 0, ConsoleRenderer.MenuOrientation.Vertical, false, weaponDescriptions);

                if (weaponChoice < 0 || weaponChoice >= shopState.AvailableWeapons.Count)
                {
                    return new ShopAction(ShopActionType.Exit);
                }

                string weaponName = shopState.AvailableWeapons[weaponChoice].Name;
                return new ShopAction(ShopActionType.BuyWeapon, weaponName, 1);
            }
            else // Exit Shop
            {
                return new ShopAction(ShopActionType.Exit);
            }
        }

        public UseItemAction? SelectRestAction(PlayerState playerState)
        {
            if (playerState.Potions == 0 || playerState.HP >= playerState.MaxHP)
            {
                return null;
            }

            Console.WriteLine();
            ConsoleRenderer.WriteSeparator();
            ConsoleRenderer.WriteInfo("  REST - Use Potions");
            ConsoleRenderer.RenderHPBar(playerState.Name, playerState.HP, playerState.MaxHP);
            ConsoleRenderer.WriteInfo($"  Potions: {playerState.Potions}");
            Console.WriteLine();

            int healPerPotion = GameConstants.PotionHealAmount;
            int maxUseful = (int)Math.Ceiling((double)(playerState.MaxHP - playerState.HP) / healPerPotion);
            int maxOptions = Math.Min(playerState.Potions, Math.Min(maxUseful, 5));

            var options = new string[maxOptions + 1];
            options[0] = "Skip";
            for (int i = 1; i <= maxOptions; i++)
            {
                int projectedHP = Math.Min(playerState.HP + i * healPerPotion, playerState.MaxHP);
                options[i] = $"Use {i} Potion{(i > 1 ? "s" : "")} (HP: {playerState.HP} -> {projectedHP})";
            }

            int selected = ConsoleRenderer.SelectFromMenu(options, 0, ConsoleRenderer.MenuOrientation.Vertical);

            if (selected <= 0)
            {
                return null;
            }

            return new UseItemAction("Potion", selected);
        }
    }
}
