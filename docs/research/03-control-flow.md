# 03. 制御フローの非ブロッキング化

API化の核心。**「内部whileループ + 入力待ち」で完結している制御を、外部から1ステップずつ駆動できる形に分解する**。

## 現状のブロッキング階層

```
GameStateMachine.Run()                         while(_currentState != null)  ← ①最外ループ
  └─ State.Execute(context) : Trigger           各Stateを同期実行
      ├─ EncounterState
      │   └─ EventManager.TriggerRandomEvent()
      │       ├─ HandleShopEvent()              while(true)                   ← ②ショップループ
      │       │   └─ _input.SelectShopAction()  Console.ReadKey() でブロック
      │       └─ BattleManager.StartBattle()
      │           └─ ExecuteBattle()            while(player.IsAlive && ...)  ← ③戦闘ループ
      │               └─ _input.SelectAttackAction()  Console.ReadKey() でブロック
      └─ PostEncounterState.Execute()           続行確認 Console.ReadKey() でブロック
```

| # | ループ箇所 | ファイル |
|---|---|---|
| ① | メインループ | `Systems/StateMachine/GameStateMachine.cs:23-33` |
| ② | ショップループ | `Systems/EventManager.cs:91-101` |
| ③ | 戦闘ループ | `Systems/BattleSystem/BattleManager.cs:53-83` |

主なブロッキング入力点: `ConsoleRenderer.SelectFromMenu()`（`ConsoleRenderer.cs:268`）、`ConsoleRenderer.WaitForKeyPress()`、`UserInteraction` の `Console.ReadLine`、`Program.cs:27`（名前入力）。

## 課題

- HTTPは「1リクエストで処理を進め、レスポンスを返して終わる」。**ループの途中で入力待ちのまま制御を保持できない**。
- 状態（戦闘の何ターン目か、ショップの何回目の選択か）が **ローカル変数やコールスタックに埋まっている**ため、リクエストをまたいで復元できない。

## 変更方針: ステップ駆動エンジン

ゲーム進行を **「現在状態（フェーズ）」+「行動を1つ受け取って1ステップ進める関数」** に再構成する。

### A. 状態機械を外部駆動可能にする

- `GameStateMachine.Run()` の `while` をやめ、**`Step(action)` / `Advance()` メソッドに分解**する。
  - 1回の呼び出しで「1つの行動を適用 → 状態を更新 → 次に必要な入力種別を提示」して戻る。
  - 現在のState名（Start/Encounter/Battle/Shop/PostEncounter/GameOver）を **保持・公開**する（現状 `GameFlowContext` は現在Stateを保持していない）。

### B. 戦闘ループを「戦闘ステート」へ分解

- `BattleManager.ExecuteBattle()` の `while` を解体し、以下に分ける:
  - `StartBattle(enemy)` → 敵生成・初期 `BattleState` を返す。
  - `SubmitPlayerTurn(AttackAction)` → プレイヤー1ターン + 敵1ターンを進め、更新後の `BattleState`/`EnemyState`/`PlayerState` を返す。勝敗判定もここで返す。
- 戦闘を独立した `BattleState`（ステート）としてステートマシンに組み込み、`BattleEnded` で次状態へ遷移。

### C. ショップループを「ショップステート」へ分解

- `EventManager` 内の `while(true)`（`EventManager.cs:91-101`）を解体。
- `ShopSystem.ProcessShopAction()` は **既に1アクション完結**なので流用可能。ループ継続/終了の判定（Exit受信まで繰り返す）を **API側（クライアントの再リクエスト）に委譲**する。
- ショップ後の休憩（`SelectRestAction`）も同様に1リクエスト＝1アクション化。

### D. エンカウント種別の決定

- 現状 `EventManager.TriggerRandomEvent()` が戦闘/ショップをランダムに内部決定。
- API化では、**「次のエンカウントを開始する」リクエストで種別を決定し、その種別を状態として保持**する（クライアントは結果を見て、戦闘なら戦闘APIを叩く）。

## 「現在何を入力すべきか」を返す

各ステップのレスポンスに、**クライアントが次に取りうる行動**を含める（既存DTOで概ね表現可能）:

- 戦闘中: `BattleState.AvailableStrategies`（攻撃戦略の選択肢）。
- ショップ中: `ShopState.AvailableItems` / `AvailableWeapons`。
- 探索/続行確認: `GamePhase` + 取りうるコントロール（続行/セーブ/終了）。

これにより、APIは「状態 + 選択肢」を返し、クライアントが選んだ行動を次リクエストで送る、というリクエスト/レスポンスのサイクルが成立する。

## 影響範囲

- `GameStateMachine` / `IGameState` / 各State / `GameFlowContext`（現在Stateの保持・公開を追加）。
- `BattleManager`（ループ分解、`ConsoleRenderer` 直呼び出しの除去）。
- `EventManager`（ショップ/エンカウントのループ除去）。
- 既存コンソール版は、これらステップAPIを**コンソール側のループから順に呼ぶアダプタ**として再実装すれば挙動を維持できる。
