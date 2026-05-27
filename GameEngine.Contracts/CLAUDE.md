# GameEngine.Contracts

API と将来の Web フロント（Blazor WASM）が共有する **HTTP 契約 DTO** のクラスライブラリ（net8.0, `Microsoft.NET.Sdk`, ASP.NET 非依存）。二重定義のずれを防ぐ「契約の単一の真実」。

## 役割

- HTTP の境界で授受する DTO のみを保持する。ロジック・依存（ASP.NET Core 等）は持たない。
- `GameEngine` を `ProjectReference` し、内側 DTO（`PlayerState`/`EnemyState`/`BattleState`/`ShopState`/`GamePhase`/`GameMessage`/`ExpectedInput`/`GameActionChoice` 等）を再公開する（`GameStateResponse` がそれらを含むため依存は必然）。

## 参照方向

```
GameEngine.Api → GameEngine.Contracts → GameEngine
GameEngine.Web → GameEngine.Contracts          （Web は API に依存しない）
```

- API/Web は `GameEngine.Contracts` を参照すれば内側 DTO まで芋づるで取得できる。

## 保持する型

- **GameStateResponse.cs** — 各 API ステップのレスポンス。状態（`Player`/`CurrentEnemy`/`CurrentBattle`/`CurrentShop`/`Phase`）＋ステップ駆動メタ（`SessionId`/`CurrentStateName`/`ExpectedInput`/`IsRunning`/`IsGameOver`）＋差分メッセージ（`Messages`）。
- **Requests.cs** — リクエスト DTO 群: `CreateSessionRequest` / `SaveRequest` / `LoadRequest` / `ContinueRequest`。

## 留意

- enum は文字列で授受される（API 側で `JsonStringEnumConverter` 登録）。namespace は JSON 形に影響しないため、移設後も契約（JSON 形）は不変。
