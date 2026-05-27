# Interfaces フォルダ

本フォルダはゲームエンジン全体の契約（インターフェース）を定義する。
実装クラスは `Models/`・`Manager/`・`Systems/` 等に配置される。

## インターフェース階層

```
ICharacter
├── IPlayer
└── IEnemy
```

- `ICharacter` は全キャラクター共通の振る舞い（HP・攻撃・被ダメ・回復・戦略変更）を定義
- `IPlayer` / `IEnemy` はそれぞれプレイヤー固有・敵固有の操作を追加

## 各インターフェースの責務

### ICharacter
- 全キャラクターの基底インターフェース
- `Name`, `HP`, `IsAlive` プロパティ
- `Attack()`, `TakeDamage()`, `Heal()`, `ChangeAttackStrategy()` メソッド

### IPlayer (extends ICharacter)
- プレイヤー固有の操作: 敵撃破報酬 (`DefeatEnemy`)、ゴールド管理、ポーション売買・使用、武器装備
- `GetSaveData()` でセーブデータ (`PlayerSaveData`) を生成

### IEnemy (extends ICharacter)
- 敵固有のプロパティ: `YieldExperience`, `YieldGold`, `MaxHP`
- `AttackStrategy` プロパティで攻撃戦略を保持

### IAttackStrategy (Strategy パターン)
- 攻撃アルゴリズムを差し替え可能にする
- `ExecuteAttack()` でダメージ値を返す
- `GetAttackStrategyName()` で戦略名を返す（YAML の名前と一致させること）
- 実装: `Models/AttackStrategy.cs` 内の `Default` / `Melee` / `Magic` 等

### IWeapon
- 武器のステータスを定義: `HP`, `AP`（攻撃力）, `DP`（防御力）, `Name`
- `WeaponFactory` で生成され、`IPlayer.EquipWeapon()` で装備

### IEquipmentStatsProvider
- 装備からステータスボーナスを取得するための抽象
- `Weapon` プロパティと `EquipmentChanged` イベントを公開
- 主な実装: `Manager/InventoryManager`
- `HealthManager` がこのインターフェース経由で装備ボーナスを参照

### IGameInput
- UI/入力層の抽象化（テスト時にモック差し替え可能）
- `SelectAttackAction()` - 戦闘中の行動選択
- `SelectShopAction()` - ショップでの行動選択
- `SelectRestAction()` - 休憩時のアイテム使用選択
- 実装: `Systems/ConsoleGameInput`

## 変更時の注意点

- `IAttackStrategy` の新規実装を追加する場合は、`Models/AttackStrategy.cs` のマッピングと `Systems/UserInteraction.cs` の UI 選択肢も更新すること
- `IGameInput` のメソッドシグネチャを変更する場合は、`ConsoleGameInput` とテスト用モックの両方を更新すること
- `IEquipmentStatsProvider.EquipmentChanged` イベントは `HealthManager` がリッスンしているため、発火タイミングに注意
