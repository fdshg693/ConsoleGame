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

### 永続化 DTO

- **PlayerSaveData.cs** - プレイヤーデータ保存用 DTO
  - MongoDB 固有の属性（BSON）は含まない。マッピングは `MongoPlayerRepository` 側の `BsonClassMap` で定義
  - `WeaponData` サブクラスで装備武器情報を保持

## 設計上の注意点

- DTO は純粋なデータクラス（ロジック不要）
- `PlayerSaveData` は永続化専用。UI変換には使わない（`GameStateMapper` が `IPlayer` プロパティから直接変換）
