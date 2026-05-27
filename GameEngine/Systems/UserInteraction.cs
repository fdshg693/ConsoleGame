using GameEngine.Constants;

namespace GameEngine.Systems
{
    public static class UserInteraction
    {
        private const int MaxInputAttempts = 5;
        private const int InputTimeoutSeconds = 60;

        /// <summary>
        /// Clears the last line of output from the console.
        /// </summary>
        /// <remarks>This method moves the cursor up by one line and clears the entire line, effectively
        /// removing         the most recent output from the console. It is useful for scenarios where the last output  
        /// needs to be erased or replaced.</remarks>
        public static void ClearLastOutput()
        {
            Console.Write("\x1b[1A");  // 上へカーソル移動
            Console.Write("\x1b[2K");  // 行全体をクリア
        }

        /// <summary>
        /// コンソールから「1以上の整数」を入力させ、正しい値が来るまで繰り返すメソッド
        /// </summary>
        /// <param name="prompt">入力プロンプトメッセージ</param>
        /// <param name="interruptKeyWord">入力をスキップするキーワード（デフォルト: "Q"）</param>
        /// <param name="minValue">許容する最小値（デフォルト: 1）</param>
        /// <param name="maxValue">許容する最大値（デフォルト: int.MaxValue）</param>
        /// <returns>有効な整数値、またはスキップされた場合はnull</returns>
        public static int? ReadPositiveInteger(
            string prompt = "Enter a positive integer: ",
            string interruptKeyWord = "Q",
            int minValue = 1,
            int? maxValue = null)
        {
            if (minValue < 1)
                throw new ArgumentException("minValue must be at least 1", nameof(minValue));

            if (maxValue.HasValue && maxValue.Value < minValue)
                throw new ArgumentException("maxValue must be greater than or equal to minValue", nameof(maxValue));

            int attempts = 0;
            string rangeMessage = maxValue.HasValue
                ? $"Enter an integer between {minValue} and {maxValue.Value}"
                : $"Enter an integer >= {minValue}";

            while (attempts < MaxInputAttempts)
            {
                attempts++;
                Console.WriteLine($"Enter '{interruptKeyWord}' to skip");
                Console.Write(prompt);
                
                string? line = Console.ReadLine();

                // 中断キーワードチェック
                if (!string.IsNullOrWhiteSpace(line) && 
                    line.Trim().Equals(interruptKeyWord, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // 空入力チェック
                if (string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine($"Input is empty. {rangeMessage}.");
                    continue;
                }

                // 数値変換と範囲チェック
                if (!int.TryParse(line.Trim(), out int value))
                {
                    Console.WriteLine($"'{line}' is not a valid integer. {rangeMessage}.");
                    continue;
                }

                if (value < minValue)
                {
                    Console.WriteLine($"Value too small ({value} < {minValue}). {rangeMessage}.");
                    continue;
                }

                if (maxValue.HasValue && value > maxValue.Value)
                {
                    Console.WriteLine($"Value too large ({value} > {maxValue.Value}). {rangeMessage}.");
                    continue;
                }

                return value;
            }

            Console.WriteLine($"Maximum input attempts ({MaxInputAttempts}) reached. Skipping.");
            return null;
        }

        /// <summary>
        /// Yes/No形式の確認入力を受け付ける
        /// </summary>
        /// <param name="prompt">確認メッセージ</param>
        /// <param name="defaultValue">デフォルト値（Enterキー押下時の値）</param>
        /// <returns>Yesの場合true、Noの場合false</returns>
        public static bool ReadConfirmation(string prompt, bool defaultValue = false)
        {
            string defaultText = defaultValue ? "Y/n" : "y/N";
            Console.Write($"{prompt} ({defaultText}): ");

            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            string normalized = input.Trim().ToLowerInvariant();
            
            if (normalized == "y" || normalized == "yes" || normalized == "はい")
                return true;
            
            if (normalized == "n" || normalized == "no" || normalized == "いいえ")
                return false;

            Console.WriteLine($"Invalid input. Using default ({(defaultValue ? "Yes" : "No")}).");
            return defaultValue;
        }

        /// <summary>
        /// 複数の選択肢から1つを選択させる
        /// </summary>
        /// <param name="prompt">選択プロンプト</param>
        /// <param name="options">選択肢の配列</param>
        /// <returns>選択されたインデックス（0始まり）、またはキャンセルされた場合はnull</returns>
        public static int? ReadChoice(string prompt, string[] options, bool allowCancel = true)
        {
            if (options == null || options.Length == 0)
                throw new ArgumentException("Options cannot be null or empty", nameof(options));

            Console.WriteLine(prompt);
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {options[i]}");
            }

            if (allowCancel)
                Console.WriteLine($"  0. Cancel");

            int minValue = allowCancel ? 0 : 1;
            int? choice = ReadPositiveInteger(
                "Select: ",
                "Q",
                minValue: minValue, 
                maxValue: options.Length);

            if (!choice.HasValue || (allowCancel && choice.Value == 0))
                return null;

            return choice.Value - 1;
        }
        public static string SelectAttackStrategy(IReadOnlyList<string>? attackStrategies = null)
        {
            var strategyList = attackStrategies ?? new List<string>(AttackStrategyNames.All);
            var options = strategyList.ToArray();

            ConsoleRenderer.WriteInfo("Select attack strategy:");
            int selected = ConsoleRenderer.SelectFromMenu(options, 0, ConsoleRenderer.MenuOrientation.Horizontal);

            return selected >= 0 ? strategyList[selected] : strategyList[0];
        }

        /// <summary>
        /// Select a game action (continue, save, quit).
        /// </summary>
        /// <returns>Selected action: "continue", "save_continue", "save_quit", "quit"</returns>
        public static string SelectGameAction()
        {
            var actionArray = new string[] { "Continue", "Save & Continue", "Save & Quit", "Quit" };

            ConsoleRenderer.WriteSection("What would you like to do?");
            int selected = ConsoleRenderer.SelectFromMenu(actionArray, 0, ConsoleRenderer.MenuOrientation.Vertical);

            if (selected < 0) selected = 0;
            return actionArray[selected].ToLowerInvariant().Replace(" ", "_").Replace("&_", "");
        }
    }

}
