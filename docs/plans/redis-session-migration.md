# Redis セッション移行プラン

API のセッション状態保持を「サーバ常駐のライブ `GameSystem`」から「**Redis に保存したスナップショットを毎リクエスト再水和**」する方式へ移行する計画。

## 背景・目的

- **現状**: `GameEngine.Api` はセッションごとにライブ `GameSystem` の object graph を `GameSessionManager` の `ConcurrentDictionary` に常駐させる（[GameEngine.Api/Hosting/GameSessionManager.cs](./../../GameEngine.Api/Hosting/GameSessionManager.cs)）。
- **課題**:
  - 保存せず放置されたセッションが TTL（既定30分）まで全状態をメモリ保持 → **総セッション数に比例した無制限のメモリ増加**。
  - 上限がなく `POST /sessions` 連打で OOM 余地（DoS）。
  - 失効が遅延のみ（新規生成時に全件 `Sweep` = O(n)）。背景回収なし。
  - 単一プロセス前提でライブ状態を持つため**水平スケール不可**。
- **ゴール**: ライブ常駐を廃止し、状態は Redis のみに保持。メモリは**同時実行数に比例**（総セッション数に非依存）、放置は Redis のネイティブ TTL/エビクションで自動回収、API はステートレスで水平スケール可能。

## 現状とゴールの対比

| 観点 | 現状（ライブ常駐） | ゴール（Redis + 再水和） |
|---|---|---|
| 状態の保持場所 | プロセスメモリ（ライブ object graph） | Redis（`GameSessionState` の JSON） |
| メモリ使用 | 総セッション数に比例 | 同時実行数に比例 |
| 放置の回収 | 遅延 Sweep（自前 TTL） | Redis EXPIRE / maxmemory エビクション |
| スケール | 単一プロセス固定 | 水平スケール可（任意インスタンスが処理） |
| 障害復旧 | プロセス死 = 全進行喪失 | Redis 永続化次第で復旧可 |

## 最大の前提: ステートマシンの再水和（エンジン未対応）

フェーズ3で**保存側** `GameSystem.CaptureSession` と `GameSessionState`／`ISessionRepository` は実装済みだが、**復元側（再水和）が無い**。これが本計画の本丸であり、ここが通れば Redis 化は `ISessionRepository` 差し替えに縮小できる。

復元すべき揮発状態と現状の手当て:

| 対象 | 復元元 | 状況 |
|---|---|---|
| `GameStateMachine` の現在ステート | `GameSessionState.CurrentStateName` | **要新規**（ステート名→`IGameState` 復元、`Prepare` 再実行の要否を定義） |
| `EventManager`（`_currentType`/`_shopState`/`_battleResult`） | `Phase`/`Shop`/`Battle`/`Enemy` | **要新規**（`EventManager.Restore`） |
| `BattleManager`（`_enemy`/`_turn`） | `Enemy`/`Battle.TurnNumber` | **要新規**（`BattleManager.Restore`） |
| `Player` | `GameSessionState.Player`（`PlayerSaveData`） | 既存 `IPlayerFactory.Restore` で可 |
| 勝敗記録 | `TotalWins`/`TotalLosses` | 既存 `IGameRecord.Restore` で可 |

### 既知のギャップ: 敵のフルステータス欠落

- `Enemy`（[GameEngine/Models/Enemy.cs](./../../GameEngine/Models/Enemy.cs)）は `BaseAP`/`BaseDP`/`YieldExperience`/`YieldGold`/`BaseHP` を持つが、捕捉される `EnemyState` DTO は `Name`/`HP`/`MaxHP`/`IsAlive`/`AttackStrategy` のみ。**戦闘途中の敵を忠実に再構築できない**。
- **対策A（推奨）**: `EnemyFactory.Create(Name)` で `enemy-specs.yml` から完全再生成し、**現在 HP だけ上書き**する（`Enemy` に現在 HP 復元手段を追加、または `EnemyFactory` に restore 経路）。YAML を唯一の真実とし DTO を汚さない。前提: `EnemyState.Name` == spec キー（現状一致）。
- **対策B**: `GameSessionState`/`EnemyState` を拡張して敵フルステータスを保存。DTO は肥大するが factory 非依存。
- → **A を採用**。`Enemy` に `internal` な HP 復元手段を追加し、`EnemyFactory.Restore(EnemyState)` を新設。

## 作業フェーズ

### フェーズ R1: エンジンに再水和を実装（最重要・リスク高）

- `GameSystem.Restore(GameSessionState)` を新設:
  - `BuildTransitions()` を再利用し、`CurrentStateName` から現在ステートを設定（ステート名→`new XxxState()` の対応表）。
  - `EventManager`/`BattleManager` を捕捉値でプライムし、マシンを「実行せず」その入力待ち状態に置く（自動前進はしない。`Prepare` 呼び出しの要否を明記）。
  - 終端（`ExpectedInput=None` / `GameOver`）は再水和不要 → セッション削除で表現。
