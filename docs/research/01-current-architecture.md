# 01. 現状アーキテクチャと API化を阻む結合点

## 現在の実行フロー

```
Program.Main()                       … Composition Root（手動new）
 └─ GameConfigLoader.Instance         … 設定をシングルトンで取得
 └─ Console.ReadLine()                … プレイヤー名入力（ブロッキング）
 └─ new ConsoleGameInput(...)         … IGameInput のコンソール実装をハードコード
 └─ new GameSystem(...).RunGameLoop() … 戻らない同期ループ
      └─ GameStateMachine.Run()       … while ループで状態遷移を完全制御
           ├─ StartState
           ├─ EncounterState          … EventManager 経由で戦闘/ショップ（内部ループ + 入力待ち）
           ├─ PostEncounterState      … 続行確認（入力待ち）
           └─ GameOverState
```

`GameEngine/Program.cs:13-61` がエントリポイント。`RunGameLoop()`（`GameEngine/Program.cs:49`）が終了するまで制御が戻らない。

## レイヤと API化の難易度

| レイヤ | 主なファイル | 結合度 | API化難度 |
|---|---|---|---|
| DTO（状態・コマンド） | `DTOs/GameState.cs`, `DTOs/PlayerAction.cs` | 低 | 低（ほぼ流用可） |
| メッセージバス | `Models/GameMessageBus.cs` | 低 | 低（購読先を差し替え） |
| 入力抽象 | `Interfaces/IGameInput.cs` | 中 | 中（同期ブロッキングが課題） |
| 永続化 | `Interfaces/IPlayerRepository.cs` | 低 | 中（保存粒度が不足） |
| 制御フロー | `Systems/StateMachine/*`, `BattleManager`, `EventManager` | 高 | 高（内部whileループ） |
| 出力 | `Systems/ConsoleRenderer.cs`, `Systems/UserInteraction.cs` | 高 | 高（Console/ANSI直結合） |
| プロジェクト構成 | `GameEngine.csproj` | - | 中（Exe→Lib化が必要） |

## API化を阻む4つの根本要因

1. **同期ブロッキングな制御フロー**
   - メインループ（`GameStateMachine.cs:23-33`）、戦闘ループ（`BattleManager.cs:53-83`）、ショップループ（`EventManager.cs:91-101`）が内部 `while` で完結。
   - HTTPは「1リクエスト = 1ステップ」なので、ループを外部から駆動できる形に分解が必要。→ [03](./03-control-flow.md)

2. **入力が `Console.ReadKey/ReadLine` でブロック**
   - `ConsoleRenderer.SelectFromMenu()`（`ConsoleRenderer.cs:268`）の `Console.ReadKey` ほか。
   - APIではリクエストボディで行動を受け取る方式に置き換える。→ [02](./02-io-layer.md)

3. **出力が `Console.WriteLine` / ANSI に直結合**
   - `ConsoleRenderer`（静的）と `GameMessageBus` 購読（`GameSystem.cs:25`）がコンソール前提。
   - 出力を抽象化し、API実装ではレスポンスDTOへ蓄積する。→ [02](./02-io-layer.md)

4. **状態がプロセス内メモリに閉じる / 保存粒度不足**
   - 進行状態はメモリ上の `Player` と「現在のState」に存在。`PlayerSaveData` は戦闘中の敵HPやターンを保持しない。
   - HTTPのステートレス境界をまたぐためのセッション管理が必要。→ [04](./04-session-and-persistence.md)

## 既に整備されていてプラスに働く点

- **インターフェースが豊富**: `IGameInput`, `IPlayerRepository`, `IEnemyFactory` など差し替え用の継ぎ目（seam）が既存。
- **DTOとMapperが整備済み**: `GameStateMapper` が `IPlayer` → `PlayerState` 等を生成。API レスポンスにそのまま使える。
- **Composition Root が一箇所に集約**: 依存生成が `Program.cs` に閉じており、DIコンテナへの置き換えが局所的。
- **永続化が `IPlayerRepository` で抽象化済み**かつ `Task` ベース（async）で ASP.NET Core と親和的。
