# Manager フォルダ概要

プレイヤーの各サブシステムを管理するクラス群。`Player` が各 Manager を所有し、責務ごとに分離する構成。

## クラス一覧と責務

### HealthManager
- プレイヤーのHP/DPを管理する
- `IEquipmentStatsProvider`（= `InventoryManager`）を注入し、装備ボーナスを加味した `MaxHP` / `TotalDP` を算出
- `EquipmentChanged` イベントを購読し、装備変更時に `CurrentHP` を `MaxHP` でクリップ
- `TakeDamage()`: DP軽減後のダメージを適用（最小0）
- `Heal()`: MaxHP上限までHP回復
- `LevelUp()`: ベースHP/DPを増加し、CurrentHPにも即反映

### InventoryManager
- `IEquipmentStatsProvider` を実装する唯一のクラス
- ゴールド、ポーション数、装備武器を管理
- `EquipWeapon()` で武器変更時に `EquipmentChanged` イベントを発火 → `HealthManager` に通知
- `BuyPotion()` / `UsePotion()` でポーション売買・使用（ゴールド不足/ポーション不足時は警告メッセージ）
- 初期値は `GameConstants.InitialGold` / `GameConstants.InitialPotions`

### ExperienceManager
- 経験値とレベルを管理
- `GainExperience()`: 経験値を加算し、`GameConstants.ExperienceRequiredForLevelUp` に到達したらレベルアップ（戻り値 1）
- 現状レベルアップは1回分のみ処理（超過分の連続レベルアップは未対応）

### CombatManager
- 攻撃の実行と戦略切替を担当
- `ExecuteAttack()`: `IAttackStrategy.ExecuteAttack()` のダメージ + プレイヤーAP で合計ダメージを算出し、対象に適用
- `ChangeAttackStrategy()`: 文字列名で `AttackStrategy.GetAttackStrategy()` から戦略を切替

### RewardManager（CombatManager.cs 内に同居）
- 敵撃破時の報酬処理を一元管理
- `ProcessEnemyDefeat()`: ゴールド獲得 → 経験値獲得 → レベルアップ判定を順次実行
- レベルアップ時は `GameConstants` の増加量で HP/DP/AP を強化

### MongoPlayerRepository（`IPlayerRepository` 実装）
- `IPlayerRepository` インターフェースの MongoDB 実装
- `PlayerSaveData`（`GameEngine.DTOs`）を `playerName` + `saveSlotName` をキーとしてUpsert保存
- BSON マッピングは静的コンストラクタで `BsonClassMap` を使って定義（`PlayerSaveData` 自体は BSON 属性を持たない）
- 主要メソッド: `SaveAsync()`, `LoadAsync()`, `GetSaveListAsync()`, `DeleteAsync()`, `TestConnectionAsync()`
- 前提: Docker Compose で MongoDB を起動しておくこと（`docker-compose up -d`）

### InMemoryPlayerRepository（`IPlayerRepository` 実装）
- テスト用のインメモリ実装
- Dictionary ベースで保存・読み込みを模擬
- MongoDB 不要で単体テストに利用可能

## 依存関係

```
InventoryManager ──implements──> IEquipmentStatsProvider
       │
       │ EquipmentChanged イベント
       ▼
HealthManager ──uses──> IEquipmentStatsProvider (装備ステータス参照)

RewardManager ──uses──> InventoryManager (ゴールド付与)
              ──uses──> ExperienceManager (経験値付与)
              ──uses──> HealthManager (レベルアップ時HP/DP増加)

CombatManager ──uses──> IAttackStrategy (戦略パターン)

MongoPlayerRepository ──implements──> IPlayerRepository
                      ──uses──> MongoDB (外部永続化)
                      ──uses──> PlayerSaveData (DTOs)
                      ──defines──> BsonClassMap (BSON マッピング)

InMemoryPlayerRepository ──implements──> IPlayerRepository (テスト用)
```

## 変更時の注意事項

- `HealthManager` と `InventoryManager` は `EquipmentChanged` イベントで結合しているため、装備関連の変更時は両方への影響を確認すること
- `RewardManager` はレベルアップ時の増加量を `GameConstants` から取得するため、バランス調整は `GameConstants` を変更する
- `IPlayerRepository` の実装を変更する場合は `PlayerSaveData` モデルとの整合性を維持すること
