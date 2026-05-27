# Models

ドメインモデル（Entity / Value Object）、戦略パターン実装、メッセージバスを定義。

## ファイル構成

### ドメインモデル

- **Player.cs** - プレイヤーエンティティ（`IPlayer` 実装）
  - Manager パターンで責務分離: `HealthManager`, `InventoryManager`, `ExperienceManager`, `CombatManager`, `RewardManager`
  - `Level`, `TotalExperience`, `EquippedWeaponName` を `IPlayer` 経由で直接公開
  - `GetSaveData()` で `PlayerSaveData`（`GameEngine.DTOs`）を生成
- **Enemy.cs** - 敵エンティティ（`IEnemy` 実装）
  - `IAttackStrategy` を保持、`ChangeAttackStrategy()` で戦闘中に切替可能
- **Weapon.cs** - 武器の値オブジェクト（`IWeapon` 実装、読み取り専用）

### 戦略パターン

- **AttackStrategy.cs** - `IAttackStrategy` 実装群 + ファクトリ
  - `DefaultAttackStrategy` / `MeleeAttackStrategy` / `MagicAttackStrategy`
  - 戦略名は `AttackStrategyNames`（`GameEngine.Constants`）で一元管理
  - 新戦略追加手順: クラス実装 → `GetAttackStrategy()` switch 追加 → `AttackStrategyNames` 更新 → YAML 名と一致させる

### メッセージバス

- **GameMessageBus.cs** - 静的イベントバス + `GameMessage` / `MessageType` 定義
  - `Publish()` でドメインイベントを発行、UI層が `MessagePublished` イベントで購読
  - `GameMessage` と `MessageType` はドメイン層から直接参照されるためここに配置

## 設計上の注意点

- **GameMessageBus は静的**: テスト時のイベント分離に注意
- **Player の Manager 依存**: コンストラクタ注入のため、テスト時はモック可能
- DTO・コマンド・永続化モデルは `GameEngine.DTOs` に、マッパーは `GameEngine.Mappers` に分離済み
