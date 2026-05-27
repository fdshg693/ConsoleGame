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

        /// <summary>
        /// エンカウント後の進行アクション（続行/セーブ/終了）を選択させる。
        /// コア（<c>GameFlowContext</c>）から呼ばれ、コンソール/API それぞれの実装が解釈する。
        /// </summary>
        GameActionChoice SelectGameAction();
    }
}
