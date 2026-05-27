# GameEngine - CLI / API RPG ゲームエンジン

ターン制 RPG のゲームロジックをコアライブラリ化し、コンソールと Web API の双方のホストから駆動できる .NET 8.0 製エンジンです。プレイヤーは敵と戦い、経験値を獲得し、装備を強化しながら冒険を進めます。

## 🚀 クイックスタート

```powershell
# セーブ機能を使う場合のみ（MongoDB を起動）
docker-compose up -d

# コンソール版を起動
dotnet run --project GameEngine.Console

# または Web API 版を起動（Swagger UI: http://localhost:5080/swagger）
dotnet run --project GameEngine.Api
```

## 特徴

- **ステップ駆動エンジン**: コアは内部ループを持たず、`Step(PlayerInput)` を 1 行動ずつ適用して進行。コンソール（駆動ループ）と API（1 リクエスト = 1 ステップ）で同一ロジックを共有
- **ターン制バトル**: プレイヤーと敵が交互に攻撃する戦闘システム
- **攻撃戦略システム**: Default / Melee / Magic の 3 種の攻撃タイプ（Strategy パターン）
- **経験値・レベルアップ**: 敵を倒して成長するプログレッションシステム
- **インベントリ・装備**: 武器やアイテムを管理・装備（装備ステータスが HP/AP/DP に反映）
- **ショップ・休憩イベント**: ゴールドで装備やポーションを購入、休憩で回復
- **セーブ機能**: MongoDB（Docker）による確定セーブ（`IPlayerRepository` で抽象化）
- **セッション層**: 進行中ゲームの揮発状態を `GameSessionState` で保持（API のマルチセッションに対応）
- **YAML 設定**: 敵・武器・ゲーム設定を外部ファイルで管理

## 必要環境

- .NET 8.0 SDK
- Windows / Linux / macOS
- Docker（セーブ機能を使用する場合）

## 依存パッケージ

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| YamlDotNet | 16.3.0 | YAML 設定ファイルの読み込み |
| MongoDB.Driver | 3.5.0 | MongoDB との通信（セーブ） |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.0 | DI 登録 |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger / OpenAPI（API のみ） |

## インストール・実行方法

### 1. リポジトリのクローン

```bash
git clone <repository-url>
cd ConsoleGame
```

### 2. MongoDB（Docker）のセットアップ（セーブ機能を使用する場合）

```powershell
docker-compose up -d   # MongoDB + Mongo Express を起動
docker-compose ps      # 起動確認
```

- **MongoDB**: ポート 27017（ゲームから接続）
- **Mongo Express**: http://localhost:8081（Web UI）
- 詳細は [docs/mongo.md](docs/mongo.md) を参照

### 3. ビルド・実行・テスト

```bash
dotnet build
dotnet run --project GameEngine.Console   # コンソール版
dotnet run --project GameEngine.Api       # API 版
dotnet test                               # テスト実行
```

## ゲームプレイ（コンソール版）

1. **開始**: プレイヤー名を入力
2. **イベント**: 探索ごとにショップ / バトル（重み付き抽選）が発生
3. **戦闘**: 矢印キー（←→）で攻撃戦略を選び Enter で決定
   - `Default`: 基本攻撃 / `Melee`: 近接攻撃 / `Magic`: 魔法攻撃
4. **成長**: 敵を倒すと経験値とゴールドを獲得
5. **進行確認**: 各イベント後に以下を選択
   - **Continue**: セーブせずに続行
   - **Save & Continue**: セーブして続行
   - **Save & Quit**: セーブして終了
   - **Quit**: セーブせずに終了

## Web API 版

- ベースパス `/api`。1 リクエスト = 1 ゲームステップで、レスポンスは更新後の状態を返す
- `sessionId` で進行を分離し、複数プレイヤーを並行処理
- レスポンスの `ExpectedInput` を見て次に叩くエンドポイントを決める（`Attack`→`battle/turn` / `Shop`→`shop/action` / `Rest`→`rest` / `GameAction`→`continue` / `None`=終了）
- 主なエンドポイント: `POST /sessions`（開始）/ `GET /sessions/{id}`（状態取得）/ `POST /sessions/{id}/battle/turn` / `POST /sessions/{id}/continue` / `POST /sessions/{id}/save`
- 詳細は [GameEngine.Api/CLAUDE.md](GameEngine.Api/CLAUDE.md) を参照

