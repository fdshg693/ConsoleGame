# GameEngine.Api

RPG エンジンを HTTP で駆動する **ASP.NET Core Web API ホスト**（net8.0, `Microsoft.NET.Sdk.Web`）。コアライブラリ [GameEngine](./../GameEngine/CLAUDE.md) を `ProjectReference` し、コンソール [GameEngine.Console](./../GameEngine.Console/CLAUDE.md) とコアを共有する（UI アダプタのみ差し替え）。

- HTTP 契約 DTO（`GameStateResponse`／リクエスト DTO）は本プロジェクト内ではなく共有ライブラリ [GameEngine.Contracts](./../GameEngine.Contracts/CLAUDE.md) に置き、`ProjectReference` で参照する（Web フロントと二重定義しないため。`namespace GameEngine.Contracts`）。
- Blazor WASM フロント [GameEngine.Web](./../GameEngine.Web/CLAUDE.md) を **ホスト型配信**する（`ProjectReference` + `UseBlazorFrameworkFiles`）。同一オリジンのため CORS 不要、デプロイ単位は 1 つ。

## 役割

- **1 リクエスト = 1 ゲームステップ**。各レスポンスは更新後の状態（`GameStateResponse`）を返す
- セッション識別子（`sessionId`）でゲーム進行を分離し、複数プレイヤーを並行に捌く
- 合成起点（Composition Root）。`GameConfigLoader.Instance` への直アクセスは [Program.cs](./Program.cs) に限定する

## 重要な設計判断: セッションごとの object graph

- エンジンのステートフルなサービス（`IGameMessageBus`/`IGameRecord`/`EventManager`/`GameSystem`/`IPlayer`）は本来「1 ゲーム = 1 グラフ」。`AddGameEngine` はこれらを **Singleton** 登録するため、複数セッションを捌く API では**そのまま使えない**（状態が混線する）
- → API は `AddGameEngine` を**呼ばず**、`GameSessionManager` がセッションごとに専用グラフを手組みする:
  - 共有（Singleton）: `GameConfig` と任意の `IPlayerRepository`（確定セーブ用）のみ
  - セッション専用に新規生成: `GameMessageBus` → それに紐づく `PlayerFactory` 由来のプレイヤー → 同じバスの `EnemyFactory` → `GameRecord` → `EventManager` → `BufferingRenderer` → `GameSystem`
- **ステートマシンはスナップショットから再水和できない**ため、セッションは `GameSystem` を「生きたまま」サーバ常駐させ、リクエストごとに `Step` を 1 回適用する（`ISessionRepository`/`CaptureSession` はあくまで保存用で、進行の継続には使わない）

## Hosting/（API ホスト固有実装）

- **GameSessionManager**（Singleton）— 進行中 `ApiGameSession` 群を `ConcurrentDictionary` で保持。`CreateNew(name)` / `CreateFromSaveAsync(name, slot)`（復元はステータスのみ→探索から再開）/ `Get` / `Remove` / セーブ系の委譲。TTL（最終アクセスから既定 30 分）で遅延失効。`Build()` が専用グラフを手組みし `GameSystem.Start()` まで前進
- **ApiGameSession**（IDisposable）— 1 ゲームの常駐インスタンス。`SyncRoot` で同一セッションへの並行リクエストを直列化（`GameSystem` はスレッドセーフでない）。`SessionId` / `Player` / `GameSystem` / `Renderer` を保持
- **BufferingRenderer** : `IRenderer` — 即時描画せずメッセージを蓄積し、`DrainMessages()` でレスポンスへ渡す。取り込むのは `RenderMessage(s)`・`Write{Info,Success,Warning,Error}`・`WriteResultBox` のみ。`WriteSystem`（`[State]` 遷移ログ）と装飾系（`ClearScreen`/`RenderHPBar`/`RenderStatusPanel`/`WaitForKeyPress`/`WriteSeparator`）は no-op
- **ApiGameInput** : `IGameInput` — スタブ。API は `RunGameLoop` を使わず `Step` を直接駆動するため、メソッドは呼ばれない（呼ばれたら例外）。`GameSystem` の必須依存を満たすためだけに存在

## エンドポイント

ベース `/api`。enum は文字列で授受（`JsonStringEnumConverter`）。Swagger は `/swagger` で常時公開。

