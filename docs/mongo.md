# MongoDB完全ガイド - GameEngine用

このドキュメントでは、GameEngineプロジェクトでMongoDBを使用するための完全な情報を提供します。

## 目次

1. [MongoDBとは](#mongodbとは)
2. [Docker Composeによる管理](#docker-composeによる管理)
3. [基本操作](#基本操作)
4. [Docker操作コマンド](#docker操作コマンド)
5. [MongoDB基本コマンド](#mongodb基本コマンド)
6. [データ操作](#データ操作)
7. [バックアップとリストア](#バックアップとリストア)
8. [トラブルシューティング](#トラブルシューティング)
9. [パフォーマンスチューニング](#パフォーマンスチューニング)

---

## MongoDBとは

### 概要
MongoDBは、NoSQL（Not Only SQL）データベースの一種で、JSONライクなドキュメント形式でデータを保存します。

### 主な特徴
- **ドキュメント指向**: JSON形式でデータを保存
- **スキーマレス**: 柔軟なデータ構造
- **高速**: インデックスによる高速検索
- **スケーラブル**: 水平スケーリングが容易

### GameEngineでの使用目的
- プレイヤーのセーブデータ保存
- ゲーム進行状況の永続化
- 複数セーブスロットの管理

---

## Docker Composeによる管理

### ファイル構成

```
ConsoleGame/
├── docker-compose.yml          # Docker Compose設定
├── Dockerfile.mongo            # MongoDBカスタムDockerfile
├── .env.example                # 環境変数テンプレート
└── mongo-init/
    └── init-db.js              # DB初期化スクリプト
```

### セットアップ手順

#### 1. 環境変数ファイルの作成

```powershell
# .env.exampleをコピーして.envを作成
Copy-Item .env.example .env

# 必要に応じて.envを編集
notepad .env
```

#### 2. Docker Composeで起動

```powershell
# すべてのサービスを起動（MongoDB + Mongo Express）
docker-compose up -d

# MongoDBのみ起動
docker-compose up -d mongodb

# ログを確認
docker-compose logs -f mongodb
```

#### 3. サービスの確認

```powershell
# 起動中のサービスを確認
docker-compose ps

# ヘルスチェック
docker-compose ps mongodb
```

### Docker Composeの利点

✅ **簡単な起動・停止**
```powershell
docker-compose up -d      # 起動
docker-compose stop       # 停止
docker-compose down       # 停止＋削除
docker-compose restart    # 再起動
```

✅ **データの永続化**
- `mongodb_data`ボリュームでデータを保存
- コンテナを削除してもデータは保持される

✅ **Mongo Express統合**
- Web UIで簡単にデータ確認
- http://localhost:8081 でアクセス
- ユーザー名: `admin`, パスワード: `admin`

---

## 基本操作

### サービスの起動と停止

```powershell
# 起動
docker-compose up -d

# 停止（データは保持）
docker-compose stop

# 停止＋コンテナ削除（データは保持）
docker-compose down

# 停止＋コンテナ削除＋ボリューム削除（データ削除）
docker-compose down -v
```

### ステータス確認

```powershell
# サービス一覧
docker-compose ps

# ログ表示（リアルタイム）
docker-compose logs -f

# MongoDBログのみ
docker-compose logs -f mongodb

# 最新100行のログ
docker-compose logs --tail=100 mongodb
```

---

## Docker操作コマンド

### コンテナ管理

```powershell
# コンテナ一覧
docker ps
docker ps -a  # 停止中も含む

# コンテナの起動・停止
docker start mongodb-gameengine
docker stop mongodb-gameengine
docker restart mongodb-gameengine

# コンテナの削除
docker rm mongodb-gameengine
docker rm -f mongodb-gameengine  # 強制削除
```

### コンテナ内でコマンド実行

```powershell
# MongoDBシェルに接続
docker exec -it mongodb-gameengine mongosh

# 管理者としてログイン
docker exec -it mongodb-gameengine mongosh -u admin -p password --authenticationDatabase admin

# Bashシェルに入る
docker exec -it mongodb-gameengine bash

# ワンライナーでコマンド実行
docker exec mongodb-gameengine mongosh --eval "db.adminCommand('ping')"
```

### ログとデバッグ

```powershell
# ログ表示
docker logs mongodb-gameengine
docker logs -f mongodb-gameengine  # フォロー
docker logs --tail=50 mongodb-gameengine  # 最新50行

# コンテナの詳細情報
docker inspect mongodb-gameengine

# リソース使用状況
docker stats mongodb-gameengine
```

### ボリューム管理

```powershell
# ボリューム一覧
docker volume ls

# ボリュームの詳細
docker volume inspect consolegame_mongodb_data

# ボリュームの削除
docker volume rm consolegame_mongodb_data

# 未使用ボリュームの一括削除
docker volume prune
```

### ネットワーク管理

```powershell
# ネットワーク一覧
docker network ls

# ネットワークの詳細
docker network inspect consolegame_gameengine-network

# コンテナのIPアドレス確認
docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' mongodb-gameengine
```

---

## MongoDB基本コマンド

### MongoDBシェルへの接続

```powershell
# Docker経由で接続
docker exec -it mongodb-gameengine mongosh

# 管理者として接続
docker exec -it mongodb-gameengine mongosh -u admin -p password --authenticationDatabase admin
```

### データベース操作

```javascript
// データベース一覧
show dbs

// データベース切り替え
use GameEngineDB

// 現在のデータベース確認
db.getName()

// データベース削除
db.dropDatabase()

// データベース統計
db.stats()
```

### コレクション操作

```javascript
// コレクション一覧
show collections

// コレクション作成
db.createCollection('PlayerSaves')

// コレクション削除
db.PlayerSaves.drop()

// コレクション名変更
db.PlayerSaves.renameCollection('PlayerSavesBackup')

// コレクション統計
db.PlayerSaves.stats()
```

### インデックス操作

```javascript
// インデックス一覧
db.PlayerSaves.getIndexes()

// インデックス作成
db.PlayerSaves.createIndex({ "playerName": 1 })  // 昇順
db.PlayerSaves.createIndex({ "savedAt": -1 })    // 降順
db.PlayerSaves.createIndex({ "playerName": 1, "saveSlotName": 1 }, { unique: true })  // 複合ユニーク

// インデックス削除
db.PlayerSaves.dropIndex("playerName_1")

// 全インデックス削除（_id以外）
db.PlayerSaves.dropIndexes()
```

---

## データ操作

### GameEngineのセーブデータ操作

```javascript
// GameEngineDBに切り替え
use GameEngineDB

// 全セーブデータを表示
db.PlayerSaves.find().pretty()

// 特定プレイヤーのセーブデータ検索
db.PlayerSaves.find({ "playerName": "TestPlayer" }).pretty()

// 最新のセーブデータ
db.PlayerSaves.find().sort({ "savedAt": -1 }).limit(1).pretty()

// レベル10以上のプレイヤー
db.PlayerSaves.find({ "level": { $gte: 10 } }).pretty()

// ゴールドが多い順
db.PlayerSaves.find().sort({ "totalGold": -1 }).pretty()
```

### CRUD操作

#### Create（作成）

```javascript
// 単一ドキュメント挿入
db.PlayerSaves.insertOne({
    playerName: "TestPlayer",
    currentHP: 100,
    maxHP: 100,
    baseAP: 10,
    baseDP: 5,
    totalGold: 50,
    totalPotions: 0,
    level: 1,
    totalExperience: 0,
    equippedWeapon: {
        name: "Sword",
        hp: 100,
        ap: 20,
        dp: 5
    },
    attackStrategy: "Melee",
    savedAt: new Date(),
    saveSlotName: "auto_save"
})

// 複数ドキュメント挿入
db.PlayerSaves.insertMany([
    { playerName: "Player1", level: 1, totalGold: 100 },
    { playerName: "Player2", level: 5, totalGold: 500 }
])
```

#### Read（読み取り）

```javascript
// 全件取得
db.PlayerSaves.find()

// 条件付き検索
db.PlayerSaves.find({ "playerName": "TestPlayer" })

// 特定フィールドのみ取得（projection）
db.PlayerSaves.find(
    { "playerName": "TestPlayer" },
    { playerName: 1, level: 1, totalGold: 1, _id: 0 }
)

// 1件のみ取得
db.PlayerSaves.findOne({ "playerName": "TestPlayer" })

// 件数カウント
db.PlayerSaves.countDocuments({ "level": { $gte: 5 } })
```

#### Update（更新）

```javascript
// 単一ドキュメント更新
db.PlayerSaves.updateOne(
    { "playerName": "TestPlayer", "saveSlotName": "auto_save" },
    { $set: { "level": 10, "totalGold": 1000 } }
)

// 複数ドキュメント更新
db.PlayerSaves.updateMany(
    { "level": { $lt: 5 } },
    { $inc: { "totalGold": 100 } }  // ゴールドを100増やす
)

// Upsert（存在しなければ挿入）
db.PlayerSaves.updateOne(
    { "playerName": "NewPlayer" },
    { $set: { "level": 1, "totalGold": 50 } },
    { upsert: true }
)

// フィールド削除
db.PlayerSaves.updateOne(
    { "playerName": "TestPlayer" },
    { $unset: { "oldField": "" } }
)
```

#### Delete（削除）

```javascript
// 単一ドキュメント削除
db.PlayerSaves.deleteOne({ "playerName": "TestPlayer" })

// 複数ドキュメント削除
db.PlayerSaves.deleteMany({ "level": { $lt: 5 } })

// 全件削除
db.PlayerSaves.deleteMany({})
```

### 高度なクエリ

```javascript
// AND条件
db.PlayerSaves.find({
    "level": { $gte: 5 },
    "totalGold": { $gte: 500 }
})

// OR条件
db.PlayerSaves.find({
    $or: [
        { "level": { $gte: 10 } },
        { "totalGold": { $gte: 1000 } }
    ]
})

// 配列内検索
db.PlayerSaves.find({
    "equippedWeapon.name": "Sword"
})

// 正規表現検索
db.PlayerSaves.find({
    "playerName": { $regex: /^Test/, $options: "i" }  // Testで始まる（大文字小文字無視）
})

// 集計（Aggregation）
db.PlayerSaves.aggregate([
    { $match: { "level": { $gte: 5 } } },
    { $group: {
        _id: "$level",
        totalPlayers: { $sum: 1 },
        avgGold: { $avg: "$totalGold" }
    }},
    { $sort: { _id: 1 } }
])
```

---

## バックアップとリストア

### データベース全体のバックアップ

```powershell
# バックアップディレクトリ作成
mkdir -p ./backup

# mongodumpでバックアップ
docker exec mongodb-gameengine mongodump \
  --db=GameEngineDB \
  --out=/data/backup

# ホストにコピー
docker cp mongodb-gameengine:/data/backup ./backup

# または直接ホストに保存
docker exec mongodb-gameengine mongodump \
  --db=GameEngineDB \
  --archive=/data/backup/gameengine-backup.archive

docker cp mongodb-gameengine:/data/backup/gameengine-backup.archive ./backup/
```

### データのリストア

```powershell
# バックアップファイルをコンテナにコピー
docker cp ./backup/gameengine-backup.archive mongodb-gameengine:/data/restore/

# mongorestore実行
docker exec mongodb-gameengine mongorestore \
  --db=GameEngineDB \
  --archive=/data/restore/gameengine-backup.archive
```

### エクスポート・インポート（JSON形式）

```powershell
# PlayerSavesコレクションをJSON形式でエクスポート
docker exec mongodb-gameengine mongoexport \
  --db=GameEngineDB \
  --collection=PlayerSaves \
  --out=/data/backup/playersaves.json

docker cp mongodb-gameengine:/data/backup/playersaves.json ./backup/

# JSON形式でインポート
docker cp ./backup/playersaves.json mongodb-gameengine:/data/restore/

docker exec mongodb-gameengine mongoimport \
  --db=GameEngineDB \
  --collection=PlayerSaves \
  --file=/data/restore/playersaves.json
```

### 自動バックアップスクリプト

```powershell
# backup.ps1
$BackupDir = ".\backup\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $BackupDir -Force

docker exec mongodb-gameengine mongodump `
  --db=GameEngineDB `
  --archive=/data/backup/backup.archive

docker cp mongodb-gameengine:/data/backup/backup.archive "$BackupDir\gameengine.archive"

Write-Host "Backup completed: $BackupDir"
```

---

## トラブルシューティング

### 接続できない

```powershell
# 1. コンテナが起動しているか確認
docker ps | Select-String "mongodb-gameengine"

# 2. ポートが開いているか確認
netstat -an | Select-String "27017"

# 3. ログを確認
docker logs mongodb-gameengine --tail=50

# 4. ヘルスチェック
docker inspect mongodb-gameengine | Select-String "Health"

# 5. 接続テスト
docker exec mongodb-gameengine mongosh --eval "db.adminCommand('ping')"
```

### メモリ不足

```powershell
# メモリ使用状況確認
docker stats mongodb-gameengine

# docker-compose.ymlにメモリ制限追加
# services:
#   mongodb:
#     mem_limit: 512m
#     memswap_limit: 512m
```

### ディスク容量不足

```powershell
# ディスク使用量確認
docker system df

# 不要なデータ削除
docker system prune -a --volumes

# ログファイルサイズ確認
docker inspect mongodb-gameengine --format='{{.LogPath}}' | ForEach-Object { Get-Item $_ }
```

### データ破損

```powershell
# データベース修復
docker exec mongodb-gameengine mongosh --eval "db.repairDatabase()"

# または再起動時に修復
docker-compose down
docker-compose up -d mongodb --force-recreate
```

### パスワードを忘れた場合

```powershell
# 認証なしで起動
docker run -d --name mongodb-temp -p 27018:27017 mongo --noauth

# 新しいパスワード設定
docker exec -it mongodb-temp mongosh
# > use admin
# > db.updateUser("admin", { pwd: "newpassword" })

# 元に戻す
docker stop mongodb-temp
docker rm mongodb-temp
```

---

## パフォーマンスチューニング

### インデックスの最適化

```javascript
// 遅いクエリを見つける
db.setProfilingLevel(2)  // 全クエリをログ

// プロファイルデータ確認
db.system.profile.find().sort({ ts: -1 }).limit(5).pretty()

// クエリ実行計画確認
db.PlayerSaves.find({ "playerName": "TestPlayer" }).explain("executionStats")

// 推奨インデックスの確認
db.PlayerSaves.aggregate([{ $indexStats: {} }])
```

### キャッシュ設定

```javascript
// WiredTigerキャッシュサイズ確認
db.serverStatus().wiredTiger.cache

// 統計情報
db.serverStatus().connections
db.serverStatus().opcounters
```

### 接続プール設定

game-config.ymlに追加：

```yaml
MongoDB:
  ConnectionString: "mongodb://localhost:27017/?maxPoolSize=50&minPoolSize=10"
  DatabaseName: "GameEngineDB"
  CollectionName: "PlayerSaves"
```

### Docker Composeリソース制限

```yaml
services:
  mongodb:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '1.0'
          memory: 512M
```

---

## 便利なコマンド集

### データベース統計

```javascript
// データベースサイズ
db.stats()

// コレクションサイズ
db.PlayerSaves.stats()

// インデックスサイズ
db.PlayerSaves.totalIndexSize()

// ドキュメント数
db.PlayerSaves.countDocuments()
```

### メンテナンス

```javascript
// コレクション圧縮（ディスク容量削減）
db.runCommand({ compact: "PlayerSaves" })

// 統計情報更新
db.runCommand({ reIndex: "PlayerSaves" })

// 接続数確認
db.serverStatus().connections
```

### モニタリング

```powershell
# リアルタイムモニタリング
docker exec -it mongodb-gameengine mongosh --eval "db.currentOp()"

# 現在の操作確認
docker exec -it mongodb-gameengine mongosh --eval "db.currentOp({ active: true })"
```

---

## 参考リンク

- [MongoDB公式ドキュメント](https://docs.mongodb.com/)
- [Docker Hub - MongoDB](https://hub.docker.com/_/mongo)
- [Mongo Express](https://github.com/mongo-express/mongo-express)
- [MongoDBクエリリファレンス](https://docs.mongodb.com/manual/reference/operator/query/)

---

## まとめ

このドキュメントでは、GameEngineプロジェクトでMongoDBを使用するための包括的な情報を提供しました。

### クイックスタート

```powershell
# 1. 起動
docker-compose up -d

# 2. 接続確認
docker exec -it mongodb-gameengine mongosh

# 3. ゲーム実行
cd GameEngine
dotnet run

# 4. Web UIでデータ確認
# http://localhost:8081
```

### 日常的な操作

```powershell
# 起動
docker-compose up -d

# 停止
docker-compose stop

# ログ確認
docker-compose logs -f mongodb

# データ確認（Web UI）
start http://localhost:8081
```

質問や問題がある場合は、[トラブルシューティング](#トラブルシューティング)セクションを参照してください。
