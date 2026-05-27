using GameEngine.Interfaces;
using GameEngine.Models;
namespace GameEngine.Factory
{
    public class EnemySpec
    {
        public string Name { get; set; } = "";
        public int HP { get; set; }
        public string AttackStrategy { get; set; } = "";
        public int Experience { get; set; }
        public int AP { get; set; }
        public int DP { get; set; }
    }

    public static class EnemyFactory
    {
        private static readonly Dictionary<string, EnemySpec> _specs;
        private const string DefaultYamlPath = "./enemy-specs.yml";

        // static コンストラクタで一度だけ読み込む
        static EnemyFactory()
        {
            _specs = LoadEnemySpecs(DefaultYamlPath);
        }

        /// <summary>
        /// YAMLファイルから敵の仕様を読み込む
        /// </summary>
        /// <param name="yamlPath">YAMLファイルのパス</param>
        /// <returns>敵の仕様のディクショナリ</returns>
        /// <exception cref="FileNotFoundException">YAMLファイルが見つからない場合</exception>
        /// <exception cref="InvalidOperationException">YAML解析に失敗した場合</exception>
        private static Dictionary<string, EnemySpec> LoadEnemySpecs(string yamlPath)
        {
            return YamlSpecLoader.Load<EnemySpec>(
                yamlPath,
                specLabelTitle: "Enemy",
                validate: ValidateEnemySpec,
                comparer: StringComparer.Ordinal);
        }

        /// <summary>
        /// 敵の仕様の妥当性を検証する
        /// </summary>
        private static void ValidateEnemySpec(string key, EnemySpec spec)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(spec.Name))
                errors.Add($"Name is required");

            if (spec.HP <= 0)
                errors.Add($"HP must be positive (got {spec.HP})");

            if (spec.AP < 0)
                errors.Add($"AP cannot be negative (got {spec.AP})");

            if (spec.DP < 0)
                errors.Add($"DP cannot be negative (got {spec.DP})");

            if (spec.Experience < 0)
                errors.Add($"Experience cannot be negative (got {spec.Experience})");

            if (string.IsNullOrWhiteSpace(spec.AttackStrategy))
                errors.Add($"AttackStrategy is required");
            else if (!IsValidAttackStrategy(spec.AttackStrategy))
                errors.Add($"Unknown AttackStrategy: {spec.AttackStrategy}. Valid values: Default, Melee, Magic");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Invalid enemy spec for key '{key}':\n  - {string.Join("\n  - ", errors)}");
            }
        }

        /// <summary>
        /// 攻撃戦略が有効かどうかを確認する
        /// </summary>
        private static bool IsValidAttackStrategy(string strategyName)
        {
            return strategyName switch
            {
                "Default" or "Melee" or "Magic" => true,
                _ => false
            };
        }

        public static IEnemy Create(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Enemy key cannot be null or empty", nameof(key));

            if (!_specs.TryGetValue(key, out var spec))
                throw new ArgumentException(
                    $"Unknown enemy key: '{key}'. Available keys: {string.Join(", ", _specs.Keys)}",
                    nameof(key));

            // 文字列をストラテジー型にマッピング
            IAttackStrategy strat = spec.AttackStrategy switch
            {
                "Melee" => new MeleeAttackStrategy(),
                "Default" => new DefaultAttackStrategy(),
                "Magic" => new MagicAttackStrategy(),
                _ => throw new InvalidOperationException($"Unknown strategy: {spec.AttackStrategy}")
            };

            return new Enemy(
                name: spec.Name,
                hp: spec.HP,
                attackStrategy: strat,
                experience: spec.Experience,
                aP: spec.AP,
                dP: spec.DP
            );
        }

        public static IEnemy CreateRandomEnemy()
        {
            if (_specs.Count == 0)
                throw new InvalidOperationException("No enemy specs available to create random enemy");

            var keys = new List<string>(_specs.Keys);
            var rnd = new Random();
            string choice = keys[rnd.Next(keys.Count)];
            return Create(choice);
        }

        /// <summary>
        /// 利用可能な敵のキー一覧を取得する（テスト用）
        /// </summary>
        public static IReadOnlyCollection<string> GetAvailableEnemyKeys()
        {
            return _specs.Keys;
        }
    }

}
