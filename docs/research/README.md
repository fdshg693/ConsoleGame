# API層追加に向けた調査（research）

コンソールRPGエンジンに **HTTP API層を追加し、APIからもプレイ可能にする** ための、コンソールゲーム側の変更点をまとめた調査ドキュメント群。

## 結論サマリ

- **DTO層はAPIにほぼ流用可能**。`GameState` / `PlayerState` / `BattleState` / `ShopState` / `PlayerAction` 系はシリアライズ可能な純粋データで、リクエスト/レスポンスに転用できる（[02](./02-io-layer.md) 参照）。
- **最大の障壁は「同期ブロッキング制御フロー」**。メインループ・戦闘ループ・ショップループが内部 `while` で完結し、`Console.ReadKey/ReadLine` で入力を待つ。HTTPのリクエスト/レスポンス境界で1ステップずつ駆動できる構造に作り替える必要がある（[03](./03-control-flow.md) 参照）。
- **出力が `Console.WriteLine` / ANSI に密結合**。`ConsoleRenderer`（静的）と `GameMessageBus` をインターフェース化し、API実装ではバッファ/DTOへ蓄積する（[02](./02-io-layer.md) 参照）。
- **状態の保存粒度が不足**。`PlayerSaveData` はプレイヤーステータスのみで、戦闘中の敵HP・ターン・進行フェーズを保持できない。ステートフルなセッション管理の追加が必要（[04](./04-session-and-persistence.md) 参照）。
- **プロジェクトが `OutputType=Exe`**。API プロジェクトから参照するにはコアをライブラリ化し、DI（`Microsoft.Extensions.DependencyInjection`）を導入する（[05](./05-project-and-di.md) 参照）。

## ドキュメント構成

| # | ファイル | 内容 |
|---|---|---|
| 00 | [README.md](./README.md) | 本索引・結論サマリ |
| 01 | [01-current-architecture.md](./01-current-architecture.md) | 現状アーキテクチャと、API化を阻む結合点の全体像 |
| 02 | [02-io-layer.md](./02-io-layer.md) | 入出力（IGameInput / ConsoleRenderer / GameMessageBus / DTO）の変更点 |
| 03 | [03-control-flow.md](./03-control-flow.md) | 制御フロー（メインループ / ステートマシン / 戦闘・ショップループ）の非ブロッキング化 |
| 04 | [04-session-and-persistence.md](./04-session-and-persistence.md) | ステートフルなセッション管理と永続化（PlayerSaveData拡張） |
| 05 | [05-project-and-di.md](./05-project-and-di.md) | プロジェクト構成のライブラリ分割とDI導入 |
| 06 | [06-api-design.md](./06-api-design.md) | 追加するAPIエンドポイント設計案 |
| 07 | [07-migration-roadmap.md](./07-migration-roadmap.md) | 段階的な移行ロードマップと優先度 |

## 前提・スコープ

- API層は **ASP.NET Core Web API**（.NET 8）を想定。
- **既存のコンソール実行を壊さない**ことを方針とする（コアを共有し、UIアダプタだけ差し替える）。
- 本ドキュメントは「変更すべき点の洗い出し」が目的であり、実装そのものは含まない。
