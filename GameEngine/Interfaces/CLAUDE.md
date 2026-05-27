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
- `SelectGameAction()` - エンカウント後の進行アクション（続行/セーブ/終了）を `GameActionChoice`（`GameEngine.DTOs`）で返す
- 役割: コアのステップ駆動ループからは呼ばれない。コンソールホストの薄い駆動ループ（`GameSystem.RunGameLoop`）が `ExpectedInput` を見て対応する `Select*` を呼び、戻り値を `PlayerInput` に包んで `GameSystem.Step` に渡すためだけに使う
- API ホストは本インターフェースを使わず、リクエストボディから `PlayerInput` を組み立てて `Step` を呼ぶ
- 実装: `GameEngine.Console/UI/ConsoleGameInput`

### IRenderer
- 出力（描画）の抽象。コアは `System.Console` に直接依存せず本インターフェース経由で表示
- コンソールホストは ANSI ベース実装、API ホストはバッファ/DTO 蓄積実装を提供
- メソッド: `ClearScreen()`, `WaitForKeyPress()`, `RenderMessage()`, `RenderMessages()`, `WriteInfo/Success/Warning/Error/System()`, `RenderHPBar()`, `RenderStatusPanel()`, `WriteSeparator()`, `WriteResultBox()`
- コアが実際に呼ぶメソッドのみ公開。矢印キー選択・装飾ボックス等のコンソール固有描画は実装側に閉じ込める
- 実装: `GameEngine.Console/UI/ConsoleRenderer`（API はバッファ実装予定）

### IGameMessageBus
- ドメインメッセージの発行/購読バス（インスタンスベース）
- 静的イベントだと並行リクエスト時に購読が混線するため、DI スコープ単位のインスタンスで扱う
- `event Action<GameMessage>? MessagePublished` / `Publish(string, MessageType)` / `Publish(GameMessage)`
- 発行側（`Player`・各 Manager・`Enemy`）に注入され、購読側（出力シンク）は `GameSystem` が接続
- 実装: `Models/GameMessageBus`（`AddGameEngine` が Singleton 登録）

### IEnemyFactory
- 敵生成を抽象化し、テスト時にインメモリ実装/モックへ差し替えるための継ぎ目（seam）
- `Create(string key)` - キー指定で敵を生成
- `CreateRandomEnemy()` - 登録済みの敵からランダムに1体生成
- `GetAvailableEnemyKeys()` - 利用可能な敵キー一覧を取得
- 実装: `Factory/EnemyFactory`（`GameEngine.Factory`）

## 変更時の注意点

- `IAttackStrategy` の新規実装を追加する場合は、`Models/AttackStrategy.cs` のマッピングと `GameEngine.Console/UI/UserInteraction.cs` の UI 選択肢も更新すること
- `IGameInput` のメソッドシグネチャを変更する場合は、`GameEngine.Console/UI/ConsoleGameInput` とテスト用モックの両方を更新すること
- `IEquipmentStatsProvider.EquipmentChanged` イベントは `HealthManager` がリッスンしているため、発火タイミングに注意
