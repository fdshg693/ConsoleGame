# Models

ドメインモデル（Entity / Value Object）、戦略パターン実装、メッセージバスを定義。

## ファイル構成

### ドメインモデル

- **Player.cs** - プレイヤーエンティティ（`IPlayer` 実装）
  - コンストラクタ: `Player(string name, GameConfig config, IAttackStrategy attackStrategy, ExperienceManager experienceManager, InventoryManager inventoryManager, IGameMessageBus bus)`
  - `bus`（`IGameMessageBus`）はメッセージ発行に注入され、内部生成する `RewardManager` にも伝播
  - 注入された `GameConfig` から値を導出:
    - `BaseAP` ← `config.Player.BaseAP`
    - `HealthManager` の初期HP/基礎DP ← `config.Player.InitialHP` / `config.Player.BaseDP`
    - ポーション回復量（`UsePotion`） ← `config.Items.Potion.HealAmount`
    - レベルアップ増加値（`RewardManager` へ） ← `config.LevelUp.HPIncrease` / `DPIncrease` / `APIncrease`
  - Manager パターンで責務分離: `HealthManager`, `InventoryManager`, `ExperienceManager`, `CombatManager`, `RewardManager`
  - `Level`, `TotalExperience`, `EquippedWeaponName` を `IPlayer` 経由で直接公開
  - `GetSaveData()` で `PlayerSaveData`（`GameEngine.DTOs`）を生成
- **Enemy.cs** - 敵エンティティ（`IEnemy` 実装）
  - コンストラクタ: `Enemy(string name, int hp, IAttackStrategy attackStrategy, int experience, int aP, int dP, int yieldGold, IGameMessageBus bus)`
  - `bus`（`IGameMessageBus`）はメッセージ発行に注入（`EnemyFactory` が伝播）
  - `YieldGold` はコンストラクタ引数で受け取る（`EnemyFactory` が算出）
  - `IAttackStrategy` を保持、`ChangeAttackStrategy()` で戦闘中に切替可能
- **Weapon.cs** - 武器の値オブジェクト（`IWeapon` 実装、読み取り専用）

### 戦略パターン

- **AttackStrategy.cs** - `IAttackStrategy` 実装群 + ファクトリ
  - `DefaultAttackStrategy` / `MeleeAttackStrategy` / `MagicAttackStrategy`
  - ダメージは `Random.Shared.Next()` で算出（共有インスタンスで連続生成時の同一シード問題を回避）
  - ダメージ範囲は `GameConstants.AttackDamage` の `const` 値（Default/Melee/Magic）
  - 戦略名は `AttackStrategyNames`（`GameEngine.Constants`）で一元管理
  - 新戦略追加手順: クラス実装 → `GetAttackStrategy()` switch 追加 → `AttackStrategyNames` 更新 → YAML 名と一致させる

### メッセージバス

- **GameMessageBus.cs** - インスタンスベースのメッセージバス（`IGameMessageBus` 実装） + `GameMessage` / `MessageType` 定義
  - 静的実装は並行リクエストで購読が混線するため、DI スコープ単位のインスタンスとして扱う（`AddGameEngine` が `IGameMessageBus` → `GameMessageBus` を Singleton 登録）
  - 発行側（`Player`・各 Manager・`Enemy`）にコンストラクタ注入され、`Publish()` でドメインイベントを発行。`GameSystem` が `MessagePublished` イベントを購読し出力シンク（`IRenderer`）へ流す
  - `GameMessage` と `MessageType` はドメイン層から直接参照されるためここに配置

## 設計上の注意点

- **GameMessageBus はインスタンスベース**: `IGameMessageBus` を発行側に注入する。DI で Singleton 登録され、テスト時は専用インスタンスで購読を分離できる
- **Player の Manager 依存**: コンストラクタ注入のため、テスト時はモック可能
- DTO・コマンド・永続化モデルは `GameEngine.DTOs` に、マッパーは `GameEngine.Mappers` に分離済み
