This is a C# console RPG engine on .NET 8.0. The runtime is a layered system that combines Factory, Strategy, and Manager patterns to drive turn-based combat, equipment, and YAML-configured enemies.

## Solution Structure

- `GameEngine`（Library, net8.0）: ゲームロジック・DTO・Manager・StateMachine・DI 登録（`AddGameEngine`）。Exe ではなくライブラリ出力で、コンソール/API 両ホストから `ProjectReference` 可能。
- `GameEngine.Contracts`（Library, net8.0, ASP.NET 非依存）: API と Web フロント（Blazor WASM）が共有する HTTP 契約 DTO（`GameStateResponse` とリクエスト DTO 群）。`GameEngine` を `ProjectReference` して内側 DTO（`PlayerState` 等）を再公開する。参照方向は `GameEngine.Api`／`GameEngine.Web` → `GameEngine.Contracts` → `GameEngine`。
- `GameEngine.Console`（Exe, net8.0）: コンソールホスト。合成起点 [./../GameEngine.Console/Program.cs](./../GameEngine.Console/Program.cs) と UI 固有実装（`ConsoleRenderer` / `UserInteraction` / `ConsoleGameInput` を `UI/`、namespace `CliRpgGame.UI` に保持）。
- `GameEngine.Api`（Web, net8.0）: ASP.NET Core Web API ホスト。1 リクエスト=1 ステップで `GameSystem.Step` を駆動し、`sessionId` で進行を分離する。`GameEngine.Web` の発行物をホスト型配信する（同一オリジン）。詳細は [./../GameEngine.Api/CLAUDE.md](./../GameEngine.Api/CLAUDE.md)。
- `GameEngine.Web`（Blazor WASM, net8.0）: ブラウザ向け SPA フロント。`GameEngine.Api` を `HttpClient` で叩き `GameStateResponse` を描画する。`GameEngine.Contracts` を共有参照（API には非依存）。詳細は [./../GameEngine.Web/CLAUDE.md](./../GameEngine.Web/CLAUDE.md)。
- `GameEngine.Tests`（Test, net8.0）: xUnit + Moq。TFM はエンジンと統一。

## Architecture & Data Flow

- **Entry/Composition Root**: 各ホストが自身の合成起点を持つ（`GameConfigLoader.Instance` への直アクセスは合成起点に限定）。コンソール [./../GameEngine.Console/Program.cs](./../GameEngine.Console/Program.cs) は `AddGameEngine()`（[./../GameEngine/DependencyInjection/ServiceCollectionExtensions.cs](./../GameEngine/DependencyInjection/ServiceCollectionExtensions.cs)）でコア依存を登録し、`IGameInput`/`IRenderer`/`IPlayer`/`IPlayerRepository` などホスト固有依存を追加登録して単一の `GameSystem` を解決・実行する。API [./../GameEngine.Api/Program.cs](./../GameEngine.Api/Program.cs) は複数セッションを並行に捌くため `AddGameEngine` の Singleton 群を使わず、`GameSessionManager` がセッションごとに専用 object graph（バス/プレイヤー/敵ファクトリ/勝敗記録/`EventManager`/`GameSystem`）を手組みする（共有は `GameConfig` と任意の `IPlayerRepository` のみ）。
- **Step-driven control flow**: [./../GameEngine/Systems/GameSystem.cs](./../GameEngine/Systems/GameSystem.cs) は内部にブロッキング `while` を持たないステップ駆動エンジン。`Start()` で統一ステートマシンを起動し、`Step(PlayerInput)`（[./../GameEngine/DTOs/StepContracts.cs](./../GameEngine/DTOs/StepContracts.cs)）を1行動ずつ適用して進める。ホストは `ExpectedInput`（`None`/`Attack`/`Shop`/`Rest`/`GameAction`）を見て対応する `PlayerInput` を渡す。コンソールは `RunGameLoop()`（`IGameInput.Select*` → `Step` の薄い駆動ループ）、API はリクエスト単位で `Step` を呼ぶ。
- **I/O abstraction**: コアは描画を `IRenderer`（[./../GameEngine/Interfaces](./../GameEngine/Interfaces)）への注入で行い、`Console` 依存を持たない。コンソール UI（`ConsoleRenderer`/`UserInteraction`/`ConsoleGameInput`）は `GameEngine.Console/UI/`（namespace `CliRpgGame.UI`）に隔離。`IRenderer` の登録はホスト責務。
- **Event routing**: [./../GameEngine/Systems/EventManager.cs](./../GameEngine/Systems/EventManager.cs) の `BeginEncounter()` が重み付き設定（`config.Events`、1/3 shop・2/3 battle）でショップ/戦闘を抽選して種別を決定し、`SubmitShopAction`/`SubmitBattleTurn`/`SubmitRestAction` で各サブシステムへ1アクションずつ委譲する（描画・入力に非依存）。
- **Combat**: [./../GameEngine/Systems/BattleSystem/BattleManager.cs](./../GameEngine/Systems/BattleSystem/BattleManager.cs) は `StartBattle()` / `SubmitPlayerTurn(AttackAction)` でターンをステップ進行し、結果を `BattleStepResult` で返す（描画はステート側が `IRenderer` 経由で行う）。攻撃戦略の選択はコンソール駆動ループから `IGameInput` を通じて供給される。
- **Composition**: `Player` owns `HealthManager`, `InventoryManager`, `ExperienceManager` (see [./../GameEngine/Models/Player.cs](./../GameEngine/Models/Player.cs) and [./../GameEngine/Manager](./../GameEngine/Manager)).

