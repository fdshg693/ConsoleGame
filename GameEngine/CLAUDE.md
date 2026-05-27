# GameEngine

C#コンソールRPGエンジンのメインアプリケーション（.NET 8.0）。

## アーキテクチャ概要

```
Program.cs (エントリポイント / Composition Root)
  -> GameConfig を一度だけ取得（GameConfigLoader.Instance）
  -> Player / EventManager / IPlayerRepository を生成
  -> GameSystem に注入して RunGameLoop()
       -> ShopSystem (買い物イベント)
       -> BattleSystem (戦闘イベント)
```

- Factory / Strategy / Manager パターンを組み合わせたターン制戦闘エンジン
- ゲームバランスは全て YAML 設定ファイルで外部化

## Program.cs（エントリポイント / Composition Root）

- 依存の組み立てを集約する唯一の合成起点。`GameConfigLoader.Instance` への直アクセスはここに限定する
- `GameConfigLoader.Instance` で `GameConfig` を一度だけ取得（起動時バリデーション）し、以降は引数で明示注入
- プレイヤー名をコンソール入力（空の場合は "Hero"）
- `CreatePlayer(name, config)` で `ExperienceManager`, `InventoryManager` を注入して `Player` を生成。攻撃戦略名は `AttackStrategyNames.Default` 定数を使用
- `EventManager` を生成し、`Player` / `IGameInput` / `IPlayerRepository?` とともに `GameSystem` にコンストラクタ注入
- `using` で `GameSystem`（IDisposable）を生成し `RunGameLoop()` を実行 → `GameMessageBus` 購読をスコープ終了時に解除
- 例外発生時は `Environment.Exit(1)` で終了

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

- **非推奨（DEPRECATED）**: 後方互換性のために残存
- 全プロパティが `GameConfigLoader.Instance` へ委譲するラッパー
- `AttackDamage` ネストクラスのみ `const` 値を保持（Default/Melee/Magic のダメージ範囲）
- 新規コードでは `GameConfigLoader.Instance` を直接使用すること

## 外部依存パッケージ

- `YamlDotNet 16.3.0` — YAML のデシリアライズ
- `MongoDB.Driver 3.5.0` — セーブデータの永続化

## サブフォルダ

各フォルダの詳細は個別の CLAUDE.md を参照:

- [Factory/CLAUDE.md](./Factory/CLAUDE.md) — EnemyFactory, WeaponFactory, YamlSpecLoader
- [Interfaces/CLAUDE.md](./Interfaces/CLAUDE.md) — IAttackStrategy, ICharacter, IEnemy 等
- [Manager/CLAUDE.md](./Manager/CLAUDE.md) — HealthManager, InventoryManager, ExperienceManager, CombatManager, SaveDataManager
- [Models/CLAUDE.md](./Models/CLAUDE.md) — Player, Enemy, Weapon, AttackStrategy, GameState 等
- [Systems/CLAUDE.md](./Systems/CLAUDE.md) — GameSystem, BattleSystem, ShopSystem
- [Systems/StateMachine/CLAUDE.md](./Systems/StateMachine/CLAUDE.md) — ステートマシン

## ビルド・実行

```bash
# ビルド（ソリューションルートから）
dotnet build

# 実行
dotnet run --project ./GameEngine

# テスト
dotnet test

# セーブ機能を使う場合は事前に MongoDB を起動
docker-compose up -d
```
