# 07. 移行ロードマップ

**既存のコンソール実行を壊さず**、段階的にAPI対応へ移行する手順。各フェーズは独立してビルド・テスト可能な区切りとする。

## フェーズ0: 足場づくり（リスク低）

- `GameEngine` を **Library 化**し、`GameEngine.Console`（Exe）を分離（[05](./05-project-and-di.md)）。
- `GameEngine.Tests` の TFM を **net8.0 に統一**。
- `AddGameEngine(IServiceCollection)` 拡張を用意し、コンソール側も DI 経由の合成に置き換え（挙動不変を回帰テストで担保）。
- **完了条件**: 既存ゲームがコンソールで従来どおり動作。

## フェーズ1: I/O抽象化（リスク中）

- `ConsoleRenderer` → `IRenderer` 抽象化（静的→インスタンス化）。コンソール実装を `GameEngine.Console` へ。
- `GameMessageBus` を **インスタンス/スコープ化**し、出力シンクをDI注入（`GameSystem.cs:25` の固定購読を解消）。
- コンソール固有UI（矢印キー・ANSI・整数入力）を `GameEngine.Console` に隔離。
- **完了条件**: コアから `Console.*` 直接依存が消え、コンソール動作は維持。

## フェーズ2: 制御フローのステップ駆動化（リスク高・最重要）

- `GameStateMachine` の `while` を `Step()`/`Advance()` に分解し、現在Stateを保持・公開（[03](./03-control-flow.md)）。
- `BattleManager.ExecuteBattle()` を `StartBattle()` + `SubmitPlayerTurn()` に分解。
- `EventManager` のショップ `while(true)` とエンカウント決定を解体し、1アクション単位に。
- コンソール版は、これらステップAPIを順に呼ぶアダプタとして再実装（挙動維持）。
- **完了条件**: ゲーム全体が「1行動 → 1ステップ → 状態返却」で外部駆動可能。コンソールもその上で動く。

## フェーズ3: セッション層（リスク中）

- `GameSessionState`（進行中の完全状態）と `ISessionRepository`（まずインメモリ + TTL）を新設（[04](./04-session-and-persistence.md)）。
- 静的 `GameRecord` のサービス化。
- セーブ/ロードから `Player` を復元する経路を整備。
- **完了条件**: 戦闘途中を含むゲーム状態を保存・復元できる。

## フェーズ4: API プロジェクト（リスク中）

- `GameEngine.Api`（ASP.NET Core）を新設し、`GameEngine` を参照（[05](./05-project-and-di.md)）。
- API用 `IGameInput`/`IRenderer` 実装（待たずにDTOを返す/バッファに蓄積）。
- コントローラを [06](./06-api-design.md) のエンドポイントで実装。Swagger 公開。
- YAML設定をAPIホスト出力先に配置。
- **完了条件**: APIからゲーム開始〜戦闘〜ショップ〜セーブ/ロードが一通り可能。

## フェーズ5: 仕上げ（任意）

- DTO補強（`BattleState` ターンログ、`ShopState.PlayerGold`）。
- セッションストアを Redis/DB へ拡張（スケール要件次第）。
- 認証・レート制限・冪等性トークンなどの横断対応。
- API統合テスト（`WebApplicationFactory` + xUnit）。

## 優先度サマリ

| 優先 | 項目 | 理由 |
|---|---|---|
| 最高 | フェーズ2（制御フロー分解） | API化の本質的ブロッカー。これ無しではHTTPに乗らない |
| 高 | フェーズ0・1（Lib化・I/O抽象） | 以降すべての前提。コンソール維持の土台 |
| 高 | フェーズ3（セッション層） | ステートフルなゲームをHTTPで成立させる鍵 |
| 中 | フェーズ4（API実装） | 上記が揃えば比較的素直に実装可能 |
| 低 | フェーズ5（仕上げ） | 体験・運用の改善。後追い可能 |

## リスクと留意点

- **フェーズ2が最大の難所**。戦闘・ショップのループ分解は既存ロジックの正しさを保つ回帰テストが必須。
- 並行リクエスト対応のため、**静的状態（MessageBus / GameRecord / ConfigLoader）の除去**を早めに済ませる。
- コンソールとAPIで**コアを共有**し続けることで、二重メンテを避ける（UIアダプタのみ差し替え）。
