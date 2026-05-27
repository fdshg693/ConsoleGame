// MongoDB初期化スクリプト
// このスクリプトはコンテナ初回起動時に自動実行されます

// GameEngineDBデータベースに切り替え
db = db.getSiblingDB('GameEngineDB');

// PlayerSavesコレクションを作成
db.createCollection('PlayerSaves');

// インデックスの作成（検索パフォーマンス向上）
db.PlayerSaves.createIndex({ "playerName": 1, "saveSlotName": 1 }, { unique: true });
db.PlayerSaves.createIndex({ "savedAt": -1 });
db.PlayerSaves.createIndex({ "level": 1 });

print('GameEngineDB initialized successfully!');
print('Collections:', db.getCollectionNames());

// サンプルデータの挿入（オプション）
// db.PlayerSaves.insertOne({
//     playerName: "TestPlayer",
//     currentHP: 100,
//     maxHP: 100,
//     baseAP: 10,
//     baseDP: 5,
//     totalGold: 50,
//     totalPotions: 0,
//     level: 1,
//     totalExperience: 0,
//     equippedWeapon: {
//         name: "Default",
//         hp: 0,
//         ap: 0,
//         dp: 0
//     },
//     attackStrategy: "Default",
//     savedAt: new Date(),
//     saveSlotName: "auto_save"
// });

print('Indexes created:', db.PlayerSaves.getIndexes());
