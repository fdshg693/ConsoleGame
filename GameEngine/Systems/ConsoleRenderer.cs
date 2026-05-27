using GameEngine.DTOs;
using GameEngine.Models;

namespace GameEngine.Systems
{
    /// <summary>
    /// Console output centralization - all display logic goes through here.
    /// Provides screen clearing, color-coded messages, HP bars, menus, and layout utilities.
    /// </summary>
    public static class ConsoleRenderer
    {
        // ANSI color codes
        private const string Reset = "\x1b[0m";
        private const string Bold = "\x1b[1m";
        private const string Red = "\x1b[31m";
        private const string Green = "\x1b[32m";
        private const string Yellow = "\x1b[33m";
        private const string Cyan = "\x1b[36m";
        private const string Gray = "\x1b[90m";
        private const string BoldYellow = "\x1b[1;33m";
        private const string BoldRed = "\x1b[1;31m";
        private const string BoldGreen = "\x1b[1;32m";
        private const string BoldCyan = "\x1b[1;36m";
        private const string Inverse = "\x1b[7m";

        private const int ScreenWidth = 60;

        public enum MenuOrientation
        {
            Vertical,
            Horizontal
        }

        // ─────────────────────────────────────────────
        // Screen Management
        // ─────────────────────────────────────────────

        /// <summary>
        /// Clears the screen and renders a header bar.
        /// </summary>
        public static void ClearScreen(string title)
        {
            Console.Clear();
            var line = new string('=', ScreenWidth);
            Console.WriteLine($"{Bold}{line}{Reset}");
            Console.WriteLine($"{Bold}{CenterText(title, ScreenWidth)}{Reset}");
            Console.WriteLine($"{Bold}{line}{Reset}");
            Console.WriteLine();
        }

        /// <summary>
        /// Waits for the user to press any key before continuing.
        /// </summary>
        public static void WaitForKeyPress(string prompt = "Press any key to continue...")
        {
            Console.WriteLine();
            Console.Write($"{Gray}{prompt}{Reset}");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }

        // ─────────────────────────────────────────────
        // Message Rendering
        // ─────────────────────────────────────────────

        /// <summary>
        /// Renders a single GameMessage with color coding based on MessageType.
        /// </summary>
        public static void RenderMessage(GameMessage message)
        {
            string color = message.Type switch
            {
                MessageType.Combat => Yellow,
                MessageType.Success => BoldGreen,
                MessageType.Warning => BoldYellow,
                MessageType.Error => BoldRed,
                MessageType.Experience => Cyan,
                MessageType.Gold => BoldYellow,
                MessageType.System => Gray,
                _ => ""
            };

            if (string.IsNullOrEmpty(color))
                Console.WriteLine(message.Text);
            else
                Console.WriteLine($"{color}{message.Text}{Reset}");
        }

        /// <summary>
        /// Renders multiple GameMessages.
        /// </summary>
        public static void RenderMessages(IEnumerable<GameMessage> messages)
        {
            foreach (var message in messages)
            {
                RenderMessage(message);
            }
        }

        // ─────────────────────────────────────────────
        // Typed Output Helpers
        // ─────────────────────────────────────────────

        public static void WriteInfo(string text)
        {
            Console.WriteLine(text);
        }

        public static void WriteSuccess(string text)
        {
            Console.WriteLine($"{BoldGreen}{text}{Reset}");
        }

        public static void WriteWarning(string text)
        {
            Console.WriteLine($"{BoldYellow}{text}{Reset}");
        }

        public static void WriteError(string text)
        {
            Console.WriteLine($"{BoldRed}{text}{Reset}");
        }

        public static void WriteSystem(string text)
        {
            Console.WriteLine($"{Gray}{text}{Reset}");
        }

        public static void WriteCombat(string text)
        {
            Console.WriteLine($"{Yellow}{text}{Reset}");
        }

        // ─────────────────────────────────────────────
        // HP Bar Rendering
        // ─────────────────────────────────────────────

        /// <summary>
        /// Renders a visual HP bar: "Name   [########........] 80/100 HP"
        /// </summary>
        public static void RenderHPBar(string name, int current, int max, int barWidth = 20)
        {
            int filled = max > 0 ? (int)Math.Round((double)current / max * barWidth) : 0;
            filled = Math.Clamp(filled, 0, barWidth);
            int empty = barWidth - filled;

            string bar = new string('#', filled) + new string('.', empty);

            // Color based on HP percentage
            double ratio = max > 0 ? (double)current / max : 0;
            string color = ratio switch
            {
                > 0.6 => Green,
                > 0.3 => Yellow,
                _ => Red
            };

            string paddedName = name.PadRight(12);
            Console.WriteLine($"  {paddedName}{color}[{bar}]{Reset} {current}/{max} HP");
        }

        // ─────────────────────────────────────────────
        // Status Panel
        // ─────────────────────────────────────────────

        /// <summary>
        /// Renders a compact status panel for player (and optionally enemy).
        /// </summary>
        public static void RenderStatusPanel(PlayerState player, EnemyState? enemy = null)
        {
            RenderHPBar(player.Name, player.HP, player.MaxHP);
            if (enemy != null)
            {
                RenderHPBar(enemy.Name, enemy.HP, enemy.MaxHP);
            }
            Console.WriteLine($"  {Gray}AP:{player.AttackPower}  DP:{player.DefensePower}  Gold:{player.Gold}  Potions:{player.Potions}  Weapon:{player.EquippedWeapon ?? "None"}{Reset}");
            Console.WriteLine();
        }

        // ─────────────────────────────────────────────
        // Section Separators
        // ─────────────────────────────────────────────

