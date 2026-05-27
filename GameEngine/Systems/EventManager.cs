using GameEngine.Configuration;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;
using GameEngine.Systems.BattleSystem;

namespace GameEngine.Systems
{
    /// <summary>
    /// エンカウント（戦闘/ショップ）をステップ駆動で進める。内部 while ループ・入力待ち・描画は持たない。
    /// <see cref="BeginEncounter"/> で種別を決定して状態を保持し、ショップ/戦闘/休憩の各 Submit を
    /// 外部から1アクションずつ呼び出すことで進行する。
    /// </summary>
    public class EventManager
    {
        private readonly IPlayer _player;
        private readonly GameConfig _config;
        private readonly BattleManager _battleManager;
        private readonly IGameRecord _gameRecord;
        private readonly Random _random;
        private readonly int _potionPrice;

        private GameEventType _currentType;
        private ShopState? _shopState;
        private BattleStepResult? _battleResult;

        public EventManager(IPlayer player, GameConfig config, IEnemyFactory enemyFactory, IGameRecord gameRecord, Random? random = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (enemyFactory == null)
                throw new ArgumentNullException(nameof(enemyFactory));
            _gameRecord = gameRecord ?? throw new ArgumentNullException(nameof(gameRecord));

            _battleManager = new BattleManager(_player, enemyFactory, _gameRecord);
            _random = random ?? new Random();
            _potionPrice = _config.Items.Potion.Price;
        }

        /// <summary>勝敗記録。ゲームオーバー表示やセッション捕捉のために公開する。</summary>
        public IGameRecord GameRecord => _gameRecord;

        /// <summary>直近に決定したエンカウント種別。</summary>
        public GameEventType CurrentEventType => _currentType;

        /// <summary>進行中のショップ状態（戦闘時は null）。</summary>
        public ShopState? CurrentShopState => _shopState;

        /// <summary>直近の戦闘ステップ結果（ショップ時は null）。</summary>
        public BattleStepResult? CurrentBattleResult => _battleResult;

        /// <summary>進行中の戦闘の敵（戦闘外は null）。</summary>
        public IEnemy? CurrentEnemy => _battleManager.CurrentEnemy;

        /// <summary>
        /// 次のエンカウントを開始する。種別を抽選して保持し、ショップなら発見ボーナス付与＋ショップ状態生成、
        /// 戦闘なら敵を生成して初期戦闘状態を返す。
        /// </summary>
        public EncounterStart BeginEncounter()
        {
            var messages = new List<GameMessage>();
            _currentType = DetermineEventType();
            _shopState = null;
            _battleResult = null;

            if (_currentType == GameEventType.Shop)
            {
                messages.Add(GameStateMapper.CreateMessage("=== You found a shop! ===", MessageType.System));

                int goldReward = _random.Next(
                    _config.Shop.GoldRewardMin,
                    _config.Shop.GoldRewardMax + 1);
                _player.GainGold(goldReward);
                messages.Add(GameStateMapper.CreateMessage($"You received {goldReward} gold as a discovery bonus!", MessageType.Gold));

                _shopState = ShopSystem.CreateShopState(_potionPrice);
                return new EncounterStart(GameEventType.Shop, messages, _shopState, null);
            }
            else
            {
                messages.Add(GameStateMapper.CreateMessage("=== You encounter a wild enemy! ===", MessageType.System));

                _battleResult = _battleManager.StartBattle();
                messages.AddRange(_battleResult.Messages);
                return new EncounterStart(GameEventType.Battle, messages, null, _battleResult);
            }
        }

        /// <summary>
        /// ショップで1アクションを処理する。終了判定（Exit 受信）は <see cref="ShopActionResult.Exited"/> で返し、
        /// ループ継続は呼び出し側（State/ホスト）に委ねる。
        /// </summary>
        public ShopActionResult SubmitShopAction(ShopAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var messages = ShopSystem.ProcessShopAction(_player, action, _potionPrice);
            bool exited = action.ShopType == ShopActionType.Exit;
            return new ShopActionResult(messages, _shopState, _player.ToPlayerState(), exited);
        }

        /// <summary>
        /// 戦闘の1ターンを進める（<see cref="BattleManager.SubmitPlayerTurn"/> に委譲）。
        /// </summary>
        public BattleStepResult SubmitBattleTurn(AttackAction action)
        {
            _battleResult = _battleManager.SubmitPlayerTurn(action);
            return _battleResult;
        }

        /// <summary>
        /// 休憩でのアイテム使用を1アクション処理する（null はスキップ）。
        /// </summary>
        public List<GameMessage> SubmitRestAction(UseItemAction? action)
        {
            return RestSystem.ProcessRestAction(_player, action);
        }

        /// <summary>
        /// 発生するイベントタイプを決定する（重み付き抽選）。
        /// </summary>
        private GameEventType DetermineEventType()
        {
            int totalWeight = _config.Events.TotalWeight;
            int roll = _random.Next(0, totalWeight);

            return roll < _config.Events.ShopEventWeight
                ? GameEventType.Shop
                : GameEventType.Battle;
        }
    }

    /// <summary>
    /// <see cref="EventManager.BeginEncounter"/> の結果。決定した種別と初期メッセージ、
    /// および種別に応じた初期状態（ショップ状態 or 初期戦闘状態）を保持する。
    /// </summary>
    public class EncounterStart
    {
        public GameEventType Type { get; }
        public IReadOnlyList<GameMessage> Messages { get; }
        public ShopState? Shop { get; }
        public BattleStepResult? Battle { get; }

        public EncounterStart(
            GameEventType type,
            IReadOnlyList<GameMessage> messages,
            ShopState? shop,
            BattleStepResult? battle)
        {
            Type = type;
            Messages = messages ?? Array.Empty<GameMessage>();
            Shop = shop;
            Battle = battle;
        }

        public bool IsShop => Type == GameEventType.Shop;
        public bool IsBattle => Type == GameEventType.Battle;
    }

    /// <summary>
    /// <see cref="EventManager.SubmitShopAction"/> の結果。メッセージ・更新後の状態・退店フラグを保持する。
    /// </summary>
    public class ShopActionResult
    {
        public IReadOnlyList<GameMessage> Messages { get; }
        public ShopState? Shop { get; }
        public PlayerState Player { get; }
        public bool Exited { get; }

        public ShopActionResult(
            IReadOnlyList<GameMessage> messages,
            ShopState? shop,
            PlayerState player,
            bool exited)
        {
            Messages = messages ?? Array.Empty<GameMessage>();
            Shop = shop;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Exited = exited;
        }
    }

    /// <summary>
    /// ゲームイベントの種類を表す列挙型
    /// </summary>
    public enum GameEventType
    {
        Shop,
        Battle,
        Treasure,  // 将来の拡張用
        Rest       // 将来の拡張用
    }
}
