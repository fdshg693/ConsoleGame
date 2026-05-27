using GameEngine.Api.Hosting;
using GameEngine.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GameEngine.Api.Controllers
{
    /// <summary>
    /// プレイヤー単位の確定セーブ（<see cref="PlayerSaveData"/>）の一覧・削除。
    /// いずれもリポジトリ（MongoDB）未設定時は 503 を返す。
    /// </summary>
    [ApiController]
    [Route("api/players")]
    [Produces("application/json")]
    public sealed class PlayersController : ControllerBase
    {
        private readonly GameSessionManager _manager;

        public PlayersController(GameSessionManager manager)
        {
            _manager = manager;
        }

        /// <summary>指定プレイヤーのセーブ一覧を取得する。</summary>
        [HttpGet("{name}/saves")]
        [ProducesResponseType(typeof(List<PlayerSaveData>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<List<PlayerSaveData>>> GetSaves(string name)
        {
            try
            {
                var saves = await _manager.GetSaveListAsync(name);
                if (saves == null)
                {
                    return SaveUnavailable();
                }
                return Ok(saves);
            }
            catch (Exception ex)
            {
                return SaveBackendError(ex);
            }
        }

        /// <summary>指定プレイヤーの特定スロットのセーブを削除する。</summary>
        [HttpDelete("{name}/saves/{slot}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> DeleteSave(string name, string slot)
        {
            try
            {
                bool? ok = await _manager.DeleteSaveAsync(name, slot);
                if (ok == null)
                {
                    return SaveUnavailable();
                }
                return Ok(new { ok = ok.Value });
            }
            catch (Exception ex)
            {
                return SaveBackendError(ex);
            }
        }

        private ObjectResult SaveUnavailable() =>
            StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Save/load is unavailable. The player repository (MongoDB) is not configured." });

        private ObjectResult SaveBackendError(Exception ex) =>
            StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = $"Save backend is unreachable: {ex.Message}" });
    }
}
