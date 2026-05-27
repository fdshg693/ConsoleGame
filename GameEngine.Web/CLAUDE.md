# GameEngine.Web

`GameEngine.Api`（ステップ駆動 REST API）を消費する **Blazor WebAssembly フロント**（net8.0, `Microsoft.NET.Sdk.BlazorWebAssembly`）。共有契約 [GameEngine.Contracts](./../GameEngine.Contracts/CLAUDE.md) を `ProjectReference` し、内側 DTO まで芋づるで取得する（API には依存しない）。

## 役割

- ブラウザ上でゲームを 1 ステップずつ進める SPA。状態は API から返る `GameStateResponse` を保持・描画するだけ（ロジック・乱数はサーバ側）。
- クライアントが持つ永続状態は実質 `sessionId` のみ（W2 以降で localStorage 保持予定）。

## 参照方向・配信

```
ブラウザ(Blazor WASM) --HTTP /api--> GameEngine.Api --Step--> GameSystem
GameEngine.Web → GameEngine.Contracts → GameEngine
```

- **ホスト型配信**: `GameEngine.Api` が本プロジェクトの発行物（`_framework/*`・`index.html`）を同一オリジンで配信する（CORS 不要）。API 側で `UseBlazorFrameworkFiles` / `UseStaticFiles` / `MapFallbackToFile("index.html")` を設定（[GameEngine.Api/Program.cs](./../GameEngine.Api/Program.cs)）。`GameEngine.Api.csproj` が本プロジェクトを `ProjectReference` して発行時に同梱する。
- **実行**: `dotnet run --project ./GameEngine.Api`（`http://localhost:5080`）。ルートで本 SPA、`/api` で API、`/swagger` で Swagger が併存。

## Program.cs（合成起点）

- `HttpClient.BaseAddress = HostEnvironment.BaseAddress`（= API オリジン）。`/api/...` が同一オリジンの API に直接届く。
- **`JsonStringEnumConverter` を登録した `JsonSerializerOptions` を Singleton 登録**。API は enum を文字列で授受するため、これを HttpClient の JSON 拡張に渡さないと `ExpectedInput`/`GamePhase`/`ShopActionType`/`GameActionChoice` の送受で破綻する。

## 構成

- **Pages/Home.razor** — フェーズ W1 の疎通確認ページ。`POST /api/sessions`（新規開始）と `GET /api/sessions/{id}`（状態取得）を叩き `GameStateResponse` を表示する。
- **Layout/** — `MainLayout` / `NavMenu`（テンプレート由来。サンプルの Counter/Weather は削除済み）。
- **wwwroot/** — `index.html`・CSS。

## 実装フェーズ（[docs/plans/blazor-wasm-frontend.md](./../docs/plans/blazor-wasm-frontend.md)）

- **W1（完了）**: 雛形・ホスト型配信・API 疎通。
- **W2 以降（未）**: `GameApiClient`（各エンドポイント 1:1 ラッパー）/ `SessionStore`（localStorage）/ `ExpectedInput` 駆動の戦闘・ショップ・休憩・続行画面 / セーブ・ロード / 仕上げ。

## 留意

- **メッセージは差分返却**: `GameStateResponse.Messages` は前ステップ以降の蓄積分のみ。累積ログはクライアント側で保持する（W2）。
- **進行はクライアント主導**: レスポンスの `ExpectedInput` を見て次に叩くエンドポイントを決める（`Attack`→`battle/turn` 等）。
