using GameEngine.Configuration;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;
using GameEngine.Systems.BattleSystem;

namespace GameEngine.Systems
{
    /// <summary>
    /// ゲームイベントの管理を行うクラス
    /// </summary>
    public class EventManager
    {
        private readonly IPlayer _player;
        private readonly BattleManager _battleManager;
        private readonly IGameInput _input;
        private readonly Random _random;
        private readonly GameConfig _config;

        public EventManager(IPlayer player, IGameInput input)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _battleManager = new BattleManager(_player, _input);
            _random = new Random();
            _config = GameConfigLoader.Instance;
        }

        /// <summary>
        /// ランダムイベントを発生させる
        /// </summary>
        /// <returns>プレイヤーが生存している場合はtrue</returns>
        public EventResult TriggerRandomEvent()
        {
            var messages = new List<GameMessage>();
            var eventType = DetermineEventType();

            switch (eventType)
            {
                case GameEventType.Shop:
                    return HandleShopEvent(messages);

                case GameEventType.Battle:
                    return HandleBattleEvent(messages);

                default:
                    messages.Add(GameStateMapper.CreateMessage("Unknown event occurred.", MessageType.Error));
                    return new EventResult(_player.IsAlive, messages);
            }
        }

        /// <summary>
        /// 発生するイベントタイプを決定する
        /// </summary>
        private GameEventType DetermineEventType()
        {
            int totalWeight = _config.Events.TotalWeight;
            int roll = _random.Next(0, totalWeight);

            if (roll < _config.Events.ShopEventWeight)
            {
                return GameEventType.Shop;
            }
            else
            {
                return GameEventType.Battle;
            }
        }

        /// <summary>
        /// ショップイベントを処理する
        /// </summary>
        private EventResult HandleShopEvent(List<GameMessage> messages)
        {
            messages.Add(GameStateMapper.CreateMessage("=== You found a shop! ===", MessageType.System));

            // ゴールド報酬を付与
            int goldReward = _random.Next(
                _config.Shop.GoldRewardMin,
                _config.Shop.GoldRewardMax + 1);

            _player.GainGold(goldReward);
            messages.Add(GameStateMapper.CreateMessage($"You received {goldReward} gold as a discovery bonus!", MessageType.Gold));

            // ショップをループで開く（Exit選択まで繰り返し）
            var shopState = ShopSystem.CreateShopState();
            while (true)
            {
                var playerState = _player.ToPlayerState();
                var shopAction = _input.SelectShopAction(shopState, playerState);
                messages.AddRange(ShopSystem.ProcessShopAction(_player, shopAction));

                if (shopAction.ShopType == ShopActionType.Exit)
                {
                    break;
                }
            }

            // 状態表示
            messages.Add(GameStateMapper.CreateMessage($"Status - {_player.Name}: {_player.HP} HP", MessageType.Info));

            // 回復アイテム使用の機会を提供
            var restAction = _input.SelectRestAction(_player.ToPlayerState());
            messages.AddRange(RestSystem.ProcessRestAction(_player, restAction));

            return new EventResult(_player.IsAlive, messages);
        }

        /// <summary>
        /// 戦闘イベントを処理する
        /// </summary>
        private EventResult HandleBattleEvent(List<GameMessage> messages)
        {
            messages.Add(GameStateMapper.CreateMessage("=== You encounter a wild enemy! ===", MessageType.System));

            var battleResult = _battleManager.StartBattle();
            messages.AddRange(battleResult.Messages);

            // 戦闘後の回復アイテム使用
            if (_player.IsAlive)
            {
                var restAction = _input.SelectRestAction(_player.ToPlayerState());
                messages.AddRange(RestSystem.ProcessRestAction(_player, restAction));
            }

            return new EventResult(_player.IsAlive, messages);
        }
    }

    /// <summary>
    /// イベント結果
    /// </summary>
    public class EventResult
    {
        public bool ContinueGame { get; }
        public IReadOnlyList<GameMessage> Messages { get; }

        public EventResult(bool continueGame, IReadOnlyList<GameMessage> messages)
        {
            ContinueGame = continueGame;
            Messages = messages ?? Array.Empty<GameMessage>();
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
