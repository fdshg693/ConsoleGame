using YamlDotNet.Serialization;

namespace GameEngine.Configuration
{
    /// <summary>
    /// ゲーム設定を表すクラス
    /// </summary>
    public class GameConfig
    {
        public MongoDBConfig MongoDB { get; set; } = new();
        public PlayerConfig Player { get; set; } = new();
        public LevelUpConfig LevelUp { get; set; } = new();
        public ItemsConfig Items { get; set; } = new();
        public EventsConfig Events { get; set; } = new();
        public ShopConfig Shop { get; set; } = new();
        public EnemyConfig Enemy { get; set; } = new();
        public Dictionary<string, WeaponStats> Weapons { get; set; } = new();
    }

    public class MongoDBConfig
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "GameEngineDB";
        public string CollectionName { get; set; } = "PlayerSaves";
    }

    public class PlayerConfig
    {
        public int InitialHP { get; set; }
        public int BaseDP { get; set; }
        public int BaseAP { get; set; }
        public int InitialGold { get; set; }
        public int InitialPotions { get; set; }
    }

    public class LevelUpConfig
    {
        public int HPIncrease { get; set; }
        public int DPIncrease { get; set; }
        public int APIncrease { get; set; }
        public int ExperienceRequired { get; set; }
    }

    public class ItemsConfig
    {
        public PotionConfig Potion { get; set; } = new();
    }

    public class PotionConfig
    {
        public int Price { get; set; }
        public int HealAmount { get; set; }
    }

    public class EventsConfig
    {
        public int ShopEventWeight { get; set; }
        public int BattleEventWeight { get; set; }
        
        public int TotalWeight => ShopEventWeight + BattleEventWeight;
    }

    public class ShopConfig
    {
        public int GoldRewardMin { get; set; }
        public int GoldRewardMax { get; set; }
    }

    public class EnemyConfig
    {
        public int GoldBaseMultiplier { get; set; }
        public int GoldRandomMin { get; set; }
        public int GoldRandomMax { get; set; }
    }

    public class WeaponStats
    {
        public int HP { get; set; }
        public int AP { get; set; }
        public int DP { get; set; }
    }

    /// <summary>
    /// ゲーム設定を読み込むクラス
    /// </summary>
    public static class GameConfigLoader
    {
        private static GameConfig? _instance;
        private static readonly object _lock = new object();
        private const string DefaultConfigPath = "./GameEngine/game-config.yml";

        /// <summary>
        /// ゲーム設定のシングルトンインスタンスを取得
        /// </summary>
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadConfig(DefaultConfigPath);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// YAMLファイルから設定を読み込む
        /// </summary>
        private static GameConfig LoadConfig(string configPath)
        {
            try
            {
                // 絶対パスを取得してログ出力
                string absolutePath = Path.GetFullPath(configPath);
                string currentDirectory = Directory.GetCurrentDirectory();
                
                Console.WriteLine($"[DEBUG] Current directory: {currentDirectory}");
                Console.WriteLine($"[DEBUG] Config relative path: {configPath}");
                Console.WriteLine($"[DEBUG] Config absolute path: {absolutePath}");
                
                // ファイル存在チェック
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"Config file not found at: {configPath}");
                    Console.WriteLine($"Absolute path tried: {absolutePath}");
                    Console.WriteLine("Using default values.");
                    return CreateDefaultConfig();
                }

                // YAML読み込み
                string yaml = File.ReadAllText(configPath);
                
                if (string.IsNullOrWhiteSpace(yaml))
                {
                    Console.WriteLine($"Config file is empty: {configPath}. Using default values.");
                    return CreateDefaultConfig();
                }

                var deserializer = new DeserializerBuilder().Build();
                var config = deserializer.Deserialize<GameConfig>(yaml);

                if (config == null)
                {
                    Console.WriteLine($"Failed to parse config file: {configPath}. Using default values.");
                    return CreateDefaultConfig();
                }

                ValidateConfig(config);
                Console.WriteLine($"Successfully loaded game configuration from {configPath}");
                return config;
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                Console.WriteLine($"YAML parse error in {configPath}: {ex.Message}");
                Console.WriteLine("Using default configuration values.");
                return CreateDefaultConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config from {configPath}: {ex.Message}");
                Console.WriteLine("Using default configuration values.");
                return CreateDefaultConfig();
            }
        }

        /// <summary>
        /// デフォルト設定を作成
        /// </summary>
        private static GameConfig CreateDefaultConfig()
        {
            return new GameConfig
            {
                Player = new PlayerConfig
                {
                    InitialHP = 100,
                    BaseDP = 5,
                    BaseAP = 10,
                    InitialGold = 50,
                    InitialPotions = 0
                },
                LevelUp = new LevelUpConfig
                {
                    HPIncrease = 10,
                    DPIncrease = 1,
                    APIncrease = 2,
                    ExperienceRequired = 100
                },
                Items = new ItemsConfig
                {
                    Potion = new PotionConfig
                    {
                        Price = 10,
                        HealAmount = 10
                    }
                },
                Events = new EventsConfig
                {
                    ShopEventWeight = 1,
                    BattleEventWeight = 2
                },
                Shop = new ShopConfig
                {
                    GoldRewardMin = 10,
                    GoldRewardMax = 20
                },
                Enemy = new EnemyConfig
                {
                    GoldBaseMultiplier = 2,
                    GoldRandomMin = 1,
                    GoldRandomMax = 10
                },
                Weapons = new Dictionary<string, WeaponStats>
                {
                    ["Sword"] = new WeaponStats { HP = 100, AP = 20, DP = 5 },
                    ["Axe"] = new WeaponStats { HP = 80, AP = 30, DP = 3 },
                    ["Spear"] = new WeaponStats { HP = 90, AP = 25, DP = 4 },
                    ["Bow"] = new WeaponStats { HP = 70, AP = 35, DP = 2 },
                    ["Staff"] = new WeaponStats { HP = 60, AP = 40, DP = 1 }
                }
            };
        }

        /// <summary>
        /// 設定の妥当性を検証
        /// </summary>
        private static void ValidateConfig(GameConfig config)
        {
            var errors = new List<string>();

            // プレイヤー設定の検証
            if (config.Player.InitialHP <= 0)
                errors.Add($"Player.InitialHP must be positive (got {config.Player.InitialHP})");
            
            if (config.Player.BaseAP < 0)
                errors.Add($"Player.BaseAP cannot be negative (got {config.Player.BaseAP})");
            
            if (config.Player.BaseDP < 0)
                errors.Add($"Player.BaseDP cannot be negative (got {config.Player.BaseDP})");

            // レベルアップ設定の検証
            if (config.LevelUp.ExperienceRequired <= 0)
                errors.Add($"LevelUp.ExperienceRequired must be positive (got {config.LevelUp.ExperienceRequired})");

            // イベント重みの検証
            if (config.Events.ShopEventWeight < 0)
                errors.Add($"Events.ShopEventWeight cannot be negative (got {config.Events.ShopEventWeight})");
            
            if (config.Events.BattleEventWeight < 0)
                errors.Add($"Events.BattleEventWeight cannot be negative (got {config.Events.BattleEventWeight})");
            
            if (config.Events.TotalWeight == 0)
                errors.Add("Events.TotalWeight cannot be zero");

            // ショップ設定の検証
            if (config.Shop.GoldRewardMin < 0)
                errors.Add($"Shop.GoldRewardMin cannot be negative (got {config.Shop.GoldRewardMin})");
            
            if (config.Shop.GoldRewardMax < config.Shop.GoldRewardMin)
                errors.Add($"Shop.GoldRewardMax must be >= GoldRewardMin");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Invalid game configuration:\n  - {string.Join("\n  - ", errors)}");
            }
        }

        /// <summary>
        /// テスト用：設定を再読み込みする
        /// </summary>
        public static void ReloadConfig(string configPath = DefaultConfigPath)
        {
            lock (_lock)
            {
                _instance = LoadConfig(configPath);
            }
        }
    }
}
