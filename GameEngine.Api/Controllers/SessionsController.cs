using GameEngine.Api.Contracts;
using GameEngine.Api.Hosting;
using GameEngine.Api.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace GameEngine.Api.Controllers
{
    /// <summary>
    /// セッションのライフサイクル（開始/取得/終了）と確定セーブ/ロードを扱う。
    /// 進行（戦闘/ショップ/休憩/続行）は <see cref="GameplayController"/> を参照。
    /// </summary>
    [ApiController]
    [Route("api/sessions")]
    [Produces("application/json")]
    public sealed class SessionsController : ControllerBase
    {
        private readonly GameSessionManager _manager;

        public SessionsController(GameSessionManager manager)
        {
            _manager = manager;
        }

        /// <summary>新規ゲームを開始する。レスポンスは最初のエンカウント（戦闘 or ショップ）まで前進済み。</summary>
        [HttpPost]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status201Created)]
        public ActionResult<GameStateResponse> Create([FromBody] CreateSessionRequest? request)
        {
            var session = _manager.CreateNew(request?.PlayerName);
            GameStateResponse response;
            lock (session.SyncRoot)
            {
                response = ApiResponseMapper.ToResponse(session);
            }
            return CreatedAtAction(nameof(Get), new { id = session.SessionId }, response);
        }

        /// <summary>現在のゲーム状態を取得する。</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateResponse> Get(string id)
        {
            var session = _manager.Get(id);
            if (session == null)
            {
                return SessionNotFound(id);
            }

            lock (session.SyncRoot)
            {
                session.Touch(DateTime.UtcNow);
                return Ok(ApiResponseMapper.ToResponse(session));
            }
        }

        /// <summary>セッションを終了して破棄する。</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(string id)
        {
            return _manager.Remove(id) ? NoContent() : SessionNotFound(id);
        }

        /// <summary>現在のプレイヤーを確定セーブする（セッションは継続）。</summary>
        [HttpPost("{id}/save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Save(string id, [FromBody] SaveRequest? request)
        {
            var session = _manager.Get(id);
            if (session == null)
            {
                return SessionNotFound(id);
            }

            string slot = string.IsNullOrWhiteSpace(request?.SlotName) ? "auto_save" : request!.SlotName!.Trim();
            try
            {
                bool? ok = await _manager.SaveAsync(session, slot);
                if (ok == null)
                {
                    return SaveUnavailable();
                }
                return Ok(new { ok = ok.Value, slotName = slot });
            }
            catch (Exception ex)
            {
                return SaveBackendError(ex);
            }
        }

        /// <summary>
        /// セーブデータからプレイヤーを復元して新規セッションを開始する。
        /// 復元できるのはプレイヤーステータスのみのため、探索（最初のエンカウント）から再開する。
        /// </summary>
        [HttpPost("load")]
        [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<GameStateResponse>> Load([FromBody] LoadRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerName))
            {
                return BadRequest(new { error = "playerName is required." });
            }

            if (!_manager.SaveLoadEnabled)
            {
                return SaveUnavailable();
            }

            string slot = string.IsNullOrWhiteSpace(request.SlotName) ? "auto_save" : request.SlotName!.Trim();
            ApiGameSession? session;
            try
            {
                session = await _manager.CreateFromSaveAsync(request.PlayerName.Trim(), slot);
            }
            catch (Exception ex)
            {
                return SaveBackendError(ex);
            }

            if (session == null)
            {
                return NotFound(new { error = $"No save found for player '{request.PlayerName}' (slot '{slot}')." });
            }

            GameStateResponse response;
            lock (session.SyncRoot)
            {
                response = ApiResponseMapper.ToResponse(session);
            }
            return CreatedAtAction(nameof(Get), new { id = session.SessionId }, response);
        }

        private NotFoundObjectResult SessionNotFound(string id) =>
            NotFound(new { error = $"Session '{id}' not found or expired." });

        private ObjectResult SaveUnavailable() =>
            StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Save/load is unavailable. The player repository (MongoDB) is not configured." });

        private ObjectResult SaveBackendError(Exception ex) =>
            StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = $"Save backend is unreachable: {ex.Message}" });
    }
}
