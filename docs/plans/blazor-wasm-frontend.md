# Blazor WebAssembly フロントエンド実装プラン

既存の `GameEngine.Api`（ステップ駆動 REST API）を消費するブラウザ向け画面を、新規 **Blazor WebAssembly** プロジェクトとして追加する計画。コード例には踏み込まず、概要・参照ファイル・考慮事項を中心にまとめる。

## 背景・目的

- **現状**: ゲームは API（[GameEngine.Api](./../../GameEngine.Api/CLAUDE.md)）と CLI（`GameEngine.Console`）から駆動できるが、ブラウザ UI が無い。
- **ゴール**: API を叩いて 1 ステップずつ進める Web 画面を提供する。プレイヤーは新規開始 → 戦闘/ショップ/休憩/続行 → セーブ/ロードまでブラウザ上で完結できる。
- **技術選定の結論**: **Blazor WebAssembly**。理由は ①UI も C# で書け既存 DTO を共有でき契約ずれが無い ②API と同一オリジン配信で CORS 不要 ③静的配信のため将来の Web 公開でスケールしやすい ④.NET 一本で学習価値が高い。詳細な比較は本プランの前提（別途検討済み）。

## 全体像

- **新規プロジェクト 2 つ**: `GameEngine.Contracts`（共有契約のクラスライブラリ）と `GameEngine.Web`（`Microsoft.NET.Sdk.BlazorWebAssembly`, net8.0）をソリューションに追加。
- **参照方向**: `GameEngine.Api` → `GameEngine.Contracts` → `GameEngine`／`GameEngine.Web` → `GameEngine.Contracts`（Web は API に依存しない）。
- **配信方式**: ASP.NET Core ホスト型（Blazor WASM の発行物を `GameEngine.Api` から静的配信し、SPA フォールバックを設定）。→ 同一オリジンのため CORS 不要、デプロイ単位は 1 つ。
- **データフロー**: 画面 → `HttpClient` で `/api/...` → 返却 `GameStateResponse` を状態として保持・描画。クライアントが持つ永続状態は実質 `sessionId` のみ。

```
ブラウザ(Blazor WASM) --HTTP /api--> GameEngine.Api --Step--> GameSystem
        ↑ GameStateResponse(JSON) を描画／sessionId を localStorage 保持
```

## 画面構成（ExpectedInput 駆動）

レスポンスの `ExpectedInput`（[StepContracts.cs](./../../GameEngine/DTOs/StepContracts.cs)）で表示・操作を出し分ける。API 仕様は [GameplayController.cs](./../../GameEngine.Api/Controllers/GameplayController.cs) に対応。

| ExpectedInput | 画面 | 叩くエンドポイント | 送る内容 |
|---|---|---|---|
| `Attack` | 戦闘 | `POST /sessions/{id}/battle/turn` | `AttackAction`（戦略名） |
| `Shop` | ショップ | `POST /sessions/{id}/shop/action` | `ShopAction`（`Exit` まで繰り返す） |
| `Rest` | 休憩 | `POST /sessions/{id}/rest` | `UseItemAction?`（null=スキップ） |
| `GameAction` | エンカウント後の選択 | `POST /sessions/{id}/continue` | `GameActionChoice` |
| `None` | 終了/ゲームオーバー | （なし） | 新規開始 or ロードへ |

- **共通レイアウト**: プレイヤーパネル（HP/Lv/EXP/Gold/Potions/装備/AP/DP）、敵パネル（戦闘時）、メッセージログ、フェーズ別アクション。
- **トップ/メニュー**: 新規開始（名前入力）、ロード（プレイヤー名＋スロット）。
- **戦闘画面**: `BattleState.AvailableStrategies` をボタン化（`AttackStrategyNames` と一致）。敵 HP バー。
- **ショップ画面**: `ShopState`（`AvailableWeapons`/`PotionPrice`）から購入 UI、`Exit` ボタン。
- **休憩画面**: ポーション使用 or スキップ。

## 想定プロジェクト/コンポーネント構成（雛形）

```
GameEngine.Web/                     # 新規 Blazor WASM
├── Program.cs                      # HttpClient(BaseAddress=API), JSON オプション登録
├── Services/GameApiClient.cs       # API ラッパー（後述の各エンドポイント呼び出し）
├── State/SessionStore.cs           # sessionId 保持(localStorage)＋最新 GameStateResponse
├── Pages/                          # Home / Battle / Shop / Rest / PostEncounter / GameOver
├── Components/                     # PlayerPanel / EnemyPanel / MessageLog / HpBar
└── wwwroot/                        # index.html, CSS
```

## 契約（DTO）共有の設計判断 ★最重要

**結論: 共有プロジェクト `GameEngine.Contracts`（純粋クラスライブラリ）を新設し、API と Web の双方が参照する。** 二重定義のずれを根絶し、契約を単一の真実とする。

