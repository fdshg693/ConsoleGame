using GameEngine.Api.Contracts;
using GameEngine.Api.Hosting;
using GameEngine.DTOs;

namespace GameEngine.Api.Mappers
{
    /// <summary>
    /// <see cref="ApiGameSession"/> の現在状態を <see cref="GameStateResponse"/> へ写像する。
    /// </summary>
    public static class ApiResponseMapper
    {
        /// <summary>
        /// セッションの現在状態をレスポンス DTO に変換し、蓄積メッセージを回収する（破壊的: <see cref="BufferingRenderer.DrainMessages"/> を呼ぶ）。
        /// 同一セッションへの並行アクセスと混ざらないよう、呼び出し側は <see cref="ApiGameSession.SyncRoot"/> をロックすること。
        /// </summary>
        public static GameStateResponse ToResponse(ApiGameSession session)
        {
            var gameSystem = session.GameSystem;

            return new GameStateResponse
            {
                SessionId = session.SessionId,
                Player = gameSystem.CurrentPlayerState,
                CurrentEnemy = gameSystem.CurrentEnemyState,
                CurrentBattle = gameSystem.CurrentBattleState,
                CurrentShop = gameSystem.CurrentShopState,
                Messages = session.Renderer.DrainMessages(),
                Phase = MapPhase(gameSystem.CurrentStateName),
                CurrentStateName = gameSystem.CurrentStateName,
                ExpectedInput = gameSystem.ExpectedInput,
                IsRunning = gameSystem.IsRunning,
                IsGameOver = !gameSystem.IsRunning
            };
        }

        /// <summary>
        /// ステートマシンのステート名を UI フェーズへ写像する（<c>GameSystem.MapPhase</c> と同じ規則）。
        /// </summary>
        private static GamePhase MapPhase(string? stateName) => stateName switch
        {
            "Start" or "Explore" or "PostEncounter" => GamePhase.Exploration,
            "Battle" => GamePhase.Battle,
            "Shop" => GamePhase.Shop,
            "Rest" => GamePhase.Rest,
            _ => GamePhase.GameOver, // "GameOver" / null（終了後）
        };
    }
}