| メソッド | パス | 用途 |
|---|---|---|
| POST | `/sessions` | 新規開始（`{playerName?}`）。最初のエンカウントまで前進済みで 201 |
| GET | `/sessions/{id}` | 現在状態 |
| DELETE | `/sessions/{id}` | セッション破棄 |
| POST | `/sessions/{id}/save` | 確定セーブ（`{slotName?}`、既定 `auto_save`） |
| POST | `/sessions/load` | セーブから復元して新規セッション開始（`{playerName, slotName?}`） |
| POST | `/sessions/{id}/battle/turn` | 戦闘 1 ターン（`AttackAction`） |
| POST | `/sessions/{id}/shop/action` | ショップ 1 アクション（`ShopAction`。`Exit` まで繰り返す） |
| POST | `/sessions/{id}/rest` | 休憩（`UseItemAction?`。ボディ省略/null でスキップ） |
| POST | `/sessions/{id}/continue` | エンカウント後の進行（`{action}` = `GameActionChoice`） |
| GET | `/players/{name}/saves` | セーブ一覧 |
| DELETE | `/players/{name}/saves/{slot}` | セーブ削除 |

- **進行はクライアント主導**。レスポンスの `ExpectedInput` を見て次に叩くエンドポイントを決める: `Attack`→`battle/turn` / `Shop`→`shop/action` / `Rest`→`rest` / `GameAction`→`continue` / `None`=終了
- **探索は自動前進**: セッション開始・`continue`（Continue）後はエンジンが即エンカウントに入るため、別途 encounter エンドポイントは持たない（設計案 06 の `/encounter` はこれに吸収）

## エラー方針

- セッション未存在/失効 → **404**
- `ExpectedInput` 不一致（例: 戦闘待ちでないのに `battle/turn`）→ **409**（現在の `expectedInput`/状態名を返す）
- 行動バリデーション失敗（`PlayerActionValidator`。不正な戦略名・数量 0 等）→ **400**。ショップ `Exit` は数量を使わないため API 側で数量を 1 に正規化
- セーブ系でリポジトリ未登録 → **503**。登録済みでも MongoDB 到達不可/認証失敗 → 例外を捕捉して **503**（500 にしない）

## Program.cs（合成起点）

1. `GameConfigLoader.Instance` を 1 度解決して `GameConfig` を Singleton 登録
2. `MongoPlayerRepository` を試行生成（失敗時は登録せず警告ログ→セーブ系は 503）
3. `GameSessionManager` を Singleton 登録（`GameConfig` + 任意の `IPlayerRepository`）
4. `AddControllers`（`JsonStringEnumConverter`）+ Swagger（XML コメント取り込み）
5. ホスト型 WASM 配信: `UseBlazorFrameworkFiles` / `UseStaticFiles` + `MapFallbackToFile("index.html")`（`/api`・`/swagger` 以外は SPA にフォールバック）
6. 末尾に `public partial class Program {}`（`WebApplicationFactory` 統合テスト〔フェーズ5〕用）

## YAML 設定

- `game-config.yml` / `enemy-specs.yml` / `weapon-specs.yml` は `GameEngine`（参照先）の `CopyToOutputDirectory=Always` で本プロジェクト出力にも伝播する
- パス解決は各ローダー（`GameConfigLoader`/`EnemyFactory`/`WeaponFactory`）がカレント → `AppContext.BaseDirectory`（出力先）の順で探索

## 依存パッケージ

- `Swashbuckle.AspNetCore 6.6.2` — Swagger/OpenAPI
- `Microsoft.AspNetCore.Components.WebAssembly.Server 8.0.21` — ホスト型 WASM 配信（`UseBlazorFrameworkFiles`）

## 実行

```bash
# 起動（http://localhost:5080、Swagger UI は /swagger）
dotnet run --project ./GameEngine.Api

# セーブ機能を使う場合は事前に MongoDB を起動
docker-compose up -d
```

- 既定接続文字列は `mongodb://localhost:27017`（`game-config.yml`）。`docker-compose` は認証付き（admin/password）で起動するため、実際にセーブを使うには接続文字列に資格情報が必要（コンソールと共通の課題。詳細は [docs/mongo.md](./../docs/mongo.md)）