- `EventManager.Restore(GameEventType, ShopState?, BattleStepResult?)` と `BattleManager.Restore(IEnemy?, int turn)` を追加。
- 敵復元: `EnemyFactory.Restore(EnemyState)`（`Create(Name)` + 現在 HP 上書き）。`Enemy` に HP 復元手段。
- **テスト**: `CaptureSession → Restore` 往復で `ExpectedInput`/`Player`/`Enemy(HP)`/`Battle(turn)`/`Shop`/勝敗数が一致することを、戦闘途中・ショップ途中・休憩・続行確認の各フェーズで検証。

### フェーズ R2: ISessionRepository の Redis 実装（リスク中）

- `RedisSessionRepository : ISessionRepository`（`SaveAsync`/`LoadAsync`/`DeleteAsync`）を追加。
- シリアライズ: `System.Text.Json`（`GameSessionState`、`JsonStringEnumConverter`）。全て plain DTO のためポリモーフィズム不要。
- キー: `session:{id}`。TTL: 保存ごとに EXPIRE を再設定（スライディング）。
- パッケージ: `StackExchange.Redis`。`IConnectionMultiplexer` を Singleton 登録。接続文字列は `appsettings`／環境変数。
- **楽観的並行制御**: `GameSessionState` に `Version`（int）を追加。保存は Lua/`WATCH`+`MULTI` で「読み込んだ Version と一致時のみ上書き＋インクリメント」。不一致は競合 → API は **409**（in-process ロックが使えなくなる代替）。

### フェーズ R3: API ホストのステートレス化（リスク中）

- `GameSessionManager` を改修してライブ辞書を撤廃。各操作 = `ISessionRepository.LoadAsync` → `GameSystem.Restore` → `Step` → `CaptureSession` → `SaveAsync`。
- object graph は**1リクエストで構築・破棄**（retain しない）。メモリは同時実行数に比例。
- `EnemyFactory` を **Singleton 共有化**（YAML を1回だけロード）。`bus` は生成時引数に移す小改修（「メモリ削減」オプションをここで回収。R1 の敵復元設計と整合）。
- 新規開始／ロードも「新規 `CaptureSession` を保存して開始」に統一。
- `BufferingRenderer` は変更不要（ステップ中のメッセージ収集はそのまま）。
- 既存エンドポイント／レスポンス契約（`GameStateResponse`）は不変。

### フェーズ R4: 運用・段階移行（リスク低）

- 切替フラグ: `ISessionRepository` を **InMemory(dev) / Redis(prod)** で DI 差し替え。再水和パス（R1）は共通なので両実装で同一コードが動く。
- `docker-compose.yml` に `redis` サービスを追加。
- 既存 `InMemorySessionRepository` をステートレス経路の dev 実装として流用（ライブ辞書廃止後も TTL 付きで機能）。
- Redis 運用: `maxmemory` 上限 + `volatile-ttl`／`allkeys-lru` エビクションポリシー、メトリクス監視。

## 影響範囲

- **エンジン**: `GameSystem`(+`Restore`)、`EventManager`(+`Restore`)、`BattleManager`(+`Restore`)、`Enemy`(HP 復元)、`EnemyFactory`(共有化/`Restore`)、`GameSessionState`(+`Version`)。`CaptureSession` は概ね流用。
- **API**: `GameSessionManager` 改修、`RedisSessionRepository` 追加、`Program.cs` の DI、`docker-compose`。
- **コンソール**: 影響なし（共有コアにメソッド追加のみ、既存挙動は不変）。
- **テスト**: 再水和往復テスト、`RedisSessionRepository`（Testcontainers もしくはインメモリ代替）、API 統合テスト（`WebApplicationFactory`）。

## リスクと留意点

- **R1 が最大の難所**（[研究フェーズ2](./../research/07-migration-roadmap.md)同様、状態復元の正しさに回帰テスト必須）。
- 捕捉漏れの総点検（プレイヤーの現在戦略=`PlayerSaveData.AttackStrategy`、ポーション/ゴールド/装備=`PlayerSaveData`、敵=上記対策A、で網羅できるか）。
- 確定セーブ（`IPlayerRepository` への `SaveGame`）とセッション保存は**別物**。混同しない（前者はプレイヤー確定スナップショット、後者は進行中の揮発状態）。
- スナップショットのスキーマ後方互換 → `Version` で吸収、または TTL で自然消滅。
- 決定性: `EventManager` の `Random` は再水和後に新規生成で良い（途中状態に乱数依存はなく、抽選は次の `Explore` で発生）。

## 完了条件

- 戦闘途中を含む任意フェーズで「保存 → プロセス再起動/別インスタンス → 再水和 → 続行」が一致動作する。
- API にライブ常駐がゼロ（メモリが同時実行数に比例）。
- Redis TTL で放置セッションが自動失効し、上限超過時もエビクションで安全に動作する。

## 推奨実装順序

1. **R1（再水和 + 往復テスト）** ← 本丸。ここを最初に固める。
2. **R3 の先行検証**: ライブ辞書を廃止し `InMemorySessionRepository` でステートレス経路を検証（Redis なしで R1 の効果を確認）。
3. **R2（Redis 実装）**: `ISessionRepository` 差し替えに縮小される。
4. **R4（運用切替）**: docker-compose / エビクション / 監視。
