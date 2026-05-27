using GameEngine.Constants;
using GameEngine.DTOs;

namespace GameEngine.Systems.StateMachine
{
    /// <summary>
    /// 戦闘ターン状態。1回の <c>Step</c> で攻撃行動を1つ受け取り、プレイヤー+敵の1ターンを進める。
    /// 戦闘継続なら自己ループ（<see cref="Trigger.Repeat"/>）、勝利は休憩へ、敗北はゲームオーバーへ遷移する。
    /// </summary>
    public class BattleTurnState : IGameState
    {
        public string Name => "Battle";
        public ExpectedInput ExpectedInput => ExpectedInput.Attack;

        /// <summary>攻撃入力を受け取る前に、ターン開始の見出しと現在のステータスパネルを描画する。</summary>
        public void Prepare(GameFlowContext context)
        {
            var result = context.EventManager.CurrentBattleResult;
            if (result == null)
            {
                return;
            }

            int upcomingTurn = (result.Battle?.TurnNumber ?? 0) + 1;
            context.ClearScreen($"BATTLE - Turn {upcomingTurn}");

            if (result.Enemy != null)
            {
                context.Renderer.RenderStatusPanel(result.Player, result.Enemy);
            }
        }

        public Trigger Execute(GameFlowContext context)
        {
            var action = context.CurrentInput.Attack ?? new AttackAction(AttackStrategyNames.Default);
            var result = context.EventManager.SubmitBattleTurn(action);
            context.RenderMessages(result.Messages);

            if (result.IsVictory)
            {
                context.Renderer.WriteResultBox("VICTORY!", new[] { $"{result.Enemy?.Name} has been defeated!" }, true);
                context.Renderer.WaitForKeyPress();
                return Trigger.Continue; // → RestState
            }

            if (result.IsDefeat)
            {
                context.Renderer.WriteResultBox("DEFEAT", new[] { $"{result.Player.Name} has fallen..." }, false);
                context.Renderer.WaitForKeyPress();
                return Trigger.EndGame; // → GameOverState
            }

            if (result.IsError)
            {
                return Trigger.EndGame; // 異常時はゲームオーバーへ退避
            }

            return Trigger.Repeat; // 戦闘継続（次ターン）
        }
    }
}
