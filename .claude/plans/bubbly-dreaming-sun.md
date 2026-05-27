# フェーズ2: 制御フローのステップ駆動化

## Context

`docs/research/07-migration-roadmap.md` のフェーズ2（リスク高・最重要）を実装する。API 化の本質的ブロッカーである「内部 `while` ループ + 同期入力待ち」を解体し、**ゲーム全体を「1行動 → 1ステップ → 状態返却」で外部駆動可能**にする。

現状のブロッキング階層（3つの `while`）:
- ① メインループ — [GameStateMachine.cs](../../GameEngine/Systems/StateMachine/GameStateMachine.cs#L23-L33) の `while`
- ② ショップループ — [EventManager.cs](../../GameEngine/Systems/EventManager.cs#L93-L103) の `while(true)`（`_input.SelectShopAction()` でブロック）
- ③ 戦闘ループ — [BattleManager.cs](../../GameEngine/Systems/BattleSystem/BattleManager.cs#L55-L85) の `while`（`_input.SelectAttackAction()` でブロック）

**採用方針（ユーザー選択: コア統一ステートマシン）**: 戦闘・ショップ・休憩・続行確認をステートマシンの**第一級 State** として組み込み、ゲーム全体を `Step(input)` 駆動の単一機械にする。「進行順序の司令塔」は**コアの State 群**に置く。コンソール / API ホストは「ExpectedInput を見て入力取得 → `Step` → 状態描画」だけの薄いアダプタになる。これは doc03 の『戦闘を独立 State に組み込み』とリスク節『コア共有で二重メンテ回避』に最も忠実。

完了条件: コアから内部ブロッキングループが消え、Step API で外部駆動できる。コンソールは従来どおりプレイできる。

## 設計

### 描画と入力の分離（重要・挙動維持の鍵）
フェーズ1で確立済みの分担を踏襲する:
- **入力 UI 描画**（矢印キーメニュー・プロンプト）→ ホストの `IGameInput` 実装（`ConsoleGameInput` + `ConsoleRenderer`）に残す。**変更しない**。
- **状態/結果描画**（画面クリア・ステータスパネル・HP バー・結果ボックス・ドメインメッセージ）→ コアの State 群が注入 `IRenderer` 経由で描画。`GameMessageBus`→`IRenderer` の即時購読（[GameSystem.cs](../../GameEngine/Systems/GameSystem.cs#L30)）も維持。

→ 結果として描画ロジックの大規模書き換えは不要。コアからは**ブロッキング入力呼び出しのみ**を除去し、行動は `Step` 引数で受け取る。

### 新規ステート機械グラフ
```
StartState        (ExpectedInput.None)      → ExploreState
ExploreState      (None)  BeginEncounter()で種別決定・ショップ報酬付与/StartBattle
                          → (type==Shop) ShopState / (type==Battle) BattleState
BattleState       (Attack) SubmitPlayerTurn を1回。継続=Repeat(自己ループ) /
                          勝利→RestState / 敗北→GameOverState
ShopState         (Shop)   SubmitShopAction を1回。Exit以外=Repeat / Exit→RestState
RestState         (Rest)   SubmitRestAction を1回 → PostEncounterState
PostEncounterState(GameAction) 続行→ExploreState / セーブ&終了・終了→GameOverState
GameOverState     (None)   → null（終了）
```
- State は**ステートレスのまま**（既存規約）。進行中データ（敵・ターン・ShopState・種別）は `BattleManager`/`EventManager`（`GameFlowContext` から参照）が保持。自己ループは `() => new BattleState()` 等で新インスタンス生成で問題ない（進行はコンテキスト側）。
- `None` の State は `Step` 内で**自動前進**（入力不要なので連続実行）し、次に入力を要する State か終端で停止する。1回の `Step`（=API なら1リクエスト）が「次の判断点まで」進める。

### 追加する型
- `ExpectedInput` enum（`None/Attack/Shop/Rest/GameAction`）— 次に必要な入力種別。
- `PlayerInput` キャリア（DTOs）— `Attack/Shop/Rest/Progress(GameActionChoice)` のいずれかを保持。`Step(PlayerInput)` に渡す。API はリクエストボディから生成可能。
- `BattleStepResult`（Outcome=`InProgress/Victory/Defeat/Error` + `BattleState`/`EnemyState`/`PlayerState` + Messages、`IsOver`）— `BattleResult`/`BattleOutcome` を置換。
- `EncounterStart`（種別 + メッセージ + `ShopState?` + `BattleStepResult?`）/ `ShopActionResult`（メッセージ + `ShopState` + `PlayerState` + Exit 済みフラグ）。

## 変更ファイル（コア: GameEngine）

1. **`Systems/BattleSystem/BattleManager.cs`** — ループ解体・**`IRenderer`/`IGameInput` 依存を除去**。
   - 新コンストラクタ `(IPlayer, IEnemyFactory)`。`_enemy`/`_turn` をインスタンス保持。
   - `StartBattle()` → 敵生成・`turn=0`・「A wild X appears!」→ `BattleStepResult(InProgress,…)`。
   - `SubmitPlayerTurn(AttackAction)` → プレイヤー1ターン+敵1ターン、勝敗判定（`GameRecord.RecordWin/Loss`・`player.DefeatEnemy`）。HP 差分から `LastDamageDealt/Taken` も埋める。**描画はしない**（結果ボックス/HPバー/待機は `BattleState` 側へ）。

2. **`Systems/EventManager.cs`** — `while(true)` 解体・**`IRenderer`/`IGameInput` 依存を除去**。
   - 新コンストラクタ `(IPlayer, GameConfig, IEnemyFactory, Random? random = null)`（`random` はテスト決定性用、既定は `new Random()`）。内部で `new BattleManager(player, enemyFactory)`。
   - `BeginEncounter()`（種別決定・ショップ報酬・`ShopSystem.CreateShopState` or `BattleManager.StartBattle`）/ `SubmitShopAction(ShopAction)`（`ShopSystem.ProcessShopAction` 委譲）/ `SubmitBattleTurn(AttackAction)`（`BattleManager` 委譲）/ `SubmitRestAction(UseItemAction?)`（`RestSystem.ProcessRestAction` 委譲）。`CurrentEventType`/現 `ShopState`/現 `BattleStepResult` を公開。
   - 旧 `TriggerRandomEvent/HandleShopEvent/HandleBattleEvent`・`EventResult` を撤去。

3. **`Systems/StateMachine/`**
   - `Trigger.cs` — `Repeat`（自己ループ）を追加。
   - `IGameState.cs` — `ExpectedInput ExpectedInput { get; }` を追加。
   - `GameStateMachine.cs` — 遷移ファクトリを `Func<GameFlowContext, IGameState>?` に一般化（分岐に context を使う）。`CurrentState`/`CurrentStateName`/`IsRunning`/`ExpectedInput` を公開。`while` の代わりに「1 State 実行→次解決」を行う `AdvanceOnce()` と、`None` 連鎖を進める内部ロジックを用意。`Run()` は互換のため残置（テスト不使用だが無害）。
   - `States/` — `EncounterState` を **`ExploreState`** に置換。新規 `BattleState`/`ShopState`/`RestState` を追加。`StartState`/`PostEncounterState`/`GameOverState` は `ExpectedInput` 実装と入力消費に合わせて改修（`PostEncounterState` が `GameActionChoice` を消費しセーブ分岐）。

4. **`Systems/StateMachine/GameFlowContext.cs`** — 現 `PlayerInput`・`CurrentEventType`・現 Battle/Shop 状態の参照・現 `GamePhase`・State 用ビュー構築を追加。旧 `TriggerRandomEvent`/`ConfirmContinue` ブロッキングヘルパーを撤去（セーブ処理 `SaveGameAsync` は `PostEncounterState` 経路へ）。描画ヘルパー（`ClearScreen`/`RenderMessages` 等）は維持。

5. **`Systems/GameSystem.cs`** — 拡張遷移マップを構築。**`IGameInput` を注入**（薄い駆動ループ用。`EventManager` から移譲）。
   - Step プリミティブを公開: `Start()` / `Step(PlayerInput)` / `IsRunning` / `ExpectedInput` / 現ビュー（`PlayerState`/`EnemyState?`/`BattleState?`/`ShopState?`/`GamePhase`）。← API ホスト・回帰テスト用。
   - `RunGameLoop()` を**薄い駆動ループ**として再実装: `Start()` 後 `while(IsRunning){ ExpectedInput に応じ IGameInput.Select* を呼ぶ → Step(PlayerInput) }`。コンソール挙動を維持し、`Program.cs` は無改修。

## 変更ファイル（ホスト: GameEngine.Console）
- 原則**無改修**。`Program.cs` は `GameSystem` 解決 → `RunGameLoop()` のまま。`ConsoleGameInput`/`ConsoleRenderer`/`UserInteraction` も無改修（入力 UI は不変）。
- DI（`AddGameEngine`）は型登録のため、`EventManager`（依存減）・`GameSystem`（`IGameInput` 追加）の解決はホスト登録済み `IGameInput` で自動的に満たされる。`AddGameEngine` 自体の編集は基本不要（必要なら登録順序の確認のみ）。

## テスト（doc が必須とする回帰テスト）
新規 `GameEngine.Tests/Systems/`:
- **ステップ機械の回帰テスト** — 台本化した `IGameInput`（スクリプト入力）+ `NullRenderer` + シード固定 `Random` + 既知敵を返す `IEnemyFactory` モックで Step API を駆動。
  - 戦闘: `BeginEncounter`→Battle、`SubmitBattleTurn` 反復で `InProgress`→`Victory`/`Defeat`、`ExpectedInput` 遷移、敗北で `GameOver` へ。
  - ショップ: `Shop`→`SubmitShopAction(Buy…)`=Repeat、`Exit`→`RestState` へ。
  - 休憩→続行: `Rest` 後 `PostEncounter`、`GameActionChoice.Continue` で `ExploreState` 復帰、`Quit`/`SaveAndQuit` で終了。
  - `BattleManager` 単体: `StartBattle`/`SubmitPlayerTurn` の HP・勝敗・メッセージ。
- 既存 `ServiceCollectionExtensionsTests` はコンストラクタ変更後も解決可能（`StubGameInput` は `IGameInput` 実装済み）。崩れる箇所があれば最小修正。

## ドキュメント（write-docs スキル使用）
更新対象 CLAUDE.md（AI 向け）: `GameEngine/Systems/CLAUDE.md`・`Systems/StateMachine/CLAUDE.md`・`Interfaces/CLAUDE.md`（`IGameInput` がホスト駆動ループ専用になった点）・`GameEngine.Console/CLAUDE.md`・`GameEngine/CLAUDE.md`（Step API 概要）。**`docs/` 配下のユーザー向けドキュメント（research ロードマップ含む）は指示が無いため変更しない**。

## 進め方（サブエージェント方針）
本リファクタは共有インターフェース/DTO/State が密結合で、並列編集は不整合・競合のリスクが高い。よって**コア実装は一貫性確保のため単独で一括実施**し、ビルド成功後に**サブエージェントへ分担委譲**する: (1) 回帰テスト作成、(2) 複数 CLAUDE.md の同時更新。

## 検証
1. `dotnet build`（ソリューション）— 全プロジェクトのコンパイル。
2. `dotnet test` — 既存 + 新規回帰テストの緑化。
3. `dotnet run --project ./GameEngine.Console` — 戦闘・ショップ・休憩・続行/セーブ/終了が従来どおり手で一周できること（/run スキルで起動確認）。

## 既知の許容範囲（挙動の差分）
- 遅延メッセージ（旧実装が戦闘/エンカ末尾に `RenderMessages` で一括出力していた分）は、ステップ駆動化に伴い**ターン/アクション毎の自然なタイミング**で描画される。ゲーム進行・戦闘計算・勝敗・プロンプト・パネル/HPバー/結果ボックスは維持。
