using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Mappers
{
    /// <summary>
    /// Player/EnemyからDTOへの変換拡張メソッド
    /// </summary>
    public static class GameStateMapper
    {
        /// <summary>
        /// IPlayerからPlayerStateへの変換（GetSaveData()を経由せず直接プロパティから変換）
        /// </summary>
        public static PlayerState ToPlayerState(this IPlayer player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            return new PlayerState
            {
                Name = player.Name,
                HP = player.HP,
                MaxHP = player.MaxHP,
                Level = player.Level,
                Experience = player.TotalExperience,
                Gold = player.ReturnTotalGold(),
                Potions = player.ReturnTotalPotions(),
                EquippedWeapon = player.EquippedWeaponName,
                IsAlive = player.IsAlive,
                AttackPower = player.AP,
                DefensePower = player.DP
            };
        }

        /// <summary>
        /// IEnemyからEnemyStateへの変換
        /// </summary>
        public static EnemyState ToEnemyState(this IEnemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            return new EnemyState
            {
                Name = enemy.Name,
                HP = enemy.HP,
                MaxHP = enemy.MaxHP,
                IsAlive = enemy.IsAlive,
                AttackStrategy = enemy.AttackStrategy?.GetAttackStrategyName() ?? "Unknown"
            };
        }

        /// <summary>
        /// IWeaponからWeaponInfoへの変換
        /// </summary>
        public static WeaponInfo ToWeaponInfo(this IWeapon weapon, int price = 0)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));

            return new WeaponInfo
            {
                Name = weapon.Name,
                AttackPower = weapon.AP,
                DefensePower = weapon.DP,
                Price = price
            };
        }

        /// <summary>
        /// 戦闘開始時のBattleState作成
        /// </summary>
        public static BattleState CreateInitialBattleState()
        {
            return new BattleState
            {
                TurnNumber = 0,
                AvailableStrategies = new List<string>(AttackStrategyNames.All),
                LastPlayerAction = null,
                LastDamageDealt = 0,
                LastDamageTaken = 0,
                PlayerWon = false,
                BattleEnded = false
            };
        }

        /// <summary>
        /// ショップ開始時のShopState作成
        /// </summary>
        public static ShopState CreateInitialShopState(int potionPrice = 50)
        {
            return new ShopState
            {
                AvailableItems = new List<ShopItem>
                {
                    new ShopItem
                    {
                        Name = "Potion",
                        Price = potionPrice,
                        Type = "Consumable",
                        Description = "Restores 50 HP"
                    }
                },
                AvailableWeapons = new List<WeaponInfo>
                {
                },
                PotionPrice = potionPrice
            };
        }

        /// <summary>
        /// 空のGameStateを作成
        /// </summary>
        public static GameState CreateEmptyGameState()
        {
            return new GameState
            {
                Player = new PlayerState(),
                CurrentEnemy = null,
                CurrentBattle = null,
                CurrentShop = null,
                Messages = new List<GameMessage>(),
                Phase = GamePhase.Initialization,
                IsGameOver = false
            };
        }

        /// <summary>
        /// GameMessageを作成するヘルパーメソッド
        /// </summary>
        public static GameMessage CreateMessage(string text, MessageType type)
        {
            return new GameMessage
            {
                Text = text,
                Type = type,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 複数のメッセージをまとめて作成
        /// </summary>
        public static List<GameMessage> CreateMessages(params (string text, MessageType type)[] messages)
        {
            var result = new List<GameMessage>();
            foreach (var (text, type) in messages)
            {
                result.Add(CreateMessage(text, type));
            }
            return result;
        }
    }
}
