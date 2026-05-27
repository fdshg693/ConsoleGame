# GameEngine - CLI RPGゲーム

コンソール上で動作するRPGゲームエンジンです。プレイヤーは敵と戦い、経験値を獲得し、装備を強化しながら冒険を進めます。

## 🚀 クイックスタート

```powershell
docker-compose up -d
cd GameEngine
dotnet run
```

詳細は [QUICKSTART.md](QUICKSTART.md) を参照してください。

## 特徴

- **ターン制バトルシステム**: プレイヤーと敵が交互に攻撃する戦闘システム
- **攻撃戦略システム**: デフォルト、近接、魔法の3つの攻撃タイプ
- **経験値・レベルアップシステム**: 敵を倒して成長するプログレッションシステム
- **インベントリ・装備システム**: 武器やアイテムを管理・装備
- **ショップシステム**: ゴールドを使って装備やアイテムを購入
- **セーブ機能**: MongoDB（Docker）によるゲームデータの保存
- **YAML設定**: 敵の設定を外部ファイルで管理

## 必要環境

- .NET 8.0
- Windows/Linux/macOS
- Docker（セーブ機能を使用する場合）

## 依存関係

- **YamlDotNet** (v16.3.0): YAML設定ファイルの読み込み
- **MongoDB.Driver** (v3.5.0): MongoDBとの通信

## インストール・実行方法

### 1. リポジトリのクローン
```bash
git clone <repository-url>
cd ConsoleGame
```

### 2. MongoDB（Docker）のセットアップ（セーブ機能を使用する場合）

セーブ機能を利用する場合は、Docker Composeで環境を起動してください：

```powershell
# MongoDBとMongo Expressを起動
docker-compose up -d

# 起動確認
docker-compose ps
```

これで以下が利用可能になります：
- **MongoDB**: ポート 27017（ゲームから接続）
- **Mongo Express**: http://localhost:8081（Web UI）

詳細な手順は [MONGODB_SETUP.md](MONGODB_SETUP.md) を参照してください。

**MongoDB操作の詳細**については [docs/mongo.md](docs/mongo.md) を参照してください。

### 3. ビルド・実行
```bash
dotnet build
dotnet run --project GameEngine
```

または、発行済みの実行ファイルを使用:
```bash
# Windows
./GameEngine/bin/Release/net8.0/GameEngine.exe

# Linux
./publish/linux-x64/GameEngine
```

## ゲームプレイ

1. **開始**: ゲーム開始時にプレイヤー名を入力
2. **イベント**: ランダムで以下のイベントが発生
   - **ショップ**: ゴールドで武器やポーションを購入
   - **バトル**: 敵と戦闘
3. **戦闘**: 矢印キー（←→）で攻撃戦略を選択し、Enterで決定
4. **進行確認**: 各イベント後に以下の選択肢が表示されます
   - **Continue**: セーブせずに続行
   - **Save & Continue**: セーブして続行
   - **Save & Quit**: セーブして終了
   - **Quit**: セーブせずに終了
   - **戦闘**: 敵との戦いが始まる
3. **戦闘**: 攻撃戦略を選択して敵と戦う
   - `Default`: 基本攻撃
   - `Melee`: 近接攻撃
   - `Magic`: 魔法攻撃
4. **成長**: 敵を倒すと経験値とゴールドを獲得
5. **継続**: HPが0になるまでゲームが続く

## 敵の種類

現在実装されている敵:

| 敵の名前 | HP | AP | DP | 経験値 | 攻撃タイプ |
|----------|----|----|----|---------|---------| 
| Slime    | 10 | 4  | 1  | 10      | Default |
| Goblin   | 30 | 5  | 2  | 20      | Melee   |
| Boss     | 100| 10 | 5  | 50      | Magic   |

## プロジェクト構造

```
GameEngine/
├── Program.cs                 # メインエントリーポイント
├── enemy-specs.yml           # 敵の設定ファイル
├── Factory/                  # ファクトリーパターン実装
│   ├── EnemyFactory.cs       # 敵生成ファクトリー
│   └── WeaponFactory.cs      # 武器生成ファクトリー
├── Interfaces/               # インターフェース定義
│   ├── IAttackStrategy.cs    # 攻撃戦略インターフェース
│   ├── ICharacter.cs         # キャラクター基底インターフェース
│   ├── IEnemy.cs             # 敵インターフェース
│   ├── IPlayer.cs            # プレイヤーインターフェース
│   └── IWeapon.cs            # 武器インターフェース
├── Manager/                  # 各種マネージャークラス
│   ├── ExperienceManager.cs  # 経験値管理
│   ├── HealthManager.cs      # HP/DP管理
│   └── InventoryManager.cs   # インベントリ管理
├── Models/                   # モデルクラス
│   ├── AttackStrategy.cs     # 攻撃戦略実装
│   ├── Enemy.cs              # 敵クラス
│   ├── Player.cs             # プレイヤークラス
│   └── Weapon.cs             # 武器クラス
└── Systems/                  # ゲームシステム
    ├── GameSystem.cs         # メインゲームシステム
    ├── GameRecord.cs         # ゲーム記録
    ├── RestSystem.cs         # 休息・回復システム
    ├── ShopSystem.cs         # ショップシステム
    ├── UserInteraction.cs    # ユーザー入力処理
    └── BattleSystem/         # 戦闘システム
```

## 設計パターン

このプロジェクトでは以下の設計パターンを採用しています:

- **Factory Pattern**: 敵と武器の生成
- **Strategy Pattern**: 攻撃戦略の実装
- **Manager Pattern**: 各種リソースの管理
- **Interface Segregation**: 機能ごとのインターフェース分離

## 拡張方法

### 新しい敵を追加
1. `enemy-specs.yml`に新しい敵の設定を追加
2. 必要に応じて`EnemyFactory.cs`を更新

### 新しい攻撃戦略を追加
1. `IAttackStrategy`を実装した新しいクラスを作成
2. `AttackStrategy.cs`に戦略を追加
3. `UserInteraction.cs`に選択肢を追加

### 新しいアイテム・武器を追加
1. `WeaponFactory.cs`に新しい武器を追加
2. `ShopSystem.cs`でショップに商品を追加

## 開発者向け情報

- **フレームワーク**: .NET 8.0
- **言語**: C# 12
- **アーキテクチャ**: レイヤー化アーキテクチャ
- **コーディング規約**: Microsoft C# コーディング規約準拠