```
Program -> ServiceCollection.AddGameEngine()+host registrations -> ServiceProvider -> GameSystem.Start()/Step(PlayerInput) -> 統一ステートマシン -> [Explore | Shop | Battle | Rest | PostEncounter] step methods -> Player/Enemy -> Managers
```

## Core Patterns (project-specific)

- **Strategy**: `IAttackStrategy` with `Default`/`Melee`/`Magic`. Strategy names are centralized in `AttackStrategyNames` ([./../GameEngine/Constants/AttackStrategyNames.cs](./../GameEngine/Constants/AttackStrategyNames.cs)). Mapping is in `AttackStrategy.GetAttackStrategy()` and `EnemyFactory.Create()` (keep names aligned with YAML).
- **Factory**: `EnemyFactory` はインスタンスクラスで `IEnemyFactory` を実装し、`AddGameEngine` が Singleton 登録して EventManager → BattleManager へDI注入する（静的な起動時ローダーではない）。コンストラクタで `EnemyConfig` + `IGameMessageBus` + `Random` を受け取り、`AppContext.BaseDirectory` フォールバック付きのパスで自身の YAML specs を読み込む。`WeaponFactory`（static）も同じ `AppContext.BaseDirectory` フォールバックで `weapon-specs.yml` を解決し、武器生成を集約する。
- **Managers**: `HealthManager` uses `IEquipmentStatsProvider` from inventory to compute HP/AP/DP; `ExperienceManager` drives level growth. ドメインクラス（`Player`・各 Manager・`Enemy`）は設定を静的な `GameConstants` ラッパー経由ではなく `GameConfig`/サブ設定値のコンストラクタ注入で受け取る（`GameConstants` は固定の `AttackDamage` const のみを保持）。
- **Repository**: `IPlayerRepository` abstracts persistence. `MongoPlayerRepository`（本番）と `InMemoryPlayerRepository`（テスト用）を切り替え可能。`GameSystem` にコンストラクタ注入される（DI 未登録時は `IPlayerRepository?` の既定値 null = セーブ無効）。
- **Player factory**: `IPlayerFactory`（`PlayerFactory`）が新規生成（`CreateNew`）とセーブデータ復元（`Restore(PlayerSaveData)`）を集約。復元は HP/レベル/経験値/ゴールド/ポーション/装備武器/攻撃戦略/基礎ステータスを再構築する（`AddGameEngine` が Singleton 登録）。
- **Game record**: `IGameRecord`（`GameRecord`）は勝敗記録のインスタンスサービス（静的状態を排除）。`AddGameEngine` が Singleton 登録し、`BattleManager` が記録・`GameSystem`/`GameFlowContext` が参照する。
- **Session layer**: 進行中ゲームの揮発状態を `GameSessionState`（戦闘途中の敵HP・ターン・フェーズ含む）で表し、`ISessionRepository`（既定 `InMemorySessionRepository` = インメモリ + TTL）で保持・復元する。`GameSystem.CaptureSession(sessionId)` がスナップショットを生成。確定セーブ（`IPlayerRepository`）とは責務分離（セーブ＝確定スナップショット、セッション＝進行中の揮発状態）。
- **Messaging**: `GameMessageBus`（Models）はインスタンス化され `IGameMessageBus` を実装。`AddGameEngine` が `IGameMessageBus`→`GameMessageBus` を Singleton 登録し、発行側（`Player`・各 Manager・`Enemy`）と購読側（`GameSystem` → `IRenderer`）が同一インスタンスを共有する。`InventoryManager`/`ExperienceManager`/`Player`/`EnemyFactory` 等はコンストラクタで `IGameMessageBus` を受け取る。
- **DI**: `AddGameEngine(IServiceCollection)` がコア依存（`GameConfig` Singleton / `IGameMessageBus` / `IEnemyFactory` / `IGameRecord` / `IPlayerFactory` / `ISessionRepository` / `EventManager` / `GameSystem`）の登録を集約。`IGameInput`・`IRenderer`（描画。ホスト責務）・`IPlayer`（実行時名。`IPlayerFactory` で生成）・`IPlayerRepository`（任意）は各ホストが登録する。`Microsoft.Extensions.DependencyInjection` 系を使用。

