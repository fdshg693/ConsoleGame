# 04. セッション管理と永続化

HTTPはステートレスだが、ゲームはステートフル。**「リクエスト間でゲーム進行状態をどこに保持・復元するか」** が設計の要。

## 現状の永続化

`GameEngine/Interfaces/IPlayerRepository.cs`（すべて async）:

```csharp
Task<bool> SaveAsync(IPlayer player, string saveSlotName = "auto_save");
Task<PlayerSaveData?> LoadAsync(string playerName, string saveSlotName = "auto_save");
Task<List<PlayerSaveData>> GetSaveListAsync(string playerName);
Task<bool> DeleteAsync(string playerName, string saveSlotName);
Task<bool> TestConnectionAsync();
```

- 実装は `MongoPlayerRepository`（本番）/ `InMemoryPlayerRepository`（テスト）。
- 保存単位は `PlayerSaveData`（`DTOs/PlayerSaveData.cs`）。

### PlayerSaveData が保持する範囲

- 保持: `PlayerName` / `CurrentHP` / `MaxHP` / `BaseAP` / `BaseDP` / `TotalGold` / `TotalPotions` / `Level` / `TotalExperience` / `EquippedWeapon` / `AttackStrategy` / `SavedAt` / `SaveSlotName`。
- **欠落**: 戦闘中の敵状態（敵HP・ターン数・次行動）、現在のフェーズ（Start/Encounter/Battle/Shop）、エンカウント進行。
- → **プレイヤーステータスのスナップショットとしては完全**だが、**戦闘中などセッション途中での中断・再開はできない**。

## 課題

- API では「戦闘の1ターンを進める」たびにHTTPリクエストが切れる。**敵HP・ターン数を含む途中状態を、次リクエストまで保持**しなければならない。
- 同時に複数プレイヤー（複数セッション）が走るため、**セッションを識別子（sessionId / playerId）で分離**する必要がある。
- 現状の静的 `GameRecord`（勝敗カウント）や静的 `GameMessageBus` は **プロセス共有のため並行リクエストで競合**する。インスタンス/スコープ化が必要。

## 変更方針

### 1. セッション状態モデルの定義

進行中ゲームの完全な状態を表す **`GameSessionState`（新規）** を定義する。最低限:

- `SessionId` / `PlayerName`
- `Phase`（`GamePhase`）と現在のState名
- `PlayerSaveData` 相当（プレイヤーステータス）
- 戦闘中なら `EnemyState`（敵の現在HP含む）+ `BattleState`（ターン数等）
- ショップ中なら `ShopState`（品揃え・価格）

`PlayerSaveData` を拡張するのではなく、**進行状態は別モデル（セッション）として分離**するのが整理しやすい（セーブ＝確定スナップショット、セッション＝進行中の揮発状態）。

### 2. セッションストアの選択

| 方式 | 概要 | 向き |
|---|---|---|
| **インメモリキャッシュ**（`IMemoryCache`） | サーバメモリにセッション保持。TTLで失効 | 単一インスタンス・PoC向き。実装が軽い |
| **分散キャッシュ**（Redis） | 外部キャッシュに保持 | 複数インスタンス・スケール向き |
| **DB（Mongo）にセッションも保存** | 既存Mongoに `GameSessions` コレクション追加 | 永続再開を重視。やや重い |

推奨: **まずインメモリ + TTL** で成立させ、スケール要件が出たらRedis/DBへ移行。確定セーブは既存 `IPlayerRepository`（Mongo）を継続利用。

### 3. リポジトリ/状態の責務分離

- `IPlayerRepository`: **確定セーブ/ロード**（現状維持、APIのセーブ機能で利用）。
- `ISessionRepository`（新規・任意）: **進行中セッションの保存/復元**（戦闘途中含む）。
- セッション復元時は `PlayerSaveData` → `Player` 復元ロジックが必要（現状 `LoadAsync` は `PlayerSaveData` を返すのみで、`Player` への再構築経路を要確認・追加）。

### 4. グローバル状態の排除

- 静的 `GameMessageBus`・静的 `GameRecord`・シングルトン `GameConfigLoader` は **DIスコープ/シングルトンサービス化**して並行リクエストの混線を防ぐ（[05](./05-project-and-di.md)）。
- `GameRecord`（勝敗）はプレイヤー単位の永続化を検討（現状はプロセス共有メモリ）。

## まとめ（保存粒度の判断）

- **プレイヤーステータスの保存粒度は現状で十分**（`PlayerSaveData` がカバー）。
- **不足は「セッション中の揮発状態（敵HP・ターン・フェーズ）」**。これを保持するセッション層を新設するのが、API化での永続化面の主変更点。
