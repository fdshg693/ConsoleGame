# StateMachine

ゲームループをステップ駆動の状態機械で制御するモジュール。内部にブロッキングループを持たず、`GameSystem.Start()` / `Step()` から1行動ずつ駆動される。遷移ルールは `GameSystem` が構築する遷移マップに集約されている。

## 構成

- **ExpectedInput**（`DTOs/StepContracts.cs`）- 次の `Step` で必要な入力種別 enum（`None` / `Attack` / `Shop` / `Rest` / `GameAction`）。`None` の状態はマシンが自動前進する
- **Trigger** - ステートが返すトリガー enum（`Continue` / `Repeat`（自己ループ）/ `EndGame` / `Done`）
- **IGameState** - 状態インターフェース。`Name` / `ExpectedInput` / `Prepare(GameFlowContext)`（既定 no-op）/ `Execute(GameFlowContext) → Trigger`
- **GameStateMachine** - 遷移マップ `Dictionary<(Type, Trigger), Func<GameFlowContext, IGameState>?>` を受け取り、トリガーから次ステートを解決する。状態自体は保持データを持たない
- **GameFlowContext** - 全状態が共有する実行コンテキスト（下記）

## Prepare / Execute の分離

- **Prepare(context)** - 入力取得の直前に呼ばれ、入力前に見せる画面を描画する。既定は no-op。`BattleTurnState`（ターン見出し＋ステータスパネル）と `PostEncounterState`（クリア＋プレイヤー情報）のみ実装。`None` 状態では呼ばれない
- **Execute(context)** - 状態処理を実行し `Trigger` を返す。入力を要する状態は `context.CurrentInput` から行動を読み取る

## GameStateMachine の駆動

- `Start()` - 初期状態（`StartState`）から `None` 状態を自動前進させ、最初の入力待ち状態（または終端）まで進めて `Prepare()` を呼ぶ
- `Step(input)` - `context.CurrentInput` に input を設定して現在状態を実行し、続く `None` 状態を自動前進させ、次の入力待ち状態で `Prepare()` を呼ぶ
- 公開: `CurrentState` / `CurrentStateName` / `IsRunning` / `ExpectedInput`
- 遷移マップはコンテキスト参照可能なファクトリ（`Func<GameFlowContext, IGameState>?`）で、`ExploreState` の戦闘/ショップ分岐を可能にする。未定義の遷移は `InvalidOperationException`

## 状態一覧と遷移マップ

| 現在のステート | ExpectedInput | Trigger | 次のステート |
|---|---|---|---|
| StartState | None | Continue | ExploreState |
| ExploreState | None | Continue | BattleTurnState または ShoppingState（`CurrentEventType` で分岐） |
| ExploreState | None | EndGame | GameOverState（戦闘開始エラー時） |
| BattleTurnState | Attack | Repeat | BattleTurnState（戦闘継続・自己ループ） |
| BattleTurnState | Attack | Continue | RestState（勝利） |
| BattleTurnState | Attack | EndGame | GameOverState（敗北） |
| ShoppingState | Shop | Repeat | ShoppingState（買い物継続・自己ループ） |
| ShoppingState | Shop | Continue | RestState（退店） |
| RestState | Rest | Continue | PostEncounterState |
| PostEncounterState | GameAction | Continue | ExploreState（続行/セーブ続行） |
| PostEncounterState | GameAction | EndGame | GameOverState（セーブ終了/終了） |
| GameOverState | None | Done | null（終了） |

## 各ステートの責務

- **StartState** - 開始メッセージとプレイヤー情報を表示（自動前進）
- **ExploreState** - `EventManager.BeginEncounter()` で種別を決定（自動前進）。戦闘開始エラー時は `EndGame`
- **BattleTurnState** - `SubmitBattleTurn` で1ターン進行。勝利/敗北は結果ボックス表示＋キー待ち
- **ShoppingState** - `SubmitShopAction` で1アクション処理。`Exit` 受信で退店
- **RestState** - `SubmitRestAction`（null＝スキップ）でアイテム使用を1アクション処理
- **PostEncounterState** - `GameActionChoice`（Continue / SaveAndContinue / SaveAndQuit / Quit）で分岐。セーブ系は `context.SaveGame()` を呼ぶ
- **GameOverState** - 最終結果を表示して終端（自動前進）

## GameFlowContext

- コンストラクタ: `GameFlowContext(IPlayer player, EventManager eventManager, IPlayerRepository? playerRepository, IRenderer renderer)`
- 状態はステートレスに保ち、進行中データ（敵・ターン・ショップ状態・種別）は `EventManager` に集約する
- 主なメンバー:
  - `CurrentInput`（settable `PlayerInput`。マシンがステップ実行前に設定）
  - `CurrentEventType`、状態 DTO アクセサ `CurrentPlayerState` / `CurrentBattleState` / `CurrentEnemyState` / `CurrentShopState`
  - `Renderer` と描画ヘルパー `ClearScreen` / `RenderMessages` / `WriteLine` / `LogTransition`（すべて `IRenderer` 経由）
  - `SaveGame()`（`IPlayerRepository.SaveAsync` の同期ラッパー。未登録なら「利用不可」を通知して続行）
  - `DisplayGameOver()`（勝敗集計は `EventManager.GameRecord`〔`IGameRecord`〕経由で取得）/ `ShowPlayerInfo()`

## エントリポイント

- `GameSystem.Start()` で遷移マップを構築し `StartState` を初期状態に `GameStateMachine` を生成・開始する。以降 `GameSystem.Step()` が `Step()` を駆動する

## 拡張時の注意

- 新しい状態を追加する場合: `IGameState` を実装し（`ExpectedInput` を宣言、必要なら `Prepare`）、`Trigger` に必要な値を追加し、`GameSystem` の遷移マップにエントリを追加する
- 状態の共有データは `GameFlowContext`（および `EventManager`）に集約する（状態クラス自体はステートレスに保つ）
- 入力前の画面描画が必要な状態は `Prepare()` をオーバーライドする（`Execute()` 内では行動の処理に集中する）
