using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace GameEngine.Tests.TestDoubles
{
    /// <summary>
    /// 台本化した行動を順に返す <see cref="IGameInput"/> 実装。
    /// キューが尽きた場合は安全な既定（退店/スキップ/終了）を返し、駆動ループが必ず停止するようにする。
    /// </summary>
    public sealed class ScriptedGameInput : IGameInput
    {
        private readonly Queue<AttackAction> _attacks;
        private readonly Queue<ShopAction> _shops;
        private readonly Queue<UseItemAction?> _rests;
        private readonly Queue<GameActionChoice> _gameActions;

        public ScriptedGameInput(
            IEnumerable<AttackAction>? attacks = null,
            IEnumerable<ShopAction>? shops = null,
            IEnumerable<UseItemAction?>? rests = null,
            IEnumerable<GameActionChoice>? gameActions = null)
        {
            _attacks = new Queue<AttackAction>(attacks ?? Array.Empty<AttackAction>());
            _shops = new Queue<ShopAction>(shops ?? Array.Empty<ShopAction>());
            _rests = new Queue<UseItemAction?>(rests ?? Array.Empty<UseItemAction?>());
            _gameActions = new Queue<GameActionChoice>(gameActions ?? Array.Empty<GameActionChoice>());
        }

        public AttackAction SelectAttackAction(BattleState battleState, PlayerState playerState, EnemyState enemyState)
            => _attacks.Count > 0 ? _attacks.Dequeue() : new AttackAction(AttackStrategyNames.Default);

        public ShopAction SelectShopAction(ShopState shopState, PlayerState playerState)
            => _shops.Count > 0 ? _shops.Dequeue() : new ShopAction(ShopActionType.Exit);

        public UseItemAction? SelectRestAction(PlayerState playerState)
            => _rests.Count > 0 ? _rests.Dequeue() : null;

        public GameActionChoice SelectGameAction()
            => _gameActions.Count > 0 ? _gameActions.Dequeue() : GameActionChoice.Quit;
    }
}