- 画面が必要とする型のうち、**内側の DTO は `GameEngine` に在る**（[GameState.cs](./../../GameEngine/DTOs/GameState.cs) の `PlayerState`/`EnemyState`/`BattleState`/`ShopState`/`GamePhase`、[PlayerAction.cs](./../../GameEngine/DTOs/PlayerAction.cs) の `AttackAction`/`ShopAction`/`ShopActionType`/`UseItemAction`、[GameActionChoice.cs](./../../GameEngine/DTOs/GameActionChoice.cs)、`ExpectedInput`/`GameMessage`）。
- 一方 **`GameStateResponse` とリクエスト DTO は `GameEngine.Api.Contracts` に在る**（[GameStateResponse.cs](./../../GameEngine.Api/Contracts/GameStateResponse.cs)、[Requests.cs](./../../GameEngine.Api/Contracts/Requests.cs)）。WASM から `GameEngine.Api` を参照すると ASP.NET Core 依存ごと取り込んでしまうため不可。
- **採用方針（`GameEngine.Contracts`）**:
  - 新規プロジェクト `GameEngine.Contracts`（`Microsoft.NET.Sdk`, net8.0, ASP.NET 非依存）を `.sln` に追加。
  - `GameStateResponse`／リクエスト DTO（`CreateSessionRequest`/`SaveRequest`/`LoadRequest`/`ContinueRequest`）を `GameEngine.Api.Contracts` から**移設**（namespace 変更に伴い API 側の `using` を追従）。
  - 内側 DTO（`PlayerState` 等）は `GameEngine` に置いたまま。`GameEngine.Contracts` が `GameEngine` を `ProjectReference` してそれらを再公開する（`GameStateResponse` が内側 DTO を含むため依存は必然）。Web は `GameEngine.Contracts` を参照すれば内側 DTO まで芋づるで取得できる。
  - 参照方向: `GameEngine.Api` → `GameEngine.Contracts` → `GameEngine`、`GameEngine.Web` → `GameEngine.Contracts`。`GameEngine.Web` は `GameEngine.Api` に依存しない（ASP.NET を巻き込まない）。
  - **留意**: `GameEngine.Contracts` が `GameEngine` 全体を参照すると、WASM へエンジン実装まで配布されうる。気になる場合は将来、純粋 DTO 群を `GameEngine` から `GameEngine.Contracts` 側へ寄せて依存を逆転させる余地を残す（本プランでは現状の依存方向で進める）。

## API クライアント層（GameApiClient）

[SessionsController.cs](./../../GameEngine.Api/Controllers/SessionsController.cs) / [GameplayController.cs](./../../GameEngine.Api/Controllers/GameplayController.cs) / [PlayersController.cs](./../../GameEngine.Api/Controllers/PlayersController.cs) に 1:1 で対応するメソッドを用意:

- セッション: `CreateNew(name?)` / `Get(id)` / `Delete(id)` / `Save(id, slot?)` / `Load(name, slot?)`
- 進行: `BattleTurn(id, strategy)` / `ShopAction(id, ...)` / `Rest(id, item?)` / `Continue(id, choice)`
- セーブ: `GetSaves(name)` / `DeleteSave(name, slot)`

## 配信・起動（API 側の小改修）

- [GameEngine.Api/Program.cs](./../../GameEngine.Api/Program.cs) にホスト型 WASM 配信を追加（`UseBlazorFrameworkFiles` / `UseStaticFiles` / `MapFallbackToFile("index.html")`）。
- `GameEngine.Api.csproj` に `GameEngine.Web` への `ProjectReference`（発行時に WASM を同梱）。
- 既定ポートは `http://localhost:5080`（[launchSettings.json](./../../GameEngine.Api/Properties/launchSettings.json)）。Swagger（`/swagger`）は併存。
- **dev で別オリジン運用する場合のみ** CORS 追加 or Vite 風プロキシが必要（ホスト型配信なら不要）。

## 作業フェーズ

### フェーズ W0: 契約プロジェクト切り出し（リスク中・先行必須）
- `GameEngine.Contracts` を新設し、`GameStateResponse`／リクエスト DTO を `GameEngine.Api.Contracts` から移設（上記「契約共有の設計判断」）。
- `GameEngine.Api` の参照・`using` を追従し、ビルド＆既存テスト（`WebApplicationFactory`）がグリーンであることを確認。レスポンス契約（JSON 形）は不変に保つ。

### フェーズ W1: 雛形と疎通（リスク低）
- `GameEngine.Web`（Blazor WASM）を追加し `.sln` に登録。`GameEngine.Contracts` を `ProjectReference`。`HttpClient`（BaseAddress=API）＋ **`JsonStringEnumConverter` を登録**（enum を文字列で授受する API 仕様に合わせる）。
- `GET /sessions/{id}` 相当の疎通確認（ダミー or 新規開始）。ホスト型配信を [Program.cs](./../../GameEngine.Api/Program.cs) に設定。

### フェーズ W2: コアループ（リスク中）
- `GameApiClient` と `SessionStore`（`sessionId` の localStorage 保持）を実装。
- `ExpectedInput` 駆動のルーティングと、戦闘/ショップ/休憩/続行の各画面。メッセージログの累積表示。