## 敵の種類

| 敵の名前 | HP | AP | DP | 経験値 | 攻撃タイプ |
|----------|-----|----|----|--------|-----------|
| Slime    | 10  | 4  | 1  | 10     | Default   |
| Goblin   | 30  | 5  | 2  | 20     | Melee     |
| Boss     | 100 | 10 | 5  | 50     | Magic     |

定義: [GameEngine/enemy-specs.yml](GameEngine/enemy-specs.yml)

## 武器の種類

| 武器  | HP  | AP | DP |
|-------|-----|----|----|
| Sword | 100 | 20 | 5  |
| Axe   | 80  | 30 | 3  |
| Spear | 90  | 25 | 4  |
| Bow   | 70  | 35 | 2  |
| Staff | 60  | 40 | 1  |

定義: [GameEngine/weapon-specs.yml](GameEngine/weapon-specs.yml)

## プロジェクト構造

```
GameEngine.sln
├── GameEngine/                 # コアライブラリ（Library）: ロジック・DTO・Manager・StateMachine・DI
│   ├── Configuration/          # GameConfig / 各ローダー
│   ├── Constants/              # AttackStrategyNames など
│   ├── DependencyInjection/    # AddGameEngine（コア依存の DI 登録）
│   ├── DTOs/                   # 状態 / コマンド / セーブ / セッション DTO
│   ├── Factory/                # EnemyFactory / WeaponFactory / PlayerFactory
│   ├── Interfaces/             # ICharacter, IPlayer, IGameInput, IRenderer, IPlayerRepository 等
│   ├── Manager/                # Health / Inventory / Experience / Repository
│   ├── Mappers/                # DTO ↔ ドメインの変換
│   ├── Models/                 # Player, Enemy, Weapon, AttackStrategy
│   ├── Systems/                # GameSystem, EventManager, ShopSystem, BattleSystem, StateMachine
│   ├── enemy-specs.yml         # 敵設定
│   ├── weapon-specs.yml        # 武器設定
│   └── game-config.yml         # ゲーム全体設定
├── GameEngine.Console/         # コンソールホスト（Exe）: Program.cs（合成起点）+ UI/
├── GameEngine.Api/             # Web API ホスト（ASP.NET Core）: マルチセッション駆動
└── GameEngine.Tests/           # テスト（xUnit + Moq）
```

## 設計パターン

- **Factory**: 敵・武器・プレイヤーの生成（`EnemyFactory` / `WeaponFactory` / `PlayerFactory`）
- **Strategy**: 攻撃戦略（`IAttackStrategy`）
- **Manager**: HP / インベントリ / 経験値などのリソース管理
- **Repository**: 永続化の抽象化（`IPlayerRepository`、本番 MongoDB / テスト InMemory）
- **DI**: `AddGameEngine()` がコア依存登録を集約し、ホストが I/O 依存を追加登録

詳細なアーキテクチャは [ai/overview.md](ai/overview.md) を参照。

## 拡張方法

- **新しい敵を追加**: [GameEngine/enemy-specs.yml](GameEngine/enemy-specs.yml) に設定追加（新戦略を使う場合のみコード変更）
- **新しい攻撃戦略を追加**: `IAttackStrategy` 実装 → `AttackStrategy.cs` にマッピング → `AttackStrategyNames` に登録 → `UserInteraction.cs` に選択肢追加
- **新しい武器・アイテムを追加**: `WeaponFactory.cs` に追加 → `ShopSystem.cs` で商品化

## 開発者向け情報

- **フレームワーク**: .NET 8.0 / C# 12
- **アーキテクチャ**: ステップ駆動 + レイヤー化（Factory / Strategy / Manager パターン）
- **コーディング規約**: Microsoft C# コーディング規約準拠
