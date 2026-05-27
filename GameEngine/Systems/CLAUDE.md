# Systems フォルダ

ゲームの進行制御・イベント処理・各サブシステムを集約するレイヤー。進行はステップ駆動（1行動＝1ステップ）で、内部にブロッキングループを持たない。

## フォルダ構成

```
Systems/
  GameSystem.cs         # ステップ駆動エンジン（Start / Step / CaptureSession）＋コンソール用駆動ループ
  EventManager.cs       # エンカウント進行（種別決定・ショップ/戦闘/休憩の各 Submit）
  ShopSystem.cs         # ショップ処理（ポーション購入・武器購入）
  RestSystem.cs         # 休憩処理（ポーション使用）
  GameRecord.cs         # 勝敗記録（IGameRecord 実装・インスタンスベース）
  BattleSystem/
    BattleManager.cs    # ターン制戦闘のステップ進行（描画・入力なし）
  StateMachine/         # ステップ駆動の状態機械
    CLAUDE.md           # ← 詳細はこちらを参照
```

- コンソール固有 UI（`ConsoleRenderer` / `ConsoleGameInput` / `UserInteraction`）は `GameEngine.Console/UI/` にある（コア `Systems/` には無い）。コアは `IRenderer` / `IGameInput` 経由で描画・入力を行う。

## 処理フロー

```
GameSystem.Start()            # 状態機械を生成し最初の入力待ち状態まで前進
GameSystem.Step(PlayerInput)  # 1行動を適用して次の入力待ち状態まで前進（繰り返し呼ぶ）
  └─ GameStateMachine (StateMachine/ 参照)
       Explore → EventManager.BeginEncounter() で種別決定
         ├─ Shop  → Shopping → Rest → PostEncounter
         └─ Battle → BattleTurn → Rest → PostEncounter
```

- `GameSystem` は外部駆動エンジン。`Start()` 後、ホストが `ExpectedInput` を見て対応する行動を `Step()` に渡す
- コアにブロッキング `while` は無い。進行順序は `StateMachine/` の State 群と遷移マップが持つ
- `EventManager` が重み付き抽選で種別を決定し、各 Submit メソッドで該当サブシステムへ委譲する
- エンカウント後は必ず休憩（ポーション使用の機会）→続行確認へ進む

## 主要クラス詳細

### GameSystem（IDisposable）
- コンストラクタ: `GameSystem(IPlayer player, IGameInput input, EventManager eventManager, IRenderer renderer, IGameMessageBus bus, IPlayerRepository? playerRepository = null)`
- ステップ駆動 API（API ホスト・テスト共通）:
  - `Start()` — `GameFlowContext` と `GameStateMachine` を生成し、最初の入力待ち状態（または終端）まで前進
  - `Step(PlayerInput input)` — 1行動を適用して1ステップ進める
  - `IsRunning` / `ExpectedInput` / `CurrentStateName`
  - 状態 DTO アクセサ: `CurrentPlayerState` / `CurrentBattleState` / `CurrentEnemyState` / `CurrentShopState`
  - `CaptureSession(string sessionId)` — 現在の進行状態（フェーズ・ステート名・ExpectedInput・プレイヤー `PlayerSaveData`・戦闘途中の敵HP/ターン・ショップ状態・勝敗数）を `GameSessionState` としてスナップショット化（フェーズ3。`ISessionRepository` でリクエスト間に保持・復元するための入口）
- `RunGameLoop()` — コンソールホスト用の薄い駆動ループ。`Start()` 後 `while(IsRunning)` で `ExpectedInput` に応じ `IGameInput.Select*` から行動を取得し `PlayerInput` に包んで `Step()` する。遷移マップ（`(Type, Trigger) → Func<GameFlowContext, IGameState>?`）も内部で構築する
- 注入された `IGameMessageBus.MessagePublished` を購読し、発行メッセージを `IRenderer.RenderMessage` へ流す。`Dispose()` で購読解除（重複購読・テスト間のイベント漏れ防止）
- `IPlayerRepository` が null（MongoDB 利用不可）の場合、セーブ時に `GameFlowContext` が「利用不可」を通知して続行する

