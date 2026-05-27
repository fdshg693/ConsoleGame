using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace CliRpgGame.UI
{
    /// <summary>
    /// Console向けの入力実装。<see cref="ConsoleRenderer"/> でメニューを描画し、矢印キー/数字入力で行動を受け取る。
    /// </summary>
    public class ConsoleGameInput : IGameInput
    {
        private readonly ConsoleRenderer _renderer;
        private readonly int _potionPrice;
        private readonly int _potionHealAmount;

        public ConsoleGameInput(ConsoleRenderer renderer, int potionPrice, int potionHealAmount)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _potionPrice = potionPrice;
            _potionHealAmount = potionHealAmount;
        }

        public AttackAction SelectAttackAction(BattleState battleState, PlayerState playerState, EnemyState enemyState)
        {
            var strategyName = UserInteraction.SelectAttackStrategy(_renderer, battleState.AvailableStrategies);
            return new AttackAction(strategyName);
        }

        public ShopAction SelectShopAction(ShopState shopState, PlayerState playerState)
        {
            _renderer.ClearScreen("SHOP");
            _renderer.WriteInfo($"  Gold: {playerState.Gold}    Potions: {playerState.Potions}    HP: {playerState.HP}/{playerState.MaxHP}");
            Console.WriteLine();
            _renderer.WriteSeparator();
            Console.WriteLine();

            var mainOptions = new[] { "Buy Potion", "Buy Weapon", "Exit Shop" };
            var descriptions = new[]
            {
                $"{_potionPrice}g each, heals {_potionHealAmount} HP",
                "Equip a new weapon",
                ""
            };

            int choice = _renderer.SelectFromMenu(mainOptions, 0, ConsoleRenderer.MenuOrientation.Vertical, false, descriptions);

            if (choice == 0) // Buy Potion
            {
                Console.WriteLine();
                int maxAffordable = playerState.Gold / shopState.PotionPrice;
                if (maxAffordable <= 0)
                {
                    _renderer.WriteWarning("  Not enough gold to buy potions!");
                    _renderer.WaitForKeyPress();
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
                _renderer.WriteInfo("  How many potions?");
                int potionChoice = _renderer.SelectFromMenu(potionOptions, 0, ConsoleRenderer.MenuOrientation.Vertical);

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

                _renderer.WriteInfo("  Choose a weapon:");
                int weaponChoice = _renderer.SelectFromMenu(weaponOptions, 0, ConsoleRenderer.MenuOrientation.Vertical, false, weaponDescriptions);

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
            _renderer.WriteSeparator();
            _renderer.WriteInfo("  REST - Use Potions");
            _renderer.RenderHPBar(playerState.Name, playerState.HP, playerState.MaxHP);
            _renderer.WriteInfo($"  Potions: {playerState.Potions}");
            Console.WriteLine();

            int healPerPotion = _potionHealAmount;
            int maxUseful = (int)Math.Ceiling((double)(playerState.MaxHP - playerState.HP) / healPerPotion);
            int maxOptions = Math.Min(playerState.Potions, Math.Min(maxUseful, 5));

            var options = new string[maxOptions + 1];
            options[0] = "Skip";
            for (int i = 1; i <= maxOptions; i++)
            {
                int projectedHP = Math.Min(playerState.HP + i * healPerPotion, playerState.MaxHP);
                options[i] = $"Use {i} Potion{(i > 1 ? "s" : "")} (HP: {playerState.HP} -> {projectedHP})";
            }

            int selected = _renderer.SelectFromMenu(options, 0, ConsoleRenderer.MenuOrientation.Vertical);

            if (selected <= 0)
            {
                return null;
            }

            return new UseItemAction("Potion", selected);
        }

        /// <summary>
        /// エンカウント後の進行アクション（続行/セーブ/終了）を矢印キーで選択させる。
        /// </summary>
        public GameActionChoice SelectGameAction()
        {
            var actionArray = new[] { "Continue", "Save & Continue", "Save & Quit", "Quit" };

            _renderer.WriteSection("What would you like to do?");
            int selected = _renderer.SelectFromMenu(actionArray, 0, ConsoleRenderer.MenuOrientation.Vertical);

            return selected switch
            {
                1 => GameActionChoice.SaveAndContinue,
                2 => GameActionChoice.SaveAndQuit,
                3 => GameActionChoice.Quit,
                _ => GameActionChoice.Continue
            };
        }
    }
}