## Configuration & External Dependencies

- **YAML**: [./../GameEngine/enemy-specs.yml](./../GameEngine/enemy-specs.yml) and [./../GameEngine/weapon-specs.yml](./../GameEngine/weapon-specs.yml). Specs are deserialized via YamlDotNet (see [./../GameEngine/Factory/EnemyFactory.cs](./../GameEngine/Factory/EnemyFactory.cs)).
- **Save system**: MongoDB via Docker Compose (see [docker-compose.yml](docker-compose.yml) and [docs/mongo.md](docs/mongo.md)); persistence is abstracted via `IPlayerRepository` ([./../GameEngine/Interfaces/IPlayerRepository.cs](./../GameEngine/Interfaces/IPlayerRepository.cs)) with `MongoPlayerRepository` ([./../GameEngine/Manager/MongoPlayerRepository.cs](./../GameEngine/Manager/MongoPlayerRepository.cs)) as the default implementation. BSON mapping is defined in `MongoPlayerRepository` via `BsonClassMap` (not on `PlayerSaveData` itself).
- **DTOs**: UI state DTOs (`GameState`, `PlayerState`, etc.), command DTOs (`PlayerAction` hierarchy), persistence DTO (`PlayerSaveData`), and session snapshot DTO (`GameSessionState`) are in [./../GameEngine/DTOs](./../GameEngine/DTOs). Mappers are in [./../GameEngine/Mappers](./../GameEngine/Mappers).

## Developer Workflows

- Build: `dotnet build`
- Run (console): `dotnet run --project ./../GameEngine.Console`
- Run (API): `dotnet run --project ./../GameEngine.Api`（Swagger UI: `/swagger`）
- Tests: `dotnet test`
- Save feature: run `docker-compose up -d` first (MongoDB + Mongo Express).

## Extension Guidelines (existing conventions)

- **Add enemy**: update [./../GameEngine/enemy-specs.yml](./../GameEngine/enemy-specs.yml); no code change unless you add a new `AttackStrategy` name.
- **Add attack strategy**: implement `IAttackStrategy`, add mapping in [./../GameEngine/Models/AttackStrategy.cs](./../GameEngine/Models/AttackStrategy.cs), register name in [./../GameEngine/Constants/AttackStrategyNames.cs](./../GameEngine/Constants/AttackStrategyNames.cs), and add UI option in [./../GameEngine.Console/UI/UserInteraction.cs](./../GameEngine.Console/UI/UserInteraction.cs).
- **Add weapons/items**: extend [./../GameEngine/Factory/WeaponFactory.cs](./../GameEngine/Factory/WeaponFactory.cs) and surface in [./../GameEngine/Systems/ShopSystem.cs](./../GameEngine/Systems/ShopSystem.cs).

## Console Interaction Details

- Strategy/menu selection uses arrow keys and redraws via ANSI escapes (`\x1b[s`/`\x1b[u`/`\x1b[0J` in `ConsoleRenderer.SelectFromMenu`; `\x1b[1A`, `\x1b[2K` in `UserInteraction.ClearLastOutput`). UI lives in [./../GameEngine.Console/UI/ConsoleRenderer.cs](./../GameEngine.Console/UI/ConsoleRenderer.cs) and [./../GameEngine.Console/UI/UserInteraction.cs](./../GameEngine.Console/UI/UserInteraction.cs).
- Input validation favors `ReadPositiveInteger()` with quit options.
