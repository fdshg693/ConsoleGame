# StateMachine

ゲームループを明示的なステートマシンで制御するモジュール。遷移ルールは `GameSystem.RunGameLoop()` の遷移マップに集約されている。

## 構成

- **Trigger** - ステートが返すトリガー enum（`Continue` / `EndGame` / `Done`）
- **IGameState** - 状態インターフェース。`Execute()` で処理を行い `Trigger` を返す
- **GameStateMachine** - 遷移マップ `Dictionary<(Type, Trigger), Func<IGameState>?>` を受け取り、トリガーから次ステートを解決するループ
- **GameFlowContext** - 全状態が共有する実行コンテキスト（Player・EventManager・Input・IPlayerRepository?・IRenderer・ヘルパー群）。描画は注入された `IRenderer` 経由で行う（旧 `renderMessages` デリゲートは廃止）

## 遷移マップ（`GameSystem.RunGameLoop()` で定義）

| 現在のステート | Trigger | 次のステート |
|---|---|---|
| StartState | Continue | EncounterState |
| EncounterState | Continue | PostEncounterState |
| EncounterState | EndGame | GameOverState |
| PostEncounterState | Continue | EncounterState |
| PostEncounterState | EndGame | GameOverState |
| GameOverState | Done | null（終了） |

## 各ステートの責務

- **StartState** - `context.ClearScreen(...)` 後に開始メッセージとプレイヤー情報を表示
- **EncounterState** - `context.ClearScreen(...)` 後に `EventManager.TriggerRandomEvent()` でイベント（戦闘/ショップ）を実行
- **PostEncounterState** - `context.ClearScreen(...)` 後にプレイヤー情報表示 + `context.ConfirmContinue()` で続行確認（続行/セーブ/終了）
- **GameOverState** - `context.ClearScreen(...)` 後に最終結果を表示
- 各 State は画面クリアに `context.ClearScreen(string)` ヘルパーを使う（`ConsoleRenderer` を直接呼ばない）

## GameFlowContext の主なヘルパー

- `ClearScreen(string title)` - 注入された `IRenderer.ClearScreen()` に委譲（各 State が利用）
- `RenderMessages()` / `WriteLine()` / `LogTransition()` - すべて `IRenderer` 経由で描画
- `ConfirmContinue()` - `Input.SelectGameAction()` が返す `GameActionChoice`（Continue / SaveAndContinue / SaveAndQuit / Quit）で分岐。セーブ系は `IPlayerRepository.SaveAsync()` を呼ぶ（未注入なら「利用不可」を通知して続行）

## エントリポイント

- `GameSystem.RunGameLoop()` で遷移マップを構築し、`StartState` を初期状態として `GameStateMachine` を生成・実行する

## 拡張時の注意

- 新しい状態を追加する場合: `IGameState` を実装し、`Trigger` に必要な値を追加し、`GameSystem.RunGameLoop()` の遷移マップにエントリを追加する
- 状態の共有データは `GameFlowContext` に集約する（状態クラス自体はステートレスに保つ）
- 未定義の遷移が発生すると `InvalidOperationException` がスローされる
