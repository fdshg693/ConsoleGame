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
  ConsoleGameInput.cs   # IGameInput のコンソール実装
  UserInteraction.cs    # コンソール入力ユーティリティ（static）
  BattleSystem/
    BattleManager.cs    # ターン制戦闘ループ
  StateMachine/         # ステートマシンによるゲームループ制御
    CLAUDE.md           # ← 詳細はこちらを参照
```

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
- `Player` / `IGameInput` / `EventManager` / `IPlayerRepository?` をコンストラクタ注入で受け取り、`GameFlowContext` を組み立てる（`EventManager` は Program.cs で生成・注入される）
- `RunGameLoop()` で `GameStateMachine` を生成・実行する（推奨エントリ）
- `Encounter()` は後方互換用メソッド
- コンストラクタで `GameMessageBus.MessagePublished` を購読し、`Dispose()` で解除する（重複購読・テスト間のイベント漏れ防止）
- `IPlayerRepository` が null（MongoDB 利用不可）の場合、セーブ時に `GameFlowContext` が「利用不可」を通知して続行する

### EventManager
- `TriggerRandomEvent()` でショップまたは戦闘イベントを発生させる
- `GameConfig` をコンストラクタ注入で受け取る（旧: `GameConfigLoader.Instance` 直アクセス）
- イベント重みは注入された `_config` から取得（`_config.Events.ShopEventWeight` / `TotalWeight`）
- ショップイベント時はゴールド報酬（ランダム範囲）も付与される
- `GameEventType` 列挙型に `Treasure` / `Rest` が予約済み（未実装）

### ConsoleGameInput
- `IGameInput` インターフェースのコンソール向け実装
- テスト時はモックに差し替え可能
- 戦闘行動: `UserInteraction.SelectAttackStrategy()` に委譲
- ショップ行動: 数字キー（1/2/3）で操作
- 休憩行動: `ReadPositiveInteger()` でポーション数入力

### UserInteraction (static)
- コンソール入力の共通ユーティリティ
- `ReadPositiveInteger()` - 範囲付き正整数入力。最大試行回数 5 回、"Q" でスキップ
- `ReadConfirmation()` - Yes/No 入力（"はい"/"いいえ" にも対応）
- `ReadChoice()` - 番号選択式メニュー
- `SelectAttackStrategy()` - 矢印キーで戦略を切り替え、Enter で確定。ANSI エスケープで行を書き換える
- `SelectGameAction()` - 上下キーで続行/セーブ/終了を選択

### ShopSystem (static)
- `CreateShopState()` - 武器一覧（SWORD / AXE / BOW）とポーション価格で `ShopState` を生成
- `ProcessShopAction()` - `ShopAction` に基づきポーション購入・武器装備・退店を処理
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
- ターン制戦闘ループを管理する
- `StartBattle()` で `EnemyFactory.CreateRandomEnemy()` を呼び敵を生成
- 各ターンの流れ:
  1. `IGameInput.SelectAttackAction()` でプレイヤーの攻撃戦略を取得
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
- `UserInteraction` のキー操作はANSIエスケープに依存するため、非対応ターミナルでは表示が崩れる可能性がある
- `IGameInput` のシグネチャ変更時は `ConsoleGameInput` とテストモック両方の更新が必要
