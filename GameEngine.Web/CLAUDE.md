# GameEngine.Web

`GameEngine.Api`（ステップ駆動 REST API）を消費する **Blazor WebAssembly フロント**（net8.0, `Microsoft.NET.Sdk.BlazorWebAssembly`）。共有契約 [GameEngine.Contracts](./../GameEngine.Contracts/CLAUDE.md) を `ProjectReference` し、内側 DTO まで芋づるで取得する（API には依存しない）。

## 役割

- ブラウザ上でゲームを 1 ステップずつ進める SPA。状態は API から返る `GameStateResponse` を保持・描画するだけ（ロジック・乱数はサーバ側）。
- レスポンスの `ExpectedInput` を見て次に叩くエンドポイントを決める（`Attack`→`battle/turn` / `Shop`→`shop/action` / `Rest`→`rest` / `GameAction`→`continue` / `None`=終了）。
- クライアントが持つ永続状態は `sessionId`（localStorage）のみ。累積メッセージログはメモリ上に保持（リロードで消える。復帰は W4）。

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
- `GameApiClient` / `SessionStore` を Scoped 登録（WASM では実質アプリ単位の単一インスタンス）。

## 構成

- **Services/GameApiClient.cs** — API エンドポイントに 1:1 対応する薄いラッパー。`CreateSession`/`GetSession`/`DeleteSession`（セッション）、`BattleTurn`/`ShopAction`/`Rest`/`Continue`（進行）。各メソッドは更新後の `GameStateResponse` を返し、非成功ステータスでは例外送出（細かい HTTP ステータス別処理は W3）。セーブ/ロード系は W3 で追加。
- **State/SessionStore.cs** — クライアント側状態の単一の保持者。`SessionId`（localStorage 永続化）・`Current`（最新レスポンス）・`Log`（累積メッセージ）を持つ。`ApplyAsync` がレスポンス取り込み＋差分メッセージ累積＋通知（`OnChange`）を行い、別セッション ID への切替時はログをリセット。
- **Pages/Home.razor** — トップ/スタートメニュー。プレイヤー名入力 →`CreateSession`→ `/play` へ遷移。ロードは W3。
- **Pages/Play.razor** — ゲーム本体（`/play`）。`SessionStore` を購読し、`ExpectedInput` で戦闘/ショップ/休憩/続行/終了ビューを出し分ける。送信中はボタンを disabled にして二重送信を防ぐ。
- **Components/** — `PlayerPanel`（HP/Lv/EXP/Gold/Potions/装備/AP/DP）・`EnemyPanel`（戦闘時）・`MessageLog`（種別色分け）・`HpBar`（割合バー）。
- **Layout/** — `MainLayout` / `NavMenu`。
- **wwwroot/** — `index.html`・CSS（`app.css` に W2 のゲーム UI 用最小スタイルを追記。視覚的な作り込みは W4）。

## 実装フェーズ（[docs/plans/blazor-wasm-frontend.md](./../docs/plans/blazor-wasm-frontend.md)）

- **W1（完了）**: 雛形・ホスト型配信・API 疎通。
- **W2（完了）**: `GameApiClient` / `SessionStore`（localStorage + 累積ログ）/ `ExpectedInput` 駆動の戦闘・ショップ・休憩・続行画面 / スタートメニュー。
- **W3（未）**: セーブ/ロード画面・セーブ一覧/削除、HTTP ステータス別ハンドリング（404→新規誘導 / 409→再取得 / 400 / 503→セーブ UI 無効化）。
- **W4（未）**: スタイル仕上げ、リロード復帰（起動時 `GET /sessions/{id}`）、ゲームオーバー/終了導線、簡易 E2E。

## 留意

- **メッセージは差分返却**: `GameStateResponse.Messages` は前ステップ以降の蓄積分のみ。累積は `SessionStore.Log` がクライアント側で保持する。
- **休憩のポーション名は "Potion"**: `Rest` でポーション使用時は `UseItemAction("Potion", 1)`。ボディ省略（null）はスキップ。
- **終了とゲームオーバーは同じ `None`**: `Quit` も `IsGameOver=true` で返るため、両者は `ExpectedInput=None` の終了ビューでまとめて扱う（導線の作り込みは W4）。