### EventManager
- コンストラクタ: `EventManager(IPlayer player, GameConfig config, IEnemyFactory enemyFactory, IGameRecord gameRecord, Random? random = null)`（描画・入力に非依存。`random` 既定は `new Random()`）
- 内部で `new BattleManager(player, enemyFactory, gameRecord)` を生成して戦闘進行を委譲する
- `GameRecord`（`IGameRecord`）プロパティで勝敗記録を公開（`GameFlowContext.DisplayGameOver` / `GameSystem.CaptureSession` が参照）
- 進行データを保持: `CurrentEventType` / `CurrentShopState` / `CurrentBattleResult` / `CurrentEnemy`
- メソッド（1アクションずつ外部から呼ぶ）:
  - `BeginEncounter()` → `EncounterStart`（種別を重み付き抽選で決定。ショップなら発見ボーナス付与＋`ShopState` 生成、戦闘なら `StartBattle()` で初期戦闘状態を返す）
  - `SubmitShopAction(ShopAction)` → `ShopActionResult`（`ShopSystem.ProcessShopAction` へ委譲。`Exit` 受信を `Exited` で返す）
  - `SubmitBattleTurn(AttackAction)` → `BattleStepResult`（`BattleManager.SubmitPlayerTurn` へ委譲）
  - `SubmitRestAction(UseItemAction?)` → `List<GameMessage>`（`RestSystem.ProcessRestAction` へ委譲。null はスキップ）
- イベント重みは注入された `_config.Events`（`ShopEventWeight` / `TotalWeight`）から取得
- ショップ呼び出し時はポーション価格 `config.Items.Potion.Price` を `ShopSystem` に渡す
- `GameEventType` 列挙型に `Treasure` / `Rest` が予約済み（未実装）

### ShopSystem (static)
- `CreateShopState(int potionPrice)` - 武器一覧（SWORD / AXE / BOW）と注入されたポーション価格で `ShopState` を生成
- `ProcessShopAction(IPlayer player, ShopAction action, int potionPrice)` - `ShopAction` に基づきポーション購入・武器装備・退店を処理（価格は引数で受け取る）
- `PlayerActionValidator` でバリデーション済み

### RestSystem (static)
- `ProcessRestAction()` - ポーション使用処理
- Potion 以外のアイテムは拒否される
- 所持数不足チェックあり

### GameRecord（`IGameRecord` 実装・インスタンスベース）
- 勝敗数・勝率を保持。静的状態を排除し並行リクエストでの混線を防ぐ（`AddGameEngine` が Singleton 登録）
- `RecordWin()` / `RecordLoss()` で更新、`Restore(wins, losses)` で復元、`GetRecordMessages()` で表示用メッセージを返す
- `BattleManager` が DI 注入インスタンスで勝敗を記録する（静的呼び出しではない）

## BattleSystem サブフォルダ

### BattleManager
- コンストラクタ: `BattleManager(IPlayer player, IEnemyFactory enemyFactory, IGameRecord gameRecord)`（描画・入力に非依存）
- ターン制戦闘をステップ進行する。内部 while ループは持たず、結果は `BattleStepResult` として返す（描画はホスト側 State が担う）
- `StartBattle()` - `_enemyFactory.CreateRandomEnemy()` で敵を1体生成し初期 `BattleStepResult`（InProgress）を返す（敵注入・決定性のテストシーム）
- `SubmitPlayerTurn(AttackAction)` - プレイヤー1ターン＋敵1ターンを進める:
  1. 攻撃戦略を検証（不正なら Default にフォールバック）
  2. `Player.Attack(enemy)` でプレイヤー攻撃
  3. 敵撃破チェック → 勝利処理（注入された `IGameRecord.RecordWin()` + `Player.DefeatEnemy()`）
  4. `enemy.Attack(player)` で敵攻撃
  5. プレイヤー撃破チェック → 敗北処理（注入された `IGameRecord.RecordLoss()`）
- `CurrentEnemy` / `TurnNumber` / `IsBattleActive` を公開
- `BattleStepResult` - `Outcome`（`BattleOutcome` enum: InProgress / Victory / Defeat / Error）と状態 DTO（`Battle` / `Enemy` / `Player`）・`Messages` を保持。判定ヘルパー `IsOver` / `IsVictory` / `IsDefeat` / `IsError`

## StateMachine サブフォルダ

詳細は [StateMachine/CLAUDE.md](./StateMachine/CLAUDE.md) を参照。

- ステップ駆動の状態機械でゲームループを制御する。`GameSystem.Start()` / `Step()` から駆動される
- 各状態は `ExpectedInput` を宣言し、`None` の状態はマシンが自動前進する
- 状態: `StartState → ExploreState → (BattleTurnState | ShoppingState) → RestState → PostEncounterState → ...（続行で ExploreState へ） → GameOverState → 終了`

## 拡張時の注意

- 新しいイベント種別を追加する場合は `EventManager.BeginEncounter()` の分岐と `_config.Events` の重み付き抽選（`DetermineEventType`）を更新し、必要なら対応する State・遷移マップエントリを追加する
- ショップの武器一覧を変更する場合は `ShopSystem.CreateShopState()` を編集する
- コア `Systems/` から `System.Console` を直接呼ばない。表示は `IRenderer`、入力は `IGameInput` 経由とする
- `IGameInput`（`SelectAttackAction` / `SelectShopAction` / `SelectRestAction` / `SelectGameAction`）のシグネチャ変更時は、`GameEngine.Console/UI/ConsoleGameInput` とテストモック両方の更新が必要
