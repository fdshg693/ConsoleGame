# DTOs

UI連携用 DTO、コマンドパターン、永続化 DTO を定義。namespace: `GameEngine.DTOs`

## ファイル構成

### UI状態 DTO

- **GameState.cs** - UI層とコアロジック層のデータ交換用 DTO 群
  - `GameState` : ルート（`PlayerState`, `EnemyState`, `BattleState`, `ShopState`, `Messages`, `GamePhase`）
  - `PlayerState` / `EnemyState` : キャラクター状態
  - `BattleState` : ターン番号、利用可能な戦略、勝敗状態
  - `ShopState` / `ShopItem` / `WeaponInfo` : ショップ関連
  - `GamePhase` enum : Initialization / Exploration / Battle / Shop / Rest / GameOver
  - `GameMessage` / `MessageType` は `GameEngine.Models.GameMessageBus.cs` で定義（ドメイン層から直接参照されるため）

### コマンドパターン

- **PlayerAction.cs** - UI層 → コアロジック層へのコマンド群
  - `PlayerAction`（抽象基底）→ `AttackAction` / `UseItemAction` / `ShopAction` / `GameControlAction` / `RestAction`
  - `ActionType` enum / `ShopActionType` enum
  - `PlayerActionValidator` : バリデーション（戦略名の検証に `AttackStrategyNames` 定数を使用）

### ステップ駆動コントラクト

- **StepContracts.cs** - ステップ駆動エンジン（`GameSystem.Step`）の入出力契約
  - `ExpectedInput` enum : 次の `Step` で必要な入力種別（`None` / `Attack` / `Shop` / `Rest` / `GameAction`）。ホストはこれを見て対応する行動を供給する
  - `PlayerInput`（sealed）: 1ステップ分の入力キャリア。`ExpectedInput` に対応するフィールドのみ設定される
    - ファクトリ: `ForAttack(AttackAction)` / `ForShop(ShopAction)` / `ForRest(UseItemAction?)`（null=スキップ）/ `ForProgress(GameActionChoice)` / `None`（入力不要）
    - API ホストはリクエストボディから、コンソールホストは `IGameInput` の戻り値から組み立てて `GameSystem.Step` に渡す
- **GameActionChoice.cs** - エンカウント後の進行選択 enum（`Continue` / `SaveAndContinue` / `SaveAndQuit` / `Quit`）。UI 非依存

### 永続化 DTO

- **PlayerSaveData.cs** - プレイヤーデータ保存用 DTO（確定セーブ＝プレイヤーステータスの確定スナップショット）
  - MongoDB 固有の属性（BSON）は含まない。マッピングは `MongoPlayerRepository` 側の `BsonClassMap` で定義
  - `WeaponData` サブクラスで装備武器情報を保持

### セッション DTO

- **GameSessionState.cs** - 進行中ゲームの完全な揮発状態（セッション）スナップショット
  - `SessionId` / `PlayerName` / `Phase`（`GamePhase`）/ `CurrentStateName` / `ExpectedInput`
  - `Player`（`PlayerSaveData`）+ 戦闘中の `Enemy`（現在HP）/ `Battle`（ターン数）/ ショップ中の `Shop` / 勝敗数（`TotalWins`/`TotalLosses`）
  - 確定セーブ（`PlayerSaveData`）と分離: セーブ＝確定スナップショット、セッション＝戦闘途中を含む進行中の揮発状態
  - `GameSystem.CaptureSession` で生成し、`ISessionRepository` でリクエスト間に保持・復元する

## 設計上の注意点

- DTO は純粋なデータクラス（ロジック不要）
- `PlayerSaveData` は永続化専用。UI変換には使わない（`GameStateMapper` が `IPlayer` プロパティから直接変換）
