# GameEngine.Console

コンソールRPGゲームの**実行可能ホスト**（Exe, .NET 8.0）。コアライブラリ [GameEngine](./../GameEngine/CLAUDE.md) を `ProjectReference` し、DI で合成して起動する。

## 役割

- ゲームの**合成起点（Composition Root）**。`GameConfigLoader.Instance` への直アクセスはこのプロジェクトに限定する
- コンソール固有のホスト処理（起動バナー、プレイヤー名入力、例外時の終了）
- コアの UI 抽象（`IGameInput`）にコンソール実装を DI 登録する

## Program.cs

起動シーケンス（挙動はライブラリ分割前と同一）:

1. バナー表示 → `GameConfigLoader.Instance` で `GameConfig` を一度だけ取得（起動時バリデーション）
2. プレイヤー名をコンソール入力（空なら "Hero"）
3. DI 合成:
   - `services.AddGameEngine()` — コア依存（`GameConfig`/`IEnemyFactory`/`EventManager`/`GameSystem`）を登録
   - `IGameInput` → `ConsoleGameInput(potionPrice, potionHealAmount)` を登録（値は `GameConfig` から解決）
   - `IPlayer` → `CreatePlayer(playerName, config)` を登録（`ExperienceManager`/`InventoryManager`/`Player` を組み立て）
   - `IPlayerRepository` → `CreatePlayerRepository(config)`。MongoDB 不可なら**登録せず**、`GameSystem` は `IPlayerRepository?` 既定値 null（セーブ無効）で続行
4. `ServiceProvider` から `GameSystem`（IDisposable）を解決し `RunGameLoop()` を実行
   - `using` でスコープ終了時に `GameMessageBus` 購読を解除（provider 破棄でも解除されるため二重 Dispose は冪等）
5. 例外発生時は警告表示後 `Environment.Exit(1)`

## 依存パッケージ

- `Microsoft.Extensions.DependencyInjection 8.0.0` — `ServiceCollection` / `BuildServiceProvider`

## YAML 設定

- `game-config.yml` / `enemy-specs.yml` / `weapon-specs.yml` は `GameEngine`（参照先）の `CopyToOutputDirectory=Always` 設定により本プロジェクトの出力にも伝播する
- 実行時は `GameConfigLoader.ResolveConfigPath()` がカレントディレクトリ → `AppContext.BaseDirectory`（出力先）の順に解決

## 注意

- アセンブリ名は `GameEngine.Console` だが、`System.Console` との曖昧化を避けるためルート名前空間は `CliRpgGame`（`RootNamespace`）に固定
- コンソール固有 UI（`ConsoleGameInput` / `ConsoleRenderer` / `UserInteraction`）は現状コア（`GameEngine/Systems`）に同居。将来フェーズで本プロジェクトへ隔離予定

## 実行

```bash
dotnet run --project ./GameEngine.Console
```
