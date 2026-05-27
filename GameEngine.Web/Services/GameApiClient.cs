using System.Net.Http.Json;
using System.Text.Json;
using GameEngine.Contracts;
using GameEngine.DTOs;

namespace GameEngine.Web.Services
{
    /// <summary>
    /// <c>GameEngine.Api</c> のエンドポイントに 1:1 で対応する薄いラッパー。
    /// 各メソッドは更新後の <see cref="GameStateResponse"/>（進行系）か、結果を持たない操作（セーブ/削除）を表す。
    /// 非成功ステータスでは本文の <c>{ "error": ... }</c> を読み取り <see cref="GameApiException"/> を送出する
    /// （呼び出し側が HTTP ステータス別に処理できるようにするため）。enum は API 仕様に合わせ
    /// <see cref="JsonStringEnumConverter"/> 入りの <see cref="JsonSerializerOptions"/> で文字列授受する。
    /// </summary>
    public sealed class GameApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _json;

        public GameApiClient(HttpClient http, JsonSerializerOptions json)
        {
            _http = http;
            _json = json;
        }

        // ---- セッションのライフサイクル（SessionsController） ----

        /// <summary>新規ゲームを開始する（<c>POST /api/sessions</c>）。最初のエンカウントまで前進済みで返る。</summary>
        public async Task<GameStateResponse> CreateSessionAsync(string? playerName)
        {
            var response = await _http.PostAsJsonAsync(
                "api/sessions",
                new CreateSessionRequest { PlayerName = playerName },
                _json);
            return await ReadStateAsync(response);
        }

        /// <summary>現在のゲーム状態を取得する（<c>GET /api/sessions/{id}</c>）。失効時は 404（<see cref="GameApiException"/>）。</summary>
        public async Task<GameStateResponse> GetSessionAsync(string id)
        {
            var response = await _http.GetAsync($"api/sessions/{id}");
            return await ReadStateAsync(response);
        }

        /// <summary>セッションを破棄する（<c>DELETE /api/sessions/{id}</c>）。</summary>
        public async Task DeleteSessionAsync(string id)
        {
            var response = await _http.DeleteAsync($"api/sessions/{id}");
            await EnsureSuccessAsync(response);
        }

        // ---- 確定セーブ / ロード（SessionsController） ----

        /// <summary>現在のプレイヤーを確定セーブする（<c>POST /api/sessions/{id}/save</c>、セッションは継続）。
        /// リポジトリ未設定/到達不可なら 503（<see cref="GameApiException"/>）。</summary>
        public async Task SaveAsync(string id, string? slotName)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/sessions/{id}/save",
                new SaveRequest { SlotName = slotName },
                _json);
            await EnsureSuccessAsync(response);
        }

        /// <summary>セーブから復元して新規セッションを開始する（<c>POST /api/sessions/load</c>）。探索から再開した状態で返る。
        /// セーブが無ければ 404、リポジトリ未設定/到達不可なら 503。</summary>
        public async Task<GameStateResponse> LoadAsync(string playerName, string? slotName)
        {
            var response = await _http.PostAsJsonAsync(
                "api/sessions/load",
                new LoadRequest { PlayerName = playerName, SlotName = slotName },
                _json);
            return await ReadStateAsync(response);
        }

        // ---- セーブ一覧 / 削除（PlayersController） ----

        /// <summary>プレイヤーのセーブ一覧を取得する（<c>GET /api/players/{name}/saves</c>）。リポジトリ未設定/到達不可なら 503。</summary>
        public async Task<List<PlayerSaveData>> GetSavesAsync(string playerName)
        {
            var response = await _http.GetAsync($"api/players/{Uri.EscapeDataString(playerName)}/saves");
            await EnsureSuccessAsync(response);
            return (await response.Content.ReadFromJsonAsync<List<PlayerSaveData>>(_json)) ?? new();
        }

        /// <summary>指定スロットのセーブを削除する（<c>DELETE /api/players/{name}/saves/{slot}</c>）。</summary>
        public async Task DeleteSaveAsync(string playerName, string slot)
        {
            var response = await _http.DeleteAsync(
                $"api/players/{Uri.EscapeDataString(playerName)}/saves/{Uri.EscapeDataString(slot)}");
            await EnsureSuccessAsync(response);
        }

        // ---- 進行（GameplayController。1 リクエスト = 1 ステップ） ----

        /// <summary>戦闘を 1 ターン進める（<c>ExpectedInput=Attack</c>）。</summary>
        public Task<GameStateResponse> BattleTurnAsync(string id, string strategyName) =>
            PostStepAsync($"api/sessions/{id}/battle/turn", new AttackAction(strategyName));

        /// <summary>ショップで 1 アクション処理する（<c>ExpectedInput=Shop</c>）。<c>Exit</c> を送るまで繰り返す。</summary>
        public Task<GameStateResponse> ShopActionAsync(string id, ShopAction action) =>
            PostStepAsync($"api/sessions/{id}/shop/action", action);

        /// <summary>休憩でアイテムを使う（<c>ExpectedInput=Rest</c>）。<paramref name="action"/> が null ならスキップ。</summary>
        public async Task<GameStateResponse> RestAsync(string id, UseItemAction? action)
        {
            // スキップはボディなし（API 側で null = スキップとして扱う）。
            HttpResponseMessage response = action is null
                ? await _http.PostAsync($"api/sessions/{id}/rest", content: null)
                : await _http.PostAsJsonAsync($"api/sessions/{id}/rest", action, _json);
            return await ReadStateAsync(response);
        }

        /// <summary>エンカウント後の進行を選ぶ（<c>ExpectedInput=GameAction</c>）。Save 系は既定スロットへ確定セーブする。</summary>
        public Task<GameStateResponse> ContinueAsync(string id, GameActionChoice choice) =>
            PostStepAsync($"api/sessions/{id}/continue", new ContinueRequest { Action = choice });

        private async Task<GameStateResponse> PostStepAsync<TBody>(string url, TBody body)
        {
            var response = await _http.PostAsJsonAsync(url, body, _json);
            return await ReadStateAsync(response);
        }

        /// <summary>成功時はレスポンスを <see cref="GameStateResponse"/> として読む。失敗時は <see cref="GameApiException"/>。</summary>
        private async Task<GameStateResponse> ReadStateAsync(HttpResponseMessage response)
        {
            await EnsureSuccessAsync(response);
            return (await response.Content.ReadFromJsonAsync<GameStateResponse>(_json))!;
        }

        /// <summary>非成功ステータスなら本文の <c>{ "error": ... }</c> を取り込み <see cref="GameApiException"/> を送出する。</summary>
        private async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string? message = null;
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(_json);
                message = error?.Error;
            }
            catch
            {
                // 本文が空 / JSON でない場合はステータス相当の既定文言にフォールバックする。
            }

            throw new GameApiException(
                response.StatusCode,
                message ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        /// <summary>API がエラー時に返す <c>{ "error": "..." }</c> 本文。</summary>
        private sealed class ApiError
        {
            public string? Error { get; set; }
        }
    }
}
