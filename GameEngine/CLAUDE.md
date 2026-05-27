# GameEngine

C#コンソールRPGエンジンの**コアライブラリ**（.NET 8.0, Library 出力）。コンソール/API 双方のホストから `ProjectReference` される。エントリポイント（Exe）は別プロジェクト [GameEngine.Console](./../GameEngine.Console/CLAUDE.md)。

## アーキテクチャ概要

```
GameEngine.Console/Program.cs (合成起点 / Composition Root)
  -> GameConfig を一度だけ取得（GameConfigLoader.Instance）
  -> ServiceCollection.AddGameEngine() でコア依存を登録
  -> IGameInput / IRenderer / IPlayer / IPlayerRepository をホスト登録
  -> ServiceProvider から GameSystem を解決
       -> Start() で状態機械を起動
       -> Step(PlayerInput) を 1 行動ずつ適用して進行
            -> 統一ステートマシン: [Explore | Shop | Battle | Rest | PostEncounter | GameOver]
```

- `GameSystem` は内部にブロッキング `while` ループを持たない**ステップ駆動エンジン**。「1 行動 → 1 ステップ → 状態」で外部から駆動する（`Start()` 後に `Step(PlayerInput)` を反復）
- ホストは `GameSystem.ExpectedInput`（次に必要な入力種別）を見て、対応する `PlayerInput` を組み立てて `Step` に渡す。進行順序の制御はコアの State 群が持つ
- Factory / Strategy / Manager パターンを組み合わせたターン制戦闘エンジン
- ゲームバランスは全て YAML 設定ファイルで外部化
- コアは `System.Console` に直接依存しない。表示は `IRenderer`、入力は `IGameInput` 経由。コンソール固有 UI（`ConsoleRenderer` / `ConsoleGameInput` / `UserInteraction`）は `GameEngine.Console/UI/` に存在する

## DependencyInjection/ServiceCollectionExtensions.cs（DI 合成）

- `AddGameEngine(this IServiceCollection)` がコア依存の登録を集約する。コンソール/API 両ホストから呼ぶ
- 登録するもの（UI 非依存・実行時入力不要のコア）:
  - `GameConfig` — Singleton（`GameConfigLoader.Instance` を1度だけ解決。直アクセスはこの合成に限定）
  - `IGameMessageBus` → `GameMessageBus`（Singleton。発行側（Player/Manager/Enemy）と購読側（`GameSystem` → `IRenderer`）が同一インスタンスを共有）
  - `IEnemyFactory` → `EnemyFactory`（Singleton, `GameConfig.Enemy` 由来。生成時に `IGameMessageBus` を渡す）
  - `EventManager` / `GameSystem` — Singleton（進行制御。`EventManager` は描画・入力に非依存で `IPlayer`/`GameConfig`/`IEnemyFactory`〔＋任意の `Random?`〕から解決。`GameSystem` の `IPlayer`/`IGameInput`/`IRenderer` はホスト登録後に解決される）
- 登録しない（ホスト責務）もの: `IGameInput`（UI 入力実装）/ `IRenderer`（描画実装。コンソールは ANSI、API はバッファ/DTO 蓄積）/ `IPlayer`（実行時プレイヤー名）/ `IPlayerRepository`（任意。未登録なら `GameSystem` は `IPlayerRepository?` 既定値 null でセーブ無効）
- 依存パッケージ: `Microsoft.Extensions.DependencyInjection.Abstractions`

## YAML 設定ファイル

| ファイル | 役割 | 読み込み元 |
|---|---|---|
| `game-config.yml` | プレイヤー初期値、レベルアップ、ショップ、イベント確率、武器ステータス、MongoDB接続 | `GameConfigLoader` |
| `enemy-specs.yml` | 敵の定義（Name, HP, AttackStrategy, Experience, AP, DP） | `EnemyFactory` |
| `weapon-specs.yml` | 武器の定義（Name, HP, AP, DP） | `WeaponFactory` |

- 3ファイルとも `CopyToOutputDirectory: Always` でビルド時にコピーされる

## Configuration/GameConfigLoader.cs

- `game-config.yml` をシングルトン（double-checked locking）で読み込む
- `GameConfig` クラスに以下のセクションをマッピング:
  - `MongoDBConfig` — MongoDB 接続設定
  - `PlayerConfig` — プレイヤー初期ステータス（HP, DP, AP, Gold, Potions）
  - `LevelUpConfig` — レベルアップ時の増加値と必要経験値
  - `ItemsConfig` / `PotionConfig` — アイテム価格・効果
  - `EventsConfig` — イベント出現重み（Shop:Battle = 1:2）
  - `ShopConfig` — ゴールド報酬範囲
  - `EnemyConfig` — 敵撃破時のゴールド計算パラメータ
  - `Weapons` — 武器ステータス辞書
- パス解決は `ResolveConfigPath()` がカレントディレクトリ相対 → 出力ディレクトリ（`AppContext.BaseDirectory`）の順に探索
- YAML パースエラー・ファイル未検出時はデフォルト値にフォールバック
- `ValidateConfig()` で値の妥当性を検証（負数チェック等）
- `ReloadConfig()` でテスト時に再読み込み可能

## Constants/GameConstants.cs

- 設定で外部化しない純粋な固定値のみを保持する静的クラス
- `AttackDamage` ネストクラスのみを持ち、攻撃戦略のダメージ範囲を `const` で定義（Default/Melee/Magic）
- 設定値はすべて `GameConfig` のコンストラクタ注入で取得する（このクラスは設定を委譲しない）

## 外部依存パッケージ

- `Microsoft.Extensions.DependencyInjection.Abstractions 8.0.0` — `AddGameEngine` 拡張（`IServiceCollection`）
- `YamlDotNet 16.3.0` — YAML のデシリアライズ
- `MongoDB.Driver 3.5.0` — セーブデータの永続化

## サブフォルダ

各フォルダの詳細は個別の CLAUDE.md を参照:

- `DependencyInjection/` — `AddGameEngine` 拡張（上記「DI 合成」参照）
- [Factory/CLAUDE.md](./Factory/CLAUDE.md) — EnemyFactory, WeaponFactory, YamlSpecLoader
- [Interfaces/CLAUDE.md](./Interfaces/CLAUDE.md) — IAttackStrategy, ICharacter, IEnemy, IGameInput, IRenderer, IGameMessageBus 等（描画/入力/メッセージバスの抽象。実装はホスト側）
- [Manager/CLAUDE.md](./Manager/CLAUDE.md) — HealthManager, InventoryManager, ExperienceManager, CombatManager, SaveDataManager
- [Models/CLAUDE.md](./Models/CLAUDE.md) — Player, Enemy, Weapon, AttackStrategy, GameState, GameMessageBus 等
- [Systems/CLAUDE.md](./Systems/CLAUDE.md) — GameSystem, BattleSystem, ShopSystem（コンソール UI は含まない）
- [Systems/StateMachine/CLAUDE.md](./Systems/StateMachine/CLAUDE.md) — ステートマシン

## ビルド・実行

```bash
# ビルド（ソリューションルートから）
dotnet build

# 実行（Exe は GameEngine.Console）
dotnet run --project ./GameEngine.Console

# テスト
dotnet test

# セーブ機能を使う場合は事前に MongoDB を起動
docker-compose up -d
```