### フェーズ W3: セーブ/ロード・エラー処理（リスク中）
- セーブ/ロード画面、セーブ一覧/削除。リポジトリ未設定（503）時はセーブ UI を無効化。
- HTTP ステータス別のハンドリング（下記「考慮事項」）。

### フェーズ W4: 仕上げ（リスク低）
- スタイル（CSS、HP バー）、リロード復帰、ゲームオーバー/終了導線、簡易 E2E 確認。

## 考慮事項

- **enum は文字列**: API は `JsonStringEnumConverter` 使用（[Program.cs](./../../GameEngine.Api/Program.cs)）。Web の `HttpClient` JSON オプションでも同コンバータを登録しないと `ShopActionType`/`GameActionChoice`/`ExpectedInput` の送受で破綻する。
- **メッセージは差分返却**: `GameStateResponse.Messages` は前ステップ以降の蓄積分のみ（[ApiResponseMapper.cs](./../../GameEngine.Api/Mappers/ApiResponseMapper.cs) が `DrainMessages` を呼ぶ破壊的取得）。**累積ログはクライアント側で保持**する。
- **探索は自動前進**: セッション開始や `continue`（Continue）後はサーバが即エンカウントへ進むため、専用の探索画面は不要。返ってきた `ExpectedInput` をそのまま見る。
- **ショップは繰り返し**: `Exit` を送るまで `ExpectedInput=Shop` が続く。`Exit` の数量は API 側で 1 に正規化されるため UI は気にしなくてよい。
- **入力バリデーション**: 戦略名/数量は [PlayerActionValidator](./../../GameEngine/DTOs/PlayerAction.cs) で 400 になり得る。UI は選択肢を `BattleState.AvailableStrategies`／`ShopState` から作り、不正入力を出さない。
- **HTTP ステータス**: 404=セッション失効（→新規開始へ誘導）、409=`ExpectedInput` 不一致（最新状態を再取得して整合）、400=バリデーション失敗、503=セーブ未設定/Mongo 到達不可（セーブ機能を隠す）。
- **リロード復帰**: `sessionId` を localStorage 保持し、起動時に `GET /sessions/{id}`。404 ならクリアして新規へ。
- **同時操作の直列化**: サーバは 1 セッションをロックで直列化（[ApiGameSession](./../../GameEngine.Api/Hosting)）。UI 側も送信中はボタンを無効化し二重送信を防ぐ。
- **セーブとセッションは別物**: 確定セーブ（`IPlayerRepository`）は進行中の揮発状態（セッション）と責務が異なる。ロードは「ステータス復元＋探索から再開」である点を UI 表現に反映。
- **Random/決定性**: 戦闘・抽選の乱数はサーバ側。UI は結果（`GameStateResponse`）を描画するだけ。

## 影響範囲

- **新規**: `GameEngine.Contracts`（共有契約）、`GameEngine.Web`（Blazor WASM）。
- **API**: [Program.cs](./../../GameEngine.Api/Program.cs)（静的配信/フォールバック）、`.csproj`（`GameEngine.Contracts` 参照）、契約 DTO の移設に伴う `using` 追従。
- **エンジン/コンソール**: 影響なし（DTO 共有のみ）。
- **テスト**: API は既存統合テスト（`WebApplicationFactory`）が契約を守る。Web は bUnit でコンポーネント、必要なら手動 E2E。

## 完了条件

- ブラウザで新規開始 → 戦闘/ショップ/休憩/続行 → ゲームオーバー/終了まで一連で操作できる。
- リロードしても `sessionId` から進行が復帰する（失効時は新規へ誘導）。
- MongoDB 稼働時はセーブ/ロード/一覧/削除が動作し、未稼働時はセーブ UI が安全に無効化される。
- API と同一オリジン配信で CORS 設定なしに動作する。

## 参照ファイル一覧

- API 概要/エンドポイント: [GameEngine.Api/CLAUDE.md](./../../GameEngine.Api/CLAUDE.md)
- 契約: [GameStateResponse.cs](./../../GameEngine.Api/Contracts/GameStateResponse.cs) / [Requests.cs](./../../GameEngine.Api/Contracts/Requests.cs)
- コントローラ: [SessionsController.cs](./../../GameEngine.Api/Controllers/SessionsController.cs) / [GameplayController.cs](./../../GameEngine.Api/Controllers/GameplayController.cs) / [PlayersController.cs](./../../GameEngine.Api/Controllers/PlayersController.cs)
- DTO/アクション: [GameState.cs](./../../GameEngine/DTOs/GameState.cs) / [PlayerAction.cs](./../../GameEngine/DTOs/PlayerAction.cs) / [GameActionChoice.cs](./../../GameEngine/DTOs/GameActionChoice.cs) / [StepContracts.cs](./../../GameEngine/DTOs/StepContracts.cs)
- 写像: [ApiResponseMapper.cs](./../../GameEngine.Api/Mappers/ApiResponseMapper.cs)
- 合成起点/起動: [Program.cs](./../../GameEngine.Api/Program.cs) / [launchSettings.json](./../../GameEngine.Api/Properties/launchSettings.json)
- 戦略名: [AttackStrategyNames.cs](./../../GameEngine/Constants/AttackStrategyNames.cs)