        public static void WriteSection(string title)
        {
            int padding = Math.Max(0, (ScreenWidth - title.Length - 4) / 2);
            string left = new string('=', padding);
            string right = new string('=', ScreenWidth - padding - title.Length - 4);
            Console.WriteLine($"\n{Bold}{left}  {title}  {right}{Reset}");
        }

        public static void WriteSeparator()
        {
            Console.WriteLine($"{Gray}{new string('-', ScreenWidth)}{Reset}");
        }

        public static void WriteBox(string[] lines)
        {
            int maxLen = lines.Max(l => l.Length);
            int boxWidth = Math.Max(maxLen + 4, 30);

            Console.WriteLine($"{Bold}+{new string('-', boxWidth - 2)}+{Reset}");
            foreach (var line in lines)
            {
                Console.WriteLine($"{Bold}|{Reset} {line.PadRight(boxWidth - 4)} {Bold}|{Reset}");
            }
            Console.WriteLine($"{Bold}+{new string('-', boxWidth - 2)}+{Reset}");
        }

        /// <summary>
        /// Renders a highlighted result box (victory, defeat, etc.)
        /// </summary>
        public static void WriteResultBox(string title, string[] details, bool isVictory)
        {
            string color = isVictory ? BoldGreen : BoldRed;
            int maxLen = Math.Max(title.Length, details.Length > 0 ? details.Max(d => d.Length) : 0);
            int boxWidth = Math.Max(maxLen + 6, 36);

            Console.WriteLine();
            Console.WriteLine($"{color}+{new string('=', boxWidth - 2)}+{Reset}");
            Console.WriteLine($"{color}|{CenterText(title, boxWidth - 2)}|{Reset}");
            Console.WriteLine($"{color}+{new string('-', boxWidth - 2)}+{Reset}");
            foreach (var line in details)
            {
                Console.WriteLine($"{color}|{Reset}  {line.PadRight(boxWidth - 4)}{color}|{Reset}");
            }
            Console.WriteLine($"{color}+{new string('=', boxWidth - 2)}+{Reset}");
            Console.WriteLine();
        }

        // ─────────────────────────────────────────────
        // Unified Menu Selection
        // ─────────────────────────────────────────────

        /// <summary>
        /// Unified arrow-key menu selector. Returns the selected index (0-based), or -1 if cancelled.
        /// </summary>
        public static int SelectFromMenu(
            string[] options,
            int initialIndex = 0,
            MenuOrientation orientation = MenuOrientation.Vertical,
            bool allowCancel = false,
            string[]? descriptions = null)
        {
            if (options.Length == 0) return -1;

            int currentIndex = Math.Clamp(initialIndex, 0, options.Length - 1);

            if (orientation == MenuOrientation.Vertical)
                return SelectVertical(options, currentIndex, allowCancel, descriptions);
            else
                return SelectHorizontal(options, currentIndex, allowCancel);
        }

        private static int SelectVertical(string[] options, int currentIndex, bool allowCancel, string[]? descriptions)
        {
            // Render initial menu
            RenderVerticalMenu(options, currentIndex, descriptions);

            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    currentIndex = (currentIndex - 1 + options.Length) % options.Length;
                    ClearLines(options.Length + 1); // +1 for hint line
                    RenderVerticalMenu(options, currentIndex, descriptions);
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    currentIndex = (currentIndex + 1) % options.Length;
                    ClearLines(options.Length + 1);
                    RenderVerticalMenu(options, currentIndex, descriptions);
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    return currentIndex;
                }
                else if (keyInfo.Key == ConsoleKey.Escape && allowCancel)
                {
                    return -1;
                }
            }
        }

        private static void RenderVerticalMenu(string[] options, int selectedIndex, string[]? descriptions)
        {
            for (int i = 0; i < options.Length; i++)
            {
                string desc = (descriptions != null && i < descriptions.Length) ? $"  {Gray}{descriptions[i]}{Reset}" : "";
                if (i == selectedIndex)
                    Console.WriteLine($"  {Inverse}{Bold} > {options[i]} {Reset}{desc}");
                else
                    Console.WriteLine($"    {options[i]}{desc}");
            }
            Console.Write($"  {Gray}(Up/Down to select, Enter to confirm){Reset}");
        }

        private static int SelectHorizontal(string[] options, int currentIndex, bool allowCancel)
        {
            RenderHorizontalMenu(options, currentIndex);

            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    currentIndex = (currentIndex - 1 + options.Length) % options.Length;
                    ClearLines(1); // horizontal menu is 1 line + hint
                    RenderHorizontalMenu(options, currentIndex);
                }
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    currentIndex = (currentIndex + 1) % options.Length;
                    ClearLines(1);
                    RenderHorizontalMenu(options, currentIndex);
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine(); // newline after selection
                    return currentIndex;
                }
                else if (keyInfo.Key == ConsoleKey.Escape && allowCancel)
                {
                    Console.WriteLine();
                    return -1;
                }
            }
        }

        private static void RenderHorizontalMenu(string[] options, int selectedIndex)
        {
            Console.Write("  ");
            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                    Console.Write($"{Inverse}{Bold} {options[i]} {Reset}  ");
                else
                    Console.Write($" {options[i]}   ");
            }
            Console.Write($"  {Gray}(<- -> Enter){Reset}");
        }

        // ─────────────────────────────────────────────
        // Internal Helpers
        // ─────────────────────────────────────────────

        /// <summary>
        /// Clears N lines above the cursor using ANSI sequences.
        /// </summary>
        private static void ClearLines(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Console.Write("\x1b[1A"); // move up
                Console.Write("\x1b[2K"); // clear line
            }
            Console.Write("\r"); // return to start of line
        }

        private static string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return text.PadLeft(padding + text.Length).PadRight(width);
        }
    }
}
