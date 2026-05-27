# GameEngine.Web

`GameEngine.Api`（ステップ駆動 REST API）を消費する **Blazor WebAssembly フロント**（net8.0, `Microsoft.NET.Sdk.BlazorWebAssembly`）。共有契約 [GameEngine.Contracts](./../GameEngine.Contracts/CLAUDE.md) を `ProjectReference` し、内側 DTO まで芋づるで取得する（API には依存しない）。

## 役割

- ブラウザ上でゲームを 1 ステップずつ進める SPA。状態は API から返る `GameStateResponse` を保持・描画するだけ（ロジック・乱数はサーバ側）。
- レスポンスの `ExpectedInput` を見て次に叩くエンドポイントを決める（`Attack`→`battle/turn` / `Shop`→`shop/action` / `Rest`→`rest` / `GameAction`→`continue` / `None`=終了）。
- クライアントが持つ永続状態は `sessionId`（localStorage）のみ。累積メッセージログはメモリ上に保持（リロードで消える）。リロード/直接アクセス時は localStorage の `sessionId` から `GET /sessions/{id}` で進行を復帰する（失効=404 ならクリアしてメニューへ）。
- HTTP ステータス別に動作を変える: `404`→セッション失効として失効ビューを出しメニューへ誘導 / `409`→`GET /sessions/{id}` で最新状態に同期 / `400`→エラー文言を表示 / `503`→セーブ UI を無効化。

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

- **Services/GameApiClient.cs** — API エンドポイントに 1:1 対応する薄いラッパー。`CreateSession`/`GetSession`/`DeleteSession`（セッション）、`Save`/`Load`/`GetSaves`/`DeleteSave`（確定セーブ）、`BattleTurn`/`ShopAction`/`Rest`/`Continue`（進行）。進行系は更新後の `GameStateResponse` を返す。非成功ステータスでは本文の `{ "error": ... }` を取り込み `GameApiException`（`StatusCode` 保持）を送出する。
- **Services/GameApiException.cs** — API が非成功ステータスを返したときの例外。`HttpStatusCode` を保持し、呼び出し側の HTTP ステータス別処理（404/409/400/503）の分岐に使う。
- **State/SessionStore.cs** — クライアント側状態の単一の保持者。`SessionId`（localStorage 永続化）・`Current`（最新レスポンス）・`Log`（累積メッセージ）・`SaveAvailable`（セーブ可否。`null`=未判定）を持つ。`ApplyAsync` がレスポンス取り込み＋差分メッセージ累積＋通知（`OnChange`）を行い、別セッション ID への切替時はログをリセット。`LoadSessionIdAsync` が起動時に localStorage から `sessionId` を読み戻す（復帰の起点）。`SetSaveAvailable` で 503 検知時にセーブ UI を無効化する。
- **Pages/Home.razor** — トップ/スタートメニュー。プレイヤー名入力 →`CreateSession`→ `/play`。初期化時に `LoadSessionIdAsync` で localStorage を確認し、進行中の `sessionId` が残っていれば「冒険を再開」導線（→`/play`）を出す。同名でセーブ一覧を表示し、スロット選択で `Load`（→`/play`）/`DeleteSave`。一覧/ロードが 503 ならセーブ機能無効を表示。
- **Pages/Play.razor** — ゲーム本体（`/play`）。`SessionStore` を購読し、`ExpectedInput` で戦闘/ショップ/休憩/続行/終了ビューを出し分ける。送信中はボタンを disabled にして二重送信を防ぐ。初期化時に `Store.Current` が空なら `TryRecoverSessionAsync` が localStorage の `sessionId` から `GET /sessions/{id}` で復帰（復帰中はスピナー表示、失効=404 はクリアしてメニューへ、復帰元無しもメニューへ）。`GetSaves` で `SaveAvailable` を一度だけ判定し、可ならセーブ欄（任意スロット）と PostEncounter の「セーブして続ける/終了」（`SaveAndContinue`/`SaveAndQuit`）を出す。終了/ゲームオーバー（`ExpectedInput=None`）は締めパネル（到達 Lv/EXP/Gold）＋「同じ名前でもう一度」（`ReplayAsync`＝同名で `CreateSession`）/「メニューに戻る」導線。`GameApiException` をステータス別に処理（404→失効ビュー / 409→再取得同期 / 503→セーブ無効化 / その他→表示）。
- **Components/** — `PlayerPanel`（HP/Lv/EXP/Gold/Potions/装備/AP/DP）・`EnemyPanel`（戦闘時）・`MessageLog`（種別色分け）・`HpBar`（割合バー）。
- **Layout/** — `MainLayout` / `NavMenu`。
- **wwwroot/** — `index.html`・CSS（`app.css`。W4 でゲーム UI・HP バー（グラデーション）・終了画面・復帰スピナーの視覚調整を実施）。

## 実装フェーズ（[docs/plans/blazor-wasm-frontend.md](./../docs/plans/blazor-wasm-frontend.md)）

- **W1（完了）**: 雛形・ホスト型配信・API 疎通。
- **W2（完了）**: `GameApiClient` / `SessionStore`（localStorage + 累積ログ）/ `ExpectedInput` 駆動の戦闘・ショップ・休憩・続行画面 / スタートメニュー。
- **W3（完了）**: セーブ/ロード画面・セーブ一覧/削除、HTTP ステータス別ハンドリング（404→失効誘導 / 409→再取得同期 / 400→表示 / 503→セーブ UI 無効化）。
- **W4（完了）**: スタイル仕上げ（HP バー/終了画面/復帰スピナー）、リロード復帰（`Play` 初期化時に localStorage の `sessionId` から `GET /sessions/{id}`、404=失効はクリア）、`Home` の再開導線、終了/ゲームオーバーの締めパネル＋再挑戦導線。E2E は API ホストへの `POST /sessions`→`GET /sessions/{id}`→`battle/turn` 往復＋全テストで確認。

## 留意

- **メッセージは差分返却**: `GameStateResponse.Messages` は前ステップ以降の蓄積分のみ。累積は `SessionStore.Log` がクライアント側で保持する。
- **休憩のポーション名は "Potion"**: `Rest` でポーション使用時は `UseItemAction("Potion", 1)`。ボディ省略（null）はスキップ。
- **終了とゲームオーバーは同じ `None`**: `Quit` も `IsGameOver=true` で返るため、両者は `ExpectedInput=None` の締めパネルでまとめて扱う（`is-gameover`/`is-clear` で色分け）。到達 Lv/EXP/Gold を表示し、「同じ名前でもう一度」（同名で `CreateSession`）/「メニューに戻る」を出す。
- **セーブ可否は事前判定が要る**: `continue` 経由の `SaveAndContinue`/`SaveAndQuit` はリポジトリ未設定でも 503 を返さずサーバ側で黙ってスキップする（503 を返すのは `save`/`load`/一覧/削除のみ）。そのため `Play` 初期化時に `GetSaves` で `SaveAvailable` を判定し、セーブ系 UI 自体を出し分けて誤操作を防ぐ。
