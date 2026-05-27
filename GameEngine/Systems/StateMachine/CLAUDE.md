# StateMachine

ゲームループを明示的なステートマシンで制御するモジュール。遷移ルールは `GameSystem.RunGameLoop()` の遷移マップに集約されている。

## 構成

- **Trigger** - ステートが返すトリガー enum（`Continue` / `EndGame` / `Done`）
- **IGameState** - 状態インターフェース。`Execute()` で処理を行い `Trigger` を返す
- **GameStateMachine** - 遷移マップ `Dictionary<(Type, Trigger), Func<IGameState>?>` を受け取り、トリガーから次ステートを解決するループ
- **GameFlowContext** - 全状態が共有する実行コンテキスト（Player・EventManager・Input・SaveDataManager・ヘルパー群）

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

- **StartState** - 開始メッセージとプレイヤー情報を表示
- **EncounterState** - `EventManager.TriggerRandomEvent()` でイベント（戦闘/ショップ）を実行
- **PostEncounterState** - プレイヤー情報表示 + 続行確認（続行/セーブ/終了）
- **GameOverState** - 最終結果を表示

## エントリポイント

- `GameSystem.RunGameLoop()` で遷移マップを構築し、`StartState` を初期状態として `GameStateMachine` を生成・実行する

## 拡張時の注意

- 新しい状態を追加する場合: `IGameState` を実装し、`Trigger` に必要な値を追加し、`GameSystem.RunGameLoop()` の遷移マップにエントリを追加する
- 状態の共有データは `GameFlowContext` に集約する（状態クラス自体はステートレスに保つ）
- 未定義の遷移が発生すると `InvalidOperationException` がスローされる
