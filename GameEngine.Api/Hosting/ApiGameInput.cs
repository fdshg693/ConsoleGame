using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace GameEngine.Api.Hosting
{
    /// <summary>
    /// API ホスト用の <see cref="IGameInput"/> スタブ。API は <c>GameSystem.RunGameLoop</c> を使わず、
    /// リクエストボディから組み立てた <see cref="PlayerInput"/> を直接 <c>GameSystem.Step</c> へ渡す。
    /// そのため本実装のメソッドは呼ばれない（呼ばれたら設計上のバグ）。
    /// <see cref="GameEngine.Systems.GameSystem"/> のコンストラクタが <see cref="IGameInput"/> を必須とするため、その依存を満たすためだけに存在する。
    /// </summary>
    public sealed class ApiGameInput : IGameInput
    {
        private static InvalidOperationException NotDriven() => new(
            "API host drives GameSystem.Step directly from request bodies; IGameInput must not be invoked. " +
            "RunGameLoop is console-only.");

        public AttackAction SelectAttackAction(BattleState battleState, PlayerState playerState, EnemyState enemyState) => throw NotDriven();
        public ShopAction SelectShopAction(ShopState shopState, PlayerState playerState) => throw NotDriven();
        public UseItemAction? SelectRestAction(PlayerState playerState) => throw NotDriven();
        public GameActionChoice SelectGameAction() => throw NotDriven();
    }
}
