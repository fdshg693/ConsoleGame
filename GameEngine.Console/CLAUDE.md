# GameEngine.Console

コンソールRPGゲームの**実行可能ホスト**（Exe, .NET 8.0）。コアライブラリ [GameEngine](./../GameEngine/CLAUDE.md) を `ProjectReference` し、DI で合成して起動する。

## 役割

- ゲームの**合成起点（Composition Root）**。`GameConfigLoader.Instance` への直アクセスはこのプロジェクトに限定する
- コンソール固有のホスト処理（起動バナー、プレイヤー名入力、例外時の終了）
- コア抽象（`IGameInput` / `IRenderer`）にコンソール実装（`UI/` 配下）を DI 登録する

## UI/（コンソール固有 UI, namespace `CliRpgGame.UI`）

- `ConsoleRenderer : IRenderer` — ANSI 描画の実装（**インスタンスクラス**）。`ClearScreen` / `RenderMessage(s)` / `RenderHPBar` / `RenderStatusPanel` / `WriteResultBox` などコア抽象に加え、コンソール専用の `SelectFromMenu`（矢印キー選択）/ `MenuOrientation`（enum）/ `WriteSection` / `WriteBox` / `WriteCombat` も保持
- `UserInteraction`（static）— `ReadPositiveInteger` / `ReadConfirmation` / `ReadChoice` / `ClearLastOutput` は `Console` 直叩き。`SelectAttackStrategy(ConsoleRenderer, IReadOnlyList<string>? = null)` は `ConsoleRenderer` インスタンスを受け取り描画を委譲する
- `ConsoleGameInput : IGameInput` — コンストラクタ `ConsoleGameInput(ConsoleRenderer renderer, int potionPrice, int potionHealAmount)`。攻撃/ショップ/休息に加え `SelectGameAction()` を実装（矢印キーで続行/セーブ/終了を選び `GameActionChoice` を返す）

## Program.cs

起動シーケンス（挙動はライブラリ分割前と同一）:

1. バナー表示 → `GameConfigLoader.Instance` で `GameConfig` を一度だけ取得（起動時バリデーション）
2. プレイヤー名をコンソール入力（空なら "Hero"）
3. DI 合成:
   - `services.AddGameEngine()` — コア依存（`GameConfig`/`IGameMessageBus`/`IEnemyFactory`/`EventManager`/`GameSystem`）を登録
   - `ConsoleRenderer` を Singleton 登録し、`IRenderer` → 同一インスタンスへ橋渡し（`GameSystem`/`EventManager` と `ConsoleGameInput` が単一の `ConsoleRenderer` を共有）
   - `IGameInput` → `new ConsoleGameInput(ConsoleRenderer, potionPrice, potionHealAmount)`（価格・回復量は `GameConfig` から解決）
   - `IPlayer` → `AddGameEngine` が登録する `IPlayerFactory.CreateNew(playerName)` で生成（プレイヤー組み立ては `PlayerFactory` に集約）
   - `IPlayerRepository` → `CreatePlayerRepository(config)`。MongoDB 不可なら**登録せず**、`GameSystem` は `IPlayerRepository?` 既定値 null（セーブ無効）で続行
4. `ServiceProvider` から `GameSystem`（IDisposable）を解決し `RunGameLoop()` を実行
   - `RunGameLoop()` はコアのステップ駆動 API に対する**薄い駆動ループ**: `Start()` 後 `while(IsRunning)` で `GameSystem.ExpectedInput` を読み、対応する `IGameInput.Select*` から行動を取得して `PlayerInput` に包み `Step()` に渡すだけ。進行順序の制御はコアの State 群が持つ
   - `using` でスコープ終了時に `GameMessageBus` 購読を解除（provider 破棄でも解除されるため二重 Dispose は冪等）
5. 例外発生時は警告表示後 `Environment.Exit(1)`

## 依存パッケージ

- `Microsoft.Extensions.DependencyInjection 8.0.0` — `ServiceCollection` / `BuildServiceProvider`

## YAML 設定

- `game-config.yml` / `enemy-specs.yml` / `weapon-specs.yml` は `GameEngine`（参照先）の `CopyToOutputDirectory=Always` 設定により本プロジェクトの出力にも伝播する
- 実行時は `GameConfigLoader.ResolveConfigPath()` がカレントディレクトリ → `AppContext.BaseDirectory`（出力先）の順に解決

## 注意

- アセンブリ名は `GameEngine.Console` だが、`System.Console` との曖昧化を避けるためルート名前空間は `CliRpgGame`（`RootNamespace`）に固定
- コンソール固有 UI（`ConsoleGameInput` / `ConsoleRenderer` / `UserInteraction`）は本プロジェクトの `UI/`（namespace `CliRpgGame.UI`）に隔離済み。コアは `IRenderer` / `IGameInput` 抽象のみに依存する

## 実行

```bash
dotnet run --project ./GameEngine.Console
```
