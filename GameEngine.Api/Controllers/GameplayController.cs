using GameEngine.Api.Hosting;
using GameEngine.Api.Mappers;
using GameEngine.Contracts;
using GameEngine.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GameEngine.Api.Controllers
{
    /// <summary>
    /// 進行中セッションを1行動＝1ステップで駆動する。各エンドポイントは特定の <see cref="ExpectedInput"/> に対応し、
    /// 現在のセッションがその入力を期待していない場合は 409 を返す。
    /// </summary>
    /// <remarks>
    /// エンジンは探索（Explore）を自動前進してエンカウントに入るため、セッション開始（<c>POST /api/sessions</c>）や
    /// <c>continue</c> 後は即座に戦闘/ショップ状態になる。クライアントはレスポンスの <see cref="ExpectedInput"/> を見て
    /// 次に叩くエンドポイントを決める: <c>Attack</c>→<c>battle/turn</c>、<c>Shop</c>→<c>shop/action</c>、
    /// <c>Rest</c>→<c>rest</c>、<c>GameAction</c>→<c>continue</c>。
    /// </remarks>
    [ApiController]
    [Route("api/sessions/{id}")]
    [Produces("application/json")]
    public sealed class GameplayController : ControllerBase
    {
        private readonly GameSessionManager _manager;

        public GameplayController(GameSessionManager manager)
        {
            _manager = manager;
        }

        /// <summary>戦闘を1ターン進める（<c>ExpectedInput=Attack</c>）。</summary>
        [HttpPost("battle/turn")]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<GameStateResponse> BattleTurn(string id, [FromBody] AttackAction? action)
        {
            var session = _manager.Get(id);
            if (session == null)
            {
                return SessionNotFound(id);
            }

            if (action == null)
            {
                return BadRequest(new { error = "An AttackAction body is required (e.g. { \"strategyName\": \"Melee\" })." });
            }

            if (!PlayerActionValidator.IsValid(action, out var error))
            {
                return BadRequest(new { error });
            }

            lock (session.SyncRoot)
            {
                if (RequireExpected(session, ExpectedInput.Attack, out var conflict))
                {
                    return conflict!;
                }
                session.Step(PlayerInput.ForAttack(action));
                return Respond(session);
            }
        }

        /// <summary>ショップで1アクション処理する（<c>ExpectedInput=Shop</c>）。<c>Exit</c> を送るまで繰り返し叩く。</summary>
        [HttpPost("shop/action")]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<GameStateResponse> ShopAction(string id, [FromBody] ShopAction? action)
        {
            var session = _manager.Get(id);
            if (session == null)
            {
                return SessionNotFound(id);
            }

            if (action == null)
            {
                return BadRequest(new { error = "A ShopAction body is required (e.g. { \"shopType\": \"BuyPotion\", \"quantity\": 1 })." });
            }

            // Exit は数量を使わない。検証（Quantity > 0）を満たすため、未指定/0 を 1 に正規化する
            // （購入アクションの数量チェックは厳格なまま）。
            if (action.ShopType == ShopActionType.Exit && action.Quantity <= 0)
            {
                action.Quantity = 1;
            }

            if (!PlayerActionValidator.IsValid(action, out var error))
            {
                return BadRequest(new { error });
            }

            lock (session.SyncRoot)
            {
                if (RequireExpected(session, ExpectedInput.Shop, out var conflict))
                {
                    return conflict!;
                }
                session.Step(PlayerInput.ForShop(action));
                return Respond(session);
            }
        }

        /// <summary>休憩でアイテムを使用する（<c>ExpectedInput=Rest</c>）。ボディ省略/ null はスキップを意味する。</summary>
        [HttpPost("rest")]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<GameStateResponse> Rest(string id, [FromBody] UseItemAction? action)
        {
            var session = _manager.Get(id);
            if (session == null)
            {
                return SessionNotFound(id);
            }

            if (action != null && !PlayerActionValidator.IsValid(action, out var error))
            {
                return BadRequest(new { error });
            }

            lock (session.SyncRoot)
            {
                if (RequireExpected(session, ExpectedInput.Rest, out var conflict))
                {
                    return conflict!;
                }
                session.Step(PlayerInput.ForRest(action));
                return Respond(session);
            }
        }

        /// <summary>エンカウント後の進行を選ぶ（<c>ExpectedInput=GameAction</c>）。Continue は次のエンカウントへ、Quit 系は終了。</summary>
        [HttpPost("continue")]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<GameStateResponse> Continue(string id, [FromBody] ContinueRequest? request)
        {
            var session = _manager.Get(id);
            if (session == null)
            {
                return SessionNotFound(id);
            }

            var choice = request?.Action ?? GameActionChoice.Continue;

            lock (session.SyncRoot)
            {
                if (RequireExpected(session, ExpectedInput.GameAction, out var conflict))
                {
                    return conflict!;
                }
                session.Step(PlayerInput.ForProgress(choice));
                return Respond(session);
            }
        }

        /// <summary>ステップ適用後の状態を 200 で返す（<see cref="ApiGameSession.SyncRoot"/> ロック内で呼ぶこと）。</summary>
        private OkObjectResult Respond(ApiGameSession session)
        {
            session.Touch(DateTime.UtcNow);
            return Ok(ApiResponseMapper.ToResponse(session));
        }

        /// <summary>
        /// セッションが <paramref name="required"/> の入力を期待しているか検証する。
        /// 不一致なら <paramref name="conflict"/> に 409 を設定して true を返す。
        /// </summary>
        private bool RequireExpected(ApiGameSession session, ExpectedInput required, out ActionResult<GameStateResponse>? conflict)
        {
            if (session.ExpectedInput == required)
            {
                conflict = null;
                return false;
            }

            conflict = Conflict(new
            {
                error = $"Session expects '{session.ExpectedInput}' input, not '{required}'.",
                expectedInput = session.ExpectedInput.ToString(),
                currentStateName = session.GameSystem.CurrentStateName,
                isGameOver = !session.IsRunning
            });
            return true;
        }

        private NotFoundObjectResult SessionNotFound(string id) =>
            NotFound(new { error = $"Session '{id}' not found or expired." });
    }
}
