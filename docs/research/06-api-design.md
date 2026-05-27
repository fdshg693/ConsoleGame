# 06. API設計案

[03](./03-control-flow.md) のステップ駆動と [04](./04-session-and-persistence.md) のセッションを前提とした、エンドポイント設計の叩き台。実装ではなく方向性の提示。

## 基本方針

- **リクエスト/レスポンスのサイクル = 1ゲームステップ**。各レスポンスは「更新後の `GameState`（状態 + 取りうる選択肢 + 発生メッセージ）」を返す。
- **セッション識別子**でゲーム進行を分離（`sessionId`。ヘッダ or パス）。
- レスポンスは既存 `GameState` DTO を基本にする（[02](./02-io-layer.md)）。
- リクエストボディは既存 `PlayerAction` 派生（JSONポリモーフィズム）を利用。

## エンドポイント案

### セッション / プレイヤー

| メソッド | パス | 用途 | Req | Res |
|---|---|---|---|---|
| POST | `/api/sessions` | 新規ゲーム開始（名前指定） | `{ playerName }` | `GameState`（+ sessionId） |
| GET | `/api/sessions/{id}` | 現在状態の取得 | - | `GameState` |
| POST | `/api/sessions/{id}/save` | 確定セーブ | `{ slotName? }` | `{ ok }` |
| POST | `/api/sessions/{id}/load` | ロードして再開 | `{ playerName, slotName? }` | `GameState` |
| GET | `/api/players/{name}/saves` | セーブ一覧 | - | `PlayerSaveData[]` |
| DELETE | `/api/players/{name}/saves/{slot}` | セーブ削除 | - | `{ ok }` |

### 進行（探索 → エンカウント）

| メソッド | パス | 用途 | Req | Res |
|---|---|---|---|---|
| POST | `/api/sessions/{id}/encounter` | 次のエンカウント開始（戦闘/ショップを決定） | - | `GameState`（`Phase`=Battle/Shop） |
| POST | `/api/sessions/{id}/continue` | エンカウント後の続行確認 | `{ action: continue\|quit }` | `GameState` |

### 戦闘

| メソッド | パス | 用途 | Req | Res |
|---|---|---|---|---|
| POST | `/api/sessions/{id}/battle/turn` | 1ターン進める | `AttackAction`（`{ type:"Attack", strategyName }`） | `GameState`（更新後 `BattleState`/`EnemyState`） |

- 戦闘開始は `encounter` で `Phase=Battle` になった時点。`AvailableStrategies` を選択肢として返す。
- `BattleState.BattleEnded` / `PlayerWon` で決着を表現。決着後は `Phase` が PostEncounter/GameOver へ。

### ショップ / 休憩

| メソッド | パス | 用途 | Req | Res |
|---|---|---|---|---|
| POST | `/api/sessions/{id}/shop/action` | 購入/退出を1アクション | `ShopAction`（`{ shopType, itemName?, quantity }`） | `GameState`（更新後 `ShopState`） |
| POST | `/api/sessions/{id}/rest` | 休憩でアイテム使用 | `UseItemAction?`（`{ itemName, quantity }`） | `GameState` |

- ショップは「Exit を送るまでクライアントが繰り返し叩く」＝ 旧 `while(true)` をクライアント主導に置換。

## リクエスト/レスポンス例（戦闘1ターン）

```http
POST /api/sessions/ab12/battle/turn
Content-Type: application/json

{ "type": "Attack", "strategyName": "Melee" }
```

```jsonc
// 200 OK
{
  "player":  { "name": "Hero", "hp": 72, "maxHP": 100, "attackPower": 18, "defensePower": 5, "isAlive": true },
  "currentEnemy": { "name": "Goblin", "hp": 12, "maxHP": 40, "isAlive": true },
  "currentBattle": {
    "turnNumber": 3, "availableStrategies": ["Default","Melee","Magic"],
    "lastPlayerAction": "Melee", "lastDamageDealt": 14, "lastDamageTaken": 6,
    "battleEnded": false, "playerWon": false
  },
  "messages": [ { "text": "You hit Goblin for 14!", "type": "Combat" } ],
  "phase": "Battle",
  "isGameOver": false
}
```

## 横断的関心事

- **エラー/バリデーション**: 不正な行動（所持金不足・戦略名不正）は既存 `PlayerActionValidator` を流用し、HTTP 400 + メッセージで返す。
- **冪等性/順序**: 同一ステップの二重送信対策に、ターン番号やステップトークンで整合性検証を検討。
- **タイムアウト**: 放置セッションはTTLで失効（[04](./04-session-and-persistence.md)）。
- **ドキュメント**: Swagger/OpenAPI（`Swashbuckle`）でスキーマ公開。
- **認証**: 初期は不要でも、`playerName` の所有者確認など将来の認証導入余地を残す。
