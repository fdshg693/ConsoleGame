# 05. プロジェクト構成とDI

API層を追加するための、ソリューション構成と依存性注入（DI）の変更点。

## 現状の構成

- `GameEngine.csproj`: **`OutputType=Exe`**, `net8.0`, パッケージは `MongoDB.Driver` / `YamlDotNet`。YAML 3ファイルを `CopyToOutputDirectory=Always` で出力。
- `GameEngine.Tests.csproj`: **`net9.0`**（エンジンは net8.0 と不一致）、`xunit` + `Moq`、`GameEngine` を `ProjectReference`。
- `GameEngine.sln`: 2プロジェクト構成。
- DIフレームワーク: **未使用**。`Program.cs` で手動 `new` の Composition Root。

## 課題

- **`OutputType=Exe` のプロジェクトは他プロジェクトからの参照に適さない**（実行アセンブリでありライブラリ出力でない）。API プロジェクトからコアを `ProjectReference` するには、コアをライブラリ化するのが正攻法。
- エントリポイント `Program.cs` がコンソール前提（`Console.ReadLine`/`RunGameLoop`）。**コア（ゲームロジック）とUI（コンソール）が同一プロジェクトに同居**している。
- テストの `net9.0` とエンジンの `net8.0` の **TFM不一致**は将来の混乱要因。

## 変更方針: ライブラリ分割

コアエンジンをライブラリ化し、UIを別プロジェクトに切り出す。

```
GameEngine.sln
├── GameEngine          (Library, net8.0)  … ゲームロジック・DTO・Manager・StateMachine 等
├── GameEngine.Console  (Exe,     net8.0)  … 現 Program.cs と ConsoleGameInput/ConsoleRenderer
├── GameEngine.Api      (Web,     net8.0)  … ASP.NET Core Web API（新規）
└── GameEngine.Tests    (Test,    net8.0)  … TFMを net8.0 に統一
```

- `GameEngine`（Library）: `<OutputType>` を外す（既定の Library）。YAML設定は引き続き同梱、もしくは設定読み込みパスを各ホストから注入。
- `GameEngine.Console`: 現 `Program.cs`・`ConsoleGameInput`・`ConsoleRenderer`・`UserInteraction` を移動。`IGameInput`/`IRenderer` のコンソール実装を担う。
- `GameEngine.Api`: コントローラ・セッション管理・API用 `IGameInput`/`IRenderer` 実装。`GameEngine` を `ProjectReference`。
- コンソール固有のUI（ANSI・矢印キー）は `GameEngine.Console` に閉じ込め、コアからは排除（[02](./02-io-layer.md)）。

## DIの導入

ASP.NET Core はDIが前提。手動 `new` をDIコンテナ登録に移す。

- `GameEngine` に **`AddGameEngine(IServiceCollection)` 拡張**を用意し、コア依存（Factory・Manager・Repository・StateMachine）の登録を集約。コンソール/APIの両ホストから呼ぶ。
- ライフタイム指針:
  - `GameConfig`: **Singleton**（`GameConfigLoader.Instance` を1度解決して登録。直アクセスはホストの合成起点に限定）。
  - `IEnemyFactory` / 設定由来の不変オブジェクト: Singleton。
  - `IPlayerRepository`（Mongo）: Singleton または Scoped。
  - **プレイヤー進行（`Player`・セッション）**: APIでは **Scoped/セッション単位**で扱う（リクエストごとに `new Player` せず、セッションから復元）。
- 静的依存の解消（[04](./04-session-and-persistence.md) と連動）:
  - `GameMessageBus`（静的イベント）→ インスタンスサービス化し、出力シンクをDIで注入。
  - `GameRecord`（静的）→ サービス化（必要ならプレイヤー単位で永続化）。
  - `GameConfigLoader`（シングルトン）→ DI登録した `GameConfig` を注入。

## 設定（YAML）の扱い

- `game-config.yml` / `enemy-specs.yml` / `weapon-specs.yml` は API ホストの出力先にも配置が必要。
- 各 csproj で `CopyToOutputDirectory` を維持するか、共有の設定ディレクトリを `AppContext.BaseDirectory` フォールバックで解決（現行 `GameConfigLoader.ResolveConfigPath()`・`EnemyFactory` のパス解決と整合）。
- 将来的には `IOptions<GameConfig>` ＋ `appsettings.json` への移行も選択肢だが、必須ではない。

## 影響範囲まとめ

- `GameEngine.csproj`（Library化）、新規 `GameEngine.Console` / `GameEngine.Api`、`GameEngine.sln` 更新。
- `Program.cs` をホスト別に分割（コンソール用ブートストラップ / API用 `Program.cs` + DI登録）。
- `GameEngine.Tests` の TFM を net8.0 に統一。
