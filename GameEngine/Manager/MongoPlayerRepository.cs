using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace GameEngine.Manager
{
    /// <summary>
    /// MongoDBを使用してプレイヤーデータの保存・読み込みを管理するクラス
    /// BsonClassMap により PlayerSaveData/WeaponData の MongoDB マッピングをリポジトリ側に閉じ込める
    /// </summary>
    public class MongoPlayerRepository : IPlayerRepository
    {
        private readonly MongoClient _client;
        private readonly IMongoCollection<PlayerSaveData> _collection;

        static MongoPlayerRepository()
        {
            RegisterBsonClassMaps();
        }

        public MongoPlayerRepository(string connectionString, string databaseName, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            _client = new MongoClient(connectionString);
            var database = _client.GetDatabase(databaseName);
            _collection = database.GetCollection<PlayerSaveData>(collectionName);
        }

        public async Task<bool> SaveAsync(IPlayer player, string saveSlotName = "auto_save")
        {
            var saveData = player.GetSaveData(saveSlotName);

            var filter = Builders<PlayerSaveData>.Filter.And(
                Builders<PlayerSaveData>.Filter.Eq(x => x.PlayerName, saveData.PlayerName),
                Builders<PlayerSaveData>.Filter.Eq(x => x.SaveSlotName, saveSlotName)
            );

            var options = new ReplaceOptions { IsUpsert = true };
            await _collection.ReplaceOneAsync(filter, saveData, options);
            return true;
        }

        public async Task<PlayerSaveData?> LoadAsync(string playerName, string saveSlotName = "auto_save")
        {
            var filter = Builders<PlayerSaveData>.Filter.And(
                Builders<PlayerSaveData>.Filter.Eq(x => x.PlayerName, playerName),
                Builders<PlayerSaveData>.Filter.Eq(x => x.SaveSlotName, saveSlotName)
            );

            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<PlayerSaveData>> GetSaveListAsync(string playerName)
        {
            var filter = Builders<PlayerSaveData>.Filter.Eq(x => x.PlayerName, playerName);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<bool> DeleteAsync(string playerName, string saveSlotName)
        {
            var filter = Builders<PlayerSaveData>.Filter.And(
                Builders<PlayerSaveData>.Filter.Eq(x => x.PlayerName, playerName),
                Builders<PlayerSaveData>.Filter.Eq(x => x.SaveSlotName, saveSlotName)
            );

            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<bool> TestConnectionAsync()
        {
            await _client.ListDatabaseNamesAsync();
            return true;
        }

        private static void RegisterBsonClassMaps()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(PlayerSaveData)))
            {
                BsonClassMap.RegisterClassMap<PlayerSaveData>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(c => c.Id)
                      .SetSerializer(new MongoDB.Bson.Serialization.Serializers.StringSerializer(BsonType.ObjectId));
                    cm.MapMember(c => c.PlayerName).SetElementName("playerName");
                    cm.MapMember(c => c.CurrentHP).SetElementName("currentHP");
                    cm.MapMember(c => c.MaxHP).SetElementName("maxHP");
                    cm.MapMember(c => c.BaseAP).SetElementName("baseAP");
                    cm.MapMember(c => c.BaseDP).SetElementName("baseDP");
                    cm.MapMember(c => c.TotalGold).SetElementName("totalGold");
                    cm.MapMember(c => c.TotalPotions).SetElementName("totalPotions");
                    cm.MapMember(c => c.Level).SetElementName("level");
                    cm.MapMember(c => c.TotalExperience).SetElementName("totalExperience");
                    cm.MapMember(c => c.EquippedWeapon).SetElementName("equippedWeapon");
                    cm.MapMember(c => c.AttackStrategy).SetElementName("attackStrategy");
                    cm.MapMember(c => c.SavedAt).SetElementName("savedAt");
                    cm.MapMember(c => c.SaveSlotName).SetElementName("saveSlotName");
                    cm.SetIgnoreExtraElements(true);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(WeaponData)))
            {
                BsonClassMap.RegisterClassMap<WeaponData>(cm =>
                {
                    cm.AutoMap();
                    cm.MapMember(c => c.Name).SetElementName("name");
                    cm.MapMember(c => c.HP).SetElementName("hp");
                    cm.MapMember(c => c.AP).SetElementName("ap");
                    cm.MapMember(c => c.DP).SetElementName("dp");
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }
    }
}
