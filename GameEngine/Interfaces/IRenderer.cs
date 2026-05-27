using GameEngine.DTOs;
using GameEngine.Models;

namespace GameEngine.Interfaces
{
    /// <summary>
    /// 出力（描画）の抽象。コアは <see cref="System.Console"/> に直接依存せず、本インターフェース経由で表示する。
    /// コンソールホストは ANSI ベースの実装を、API ホストはバッファ/DTO へ蓄積する実装を提供する。
    /// </summary>
    /// <remarks>
    /// 公開するのはコア（<c>GameSystem</c> / <c>BattleManager</c> / StateMachine / <c>GameFlowContext</c>）が
    /// 実際に呼び出すメソッドのみ。矢印キー選択や装飾ボックスなどコンソール固有の描画は実装側に閉じ込める。
    /// </remarks>
    public interface IRenderer
    {
        /// <summary>画面をクリアし、タイトルヘッダーを描画する。</summary>
        void ClearScreen(string title);

        /// <summary>任意のキー入力まで待機する（API 実装では何もしない想定）。</summary>
        void WaitForKeyPress(string prompt = "Press any key to continue...");

        /// <summary>単一のゲームメッセージを種別に応じて描画する。</summary>
        void RenderMessage(GameMessage message);

        /// <summary>複数のゲームメッセージを描画する。</summary>
        void RenderMessages(IEnumerable<GameMessage> messages);

        void WriteInfo(string text);
        void WriteSuccess(string text);
        void WriteWarning(string text);
        void WriteError(string text);
        void WriteSystem(string text);

        /// <summary>HP バーを描画する。</summary>
        void RenderHPBar(string name, int current, int max, int barWidth = 20);

        /// <summary>プレイヤー（および任意で敵）のステータスパネルを描画する。</summary>
        void RenderStatusPanel(PlayerState player, EnemyState? enemy = null);

        /// <summary>区切り線を描画する。</summary>
        void WriteSeparator();

        /// <summary>結果（勝利/敗北など）の強調ボックスを描画する。</summary>
        void WriteResultBox(string title, string[] details, bool isVictory);
    }
}
