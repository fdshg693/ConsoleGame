using GameEngine.Configuration;
using GameEngine.Constants;
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

    /// <summary>
    /// YAML 定義から敵を生成するファクトリ。
    /// 設定（<see cref="EnemyConfig"/>）と <see cref="Random"/> をコンストラクタ注入し、
    /// 戦闘テストで決定的・インメモリな差し替えが可能になるよう <see cref="IEnemyFactory"/> を実装する。
    /// </summary>
    public class EnemyFactory : IEnemyFactory
    {
        private readonly Dictionary<string, EnemySpec> _specs;
        private readonly EnemyConfig _enemyConfig;
        private readonly Random _random;
        private const string DefaultYamlPath = "./enemy-specs.yml";

        /// <param name="enemyConfig">敵撃破時のゴールド計算に使う設定。</param>
        /// <param name="random">ランダム選択・ゴールド計算に使う乱数源。省略時は <see cref="Random.Shared"/>。</param>
        public EnemyFactory(EnemyConfig enemyConfig, Random? random = null)
        {
            _enemyConfig = enemyConfig ?? throw new ArgumentNullException(nameof(enemyConfig));
            _random = random ?? Random.Shared;
            _specs = LoadEnemySpecs(ResolveSpecPath(DefaultYamlPath));
        }

        /// <summary>
        /// 仕様ファイルの実在パスを解決する。
        /// 1) 指定パス（カレントディレクトリ相対）、2) 出力ディレクトリ
        /// （CopyToOutputDirectory: Always でコピーされたファイル）の順に探索する。
        /// いずれも存在しない場合は元のパスを返し、読み込み時に明示的な例外を送出させる。
        /// </summary>
        private static string ResolveSpecPath(string yamlPath)
        {
            if (File.Exists(yamlPath))
                return yamlPath;

            string baseDirCandidate = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(yamlPath));
            if (File.Exists(baseDirCandidate))
                return baseDirCandidate;

            return yamlPath;
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
                errors.Add($"Unknown AttackStrategy: {spec.AttackStrategy}. Valid values: {string.Join(", ", AttackStrategyNames.All)}");

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
                AttackStrategyNames.Default
                or AttackStrategyNames.Melee
                or AttackStrategyNames.Magic => true,
                _ => false
            };
        }

        public IEnemy Create(string key)
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
                AttackStrategyNames.Melee => new MeleeAttackStrategy(),
                AttackStrategyNames.Default => new DefaultAttackStrategy(),
                AttackStrategyNames.Magic => new MagicAttackStrategy(),
                _ => throw new InvalidOperationException($"Unknown strategy: {spec.AttackStrategy}")
            };

            // ゴールド報酬を設定と注入された乱数源から算出する（Enemy 自身は乱数を持たない）
            int yieldGold = (spec.Experience / _enemyConfig.GoldBaseMultiplier)
                + _random.Next(_enemyConfig.GoldRandomMin, _enemyConfig.GoldRandomMax);

            return new Enemy(
                name: spec.Name,
                hp: spec.HP,
                attackStrategy: strat,
                experience: spec.Experience,
                aP: spec.AP,
                dP: spec.DP,
                yieldGold: yieldGold
            );
        }

        public IEnemy CreateRandomEnemy()
        {
            if (_specs.Count == 0)
                throw new InvalidOperationException("No enemy specs available to create random enemy");

            var keys = new List<string>(_specs.Keys);
            string choice = keys[_random.Next(keys.Count)];
            return Create(choice);
        }

        /// <summary>
        /// 利用可能な敵のキー一覧を取得する（テスト用）
        /// </summary>
        public IReadOnlyCollection<string> GetAvailableEnemyKeys()
        {
            return _specs.Keys;
        }
    }

}
