using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// ショップ状態。1回の <c>Step</c> でショップ行動を1つ処理する。Exit 受信まで自己ループ
    /// （<see cref="Trigger.Repeat"/>）し、退店で休憩へ遷移する。ショップ画面の描画は
    /// ホスト側 <c>IGameInput.SelectShopAction</c> が担うため、ここでは <c>Prepare</c> を行わない。
    /// </summary>
    public class ShoppingState : IGameState
    {
        public string Name => "Shop";
        public ExpectedInput ExpectedInput => ExpectedInput.Shop;

        public Trigger Execute(GameFlowContext context)
        {
            var action = context.CurrentInput.Shop ?? new ShopAction(ShopActionType.Exit);
            var result = context.EventManager.SubmitShopAction(action);
            context.RenderMessages(result.Messages);

            if (result.Exited)
            {
                context.WriteLine($"Status - {context.Player.Name}: {context.Player.HP} HP");
                return Trigger.Continue; // → RestState
            }

            return Trigger.Repeat; // 買い物継続
        }
    }
}
