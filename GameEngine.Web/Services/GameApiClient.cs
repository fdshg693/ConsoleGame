using System.Net.Http.Json;
using System.Text.Json;
using GameEngine.Contracts;
using GameEngine.DTOs;

namespace GameEngine.Web.Services
{
    /// <summary>
    /// <c>GameEngine.Api</c> のエンドポイントに 1:1 で対応する薄いラッパー。
    /// 各メソッドは更新後の <see cref="GameStateResponse"/> を返し、非成功ステータスでは例外を送出する
    /// （詳細な HTTP ステータス別ハンドリングはフェーズ W3 で導入予定）。enum は API 仕様に合わせ
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
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<GameStateResponse>(_json))!;
        }

        /// <summary>現在のゲーム状態を取得する（<c>GET /api/sessions/{id}</c>）。失効時は 404 で例外。</summary>
        public async Task<GameStateResponse> GetSessionAsync(string id)
        {
            return (await _http.GetFromJsonAsync<GameStateResponse>($"api/sessions/{id}", _json))!;
        }

        /// <summary>セッションを破棄する（<c>DELETE /api/sessions/{id}</c>）。</summary>
        public async Task DeleteSessionAsync(string id)
        {
            var response = await _http.DeleteAsync($"api/sessions/{id}");
            response.EnsureSuccessStatusCode();
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
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<GameStateResponse>(_json))!;
        }

        /// <summary>エンカウント後の進行を選ぶ（<c>ExpectedInput=GameAction</c>）。</summary>
        public Task<GameStateResponse> ContinueAsync(string id, GameActionChoice choice) =>
            PostStepAsync($"api/sessions/{id}/continue", new ContinueRequest { Action = choice });

        private async Task<GameStateResponse> PostStepAsync<TBody>(string url, TBody body)
        {
            var response = await _http.PostAsJsonAsync(url, body, _json);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<GameStateResponse>(_json))!;
        }
    }
}
