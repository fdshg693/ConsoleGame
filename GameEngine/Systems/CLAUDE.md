# Systems フォルダ

ゲームの進行制御・イベント処理・ユーザー入力・各サブシステムを集約するレイヤー。

## フォルダ構成

```
Systems/
  GameSystem.cs         # ゲーム全体の起動・メインループ
  EventManager.cs       # ランダムイベント振り分け（戦闘 / ショップ）
  ShopSystem.cs         # ショップ処理（ポーション購入・武器購入）
  RestSystem.cs         # 休憩処理（ポーション使用）
  GameRecord.cs         # 勝敗記録（static）
  BattleSystem/
    BattleManager.cs    # ターン制戦闘ループ
  StateMachine/         # ステートマシンによるゲームループ制御
    CLAUDE.md           # ← 詳細はこちらを参照
```

- コンソール固有 UI（`ConsoleRenderer` / `ConsoleGameInput` / `UserInteraction`）は `GameEngine.Console/UI/` へ移動済み（コア `Systems/` には無い）。コアは `IRenderer` / `IGameInput` 経由で描画・入力を行う。

## 処理フロー

```
GameSystem.RunGameLoop()
  └─ GameStateMachine (StateMachine/ 参照)
       └─ EncounterState
            └─ EventManager.TriggerRandomEvent()
                 ├─ Shop  → ShopSystem + RestSystem
                 └─ Battle → BattleManager + RestSystem
```

- `GameSystem` はエントリポイント。`GameFlowContext` を生成し `GameStateMachine` に委譲する
- `EventManager` が重み付き抽選でイベント種別を決定し、該当サブシステムを呼び出す
- イベント後は必ず `RestSystem` によるポーション使用の機会が提供される

## 主要クラス詳細

### GameSystem（IDisposable）
- コンストラクタ: `GameSystem(IPlayer player, IGameInput input, EventManager eventManager, IRenderer renderer, IGameMessageBus bus, IPlayerRepository? playerRepository = null)`。`GameFlowContext` を組み立てる（`EventManager` は DI で解決・注入される）
- `RunGameLoop()` で `GameStateMachine` を生成・実行する（推奨エントリ）
- `Encounter()` は後方互換用メソッド
- コンストラクタで注入された `IGameMessageBus.MessagePublished` を購読し、発行メッセージを注入された `IRenderer`（`RenderMessage`）へ流す。`Dispose()` で購読を解除する（重複購読・テスト間のイベント漏れ防止）
- `IPlayerRepository` が null（MongoDB 利用不可）の場合、セーブ時に `GameFlowContext` が「利用不可」を通知して続行する

### EventManager
- コンストラクタ: `EventManager(IPlayer player, IGameInput input, GameConfig config, IEnemyFactory enemyFactory, IRenderer renderer)`
- `enemyFactory` / `renderer` を `new BattleManager(player, input, enemyFactory, renderer)` に渡して内部の `BattleManager` を生成
- `TriggerRandomEvent()` でショップまたは戦闘イベントを発生させる
- イベント重みは注入された `_config` から取得（`_config.Events.ShopEventWeight` / `TotalWeight`）
- ショップイベント時はゴールド報酬（ランダム範囲）も付与される
- ショップ呼び出し時はポーション価格 `config.Items.Potion.Price` を `ShopSystem.CreateShopState(potionPrice)` と `ShopSystem.ProcessShopAction(player, action, potionPrice)` に渡す
- `GameEventType` 列挙型に `Treasure` / `Rest` が予約済み（未実装）

### ShopSystem (static)
- `CreateShopState(int potionPrice)` - 武器一覧（SWORD / AXE / BOW）と注入されたポーション価格で `ShopState` を生成
- `ProcessShopAction(IPlayer player, ShopAction action, int potionPrice)` - `ShopAction` に基づきポーション購入・武器装備・退店を処理（価格は引数で受け取り、`GameConstants.PotionPrice` は参照しない）
- `PlayerActionValidator` でバリデーション済み

### RestSystem (static)
- `ProcessRestAction()` - ポーション使用処理
- Potion 以外のアイテムは拒否される
- 所持数不足チェックあり

### GameRecord (static)
- 勝敗数・勝率を保持するグローバル記録
- `RecordWin()` / `RecordLoss()` で更新
- `GetRecordMessages()` で表示用メッセージリストを返す

## BattleSystem サブフォルダ

### BattleManager
- コンストラクタ: `BattleManager(IPlayer player, IGameInput input, IEnemyFactory enemyFactory, IRenderer renderer)`
- ターン制戦闘ループを管理する。画面クリア・ステータスパネル・HP バー・結果ボックス・キー待ちは注入された `IRenderer` 経由で描画する（`ConsoleRenderer` 直呼びはしない）
- `StartBattle()` で注入された `_enemyFactory.CreateRandomEnemy()` を呼び敵を生成（敵注入・決定性のテストシームとなる）
- 各ターンの流れ:
  1. `IGameInput.SelectAttackAction()` でプレイヤーの攻撃戦略を取得（コンソール実装は内部で `UserInteraction.SelectAttackStrategy()` に委譲）
  2. `Player.Attack(enemy)` でプレイヤー攻撃
  3. 敵撃破チェック -> 勝利処理（`GameRecord.RecordWin()` + `Player.DefeatEnemy()`）
  4. `enemy.Attack(player)` で敵攻撃
  5. プレイヤー撃破チェック -> 敗北処理（`GameRecord.RecordLoss()`）
- `BattleResult` クラスで結果（Victory / Defeat / Error）とメッセージを返す

## StateMachine サブフォルダ

詳細は [StateMachine/CLAUDE.md](./StateMachine/CLAUDE.md) を参照。

- ゲームループを明示的なステートマシンで制御する
- `GameSystem.RunGameLoop()` から `StartState` を初期状態として起動
- 状態遷移: `StartState -> EncounterState -> PostEncounterState -> ... -> GameOverState -> 終了`

## 拡張時の注意

- 新しいイベント種別を追加する場合は `EventManager.DetermineEventType()` と `TriggerRandomEvent()` の switch に分岐を追加する
- ショップの武器一覧を変更する場合は `ShopSystem.CreateShopState()` を編集する
- コア `Systems/` から `System.Console` を直接呼ばない。表示は `IRenderer`、入力は `IGameInput` 経由とする
- `IGameInput`（`SelectAttackAction` / `SelectShopAction` / `SelectRestAction` / `SelectGameAction`）のシグネチャ変更時は、`GameEngine.Console/UI/ConsoleGameInput` とテストモック両方の更新が必要
