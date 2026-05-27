# 02. 入出力（I/O）層の変更点

API化の本質は **「コンソールI/Oを、HTTPのリクエスト/レスポンスに置き換える」** こと。入力・出力・DTOの3観点で整理する。

## 入力: IGameInput

現状の定義（`GameEngine/Interfaces/IGameInput.cs`）:

```csharp
AttackAction SelectAttackAction(BattleState, PlayerState, EnemyState);
ShopAction   SelectShopAction(ShopState, PlayerState);
UseItemAction? SelectRestAction(PlayerState);
```

- すべて **同期メソッドで、戻り値を返すまでブロック**。`ConsoleGameInput` 実装は内部で `ConsoleRenderer.SelectFromMenu()` → `Console.ReadKey()` を呼ぶ。
- 戻り値の型（`AttackAction` 等の `PlayerAction` 派生）は **APIのリクエストボディにそのまま使える**。

### 変更方針

- **インターフェースは温存**し、新実装 `ApiGameInput`（仮）を追加する。
  - コンソール実装: キー入力を待つ（現状維持）。
  - API実装: 「クライアントから受信済みの行動」を即座に返す（待たない）。具体的には、コントローラが受け取った `PlayerAction` を保持し、`Select～Action()` がそれを返す。
- ただし内部ループ（戦闘・ショップ）が `Select～Action()` を繰り返し呼ぶ前提のままだと、API実装でも1リクエスト内で複数回呼ばれてしまう。→ ループ分解（[03](./03-control-flow.md)）と併せて、**「1リクエスト = 1行動」になるよう制御フロー側を変える**のが本筋。
- 代替案として `IGameInput` を `async`（`Task<AttackAction>`）化し、入力到着まで待つチャネル/`TaskCompletionSource` を挟む方式もあるが、ループ分解の方が状態復元・スケールで有利。

## 出力: ConsoleRenderer と GameMessageBus

### 現状

- `Systems/ConsoleRenderer.cs` は **静的クラス**で、`Console.WriteLine` とANSIエスケープ（色・カーソル制御）を直接呼ぶ。約40箇所から参照。
- `Models/GameMessageBus.cs` は静的イベント pub/sub。`GameSystem.cs:25` が唯一の購読者で、受信メッセージを `ConsoleRenderer` で描画する。
- 矢印キー選択UI（`SelectFromMenu`, `ConsoleRenderer.cs:243-334`）と整数入力・確認（`UserInteraction.cs`）もコンソール固有。

### 変更方針

- **`ConsoleRenderer` を `IRenderer` インターフェースへ抽象化**し、静的→インスタンス化。
  - `ConsoleRenderer`（実装A）: 現状のコンソール描画。
  - `BufferRenderer`（実装B）: 出力を `List<GameMessage>` 等に蓄積し、APIレスポンスへ詰める。
- **`GameMessageBus` の購読をDIで差し替え可能に**。`GameSystem.cs:25` の固定購読をやめ、注入された `IRenderer`/シンクへ流す。
  - 静的イベントはリクエスト並行時に購読が混線するため、**インスタンスベースのバス（またはスコープ単位の購読）へ移行**する。
- **選択UI（矢印キー/整数入力）はコンソール専用**として `IGameInput` のコンソール実装内に閉じ込め、API実装からは排除する（APIは選択肢DTOを返し、クライアントが選ぶ）。
- **ANSIエスケープは構造化**: 色などの装飾は `MessageType`（既存enum）でDTOに持たせ、表示側（コンソール or クライアント）で解釈する。API応答にはANSIを含めない。

## DTO: ほぼそのまま流用可能

`DTOs/GameState.cs` は既にAPI向けに十分なリッチさを持つ。

| DTO | 内容 | API用途 |
|---|---|---|
| `GameState` | Player/Enemy/Battle/Shop/Messages/Phase/IsGameOver | レスポンスのルート |
| `PlayerState` | HP/MaxHP/Level/Exp/Gold/Potions/装備/AP/DP | プレイヤー状態 |
| `EnemyState` | Name/HP/MaxHP/IsAlive/AttackStrategy | 敵状態 |
| `BattleState` | TurnNumber / **AvailableStrategies**（選択肢）/ Last各種 / BattleEnded | 戦闘状態 + 選べる行動 |
| `ShopState` | AvailableItems / AvailableWeapons / PotionPrice | ショップ品揃え |
| `GamePhase` enum | Initialization/Exploration/Battle/Shop/Rest/GameOver | クライアントの画面分岐 |
| `PlayerAction` 派生 | AttackAction / ShopAction / UseItemAction 等 | **リクエストボディ** |

### DTOで補強したい点

- **`PlayerAction` のJSONポリモーフィズム**: `ActionType` を判別子に `[JsonPolymorphic]` / `[JsonDerivedType]`（System.Text.Json）を付与し、デシリアライズを安定させる。
- **`BattleState` にターンログ**（任意）: 1ターンごとの結果履歴（与/被ダメ、HP推移）を `List<TurnLog>` で持たせるとクライアント表示が容易（現状は直近のみ）。
- **`ShopState` に `PlayerGold`**（任意）: 購入可否判定がクライアント側でできるよう所持金を含める（現状は `ConsoleGameInput` 内で計算）。

→ 制御フローの分解は [03](./03-control-flow.md)、セッション管理は [04](./04-session-and-persistence.md) を参照。
