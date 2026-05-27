using GameEngine.DTOs;

namespace GameEngine.Interfaces
{
    /// <summary>
    /// UI層から入力を受け取るためのインターフェース
    /// </summary>
    public interface IGameInput
    {
        AttackAction SelectAttackAction(BattleState battleState, PlayerState playerState, EnemyState enemyState);
        ShopAction SelectShopAction(ShopState shopState, PlayerState playerState);
        UseItemAction? SelectRestAction(PlayerState playerState);
    }
}
