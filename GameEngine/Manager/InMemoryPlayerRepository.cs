using GameEngine.DTOs;
using GameEngine.Interfaces;

namespace GameEngine.Manager
{
    /// <summary>
    /// インメモリでプレイヤーデータを管理するリポジトリ（テスト用）
    /// </summary>
    public class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly Dictionary<(string PlayerName, string SlotName), PlayerSaveData> _store = new();

        public Task<bool> SaveAsync(IPlayer player, string saveSlotName = "auto_save")
        {
            var saveData = player.GetSaveData(saveSlotName);
            _store[(saveData.PlayerName, saveSlotName)] = saveData;
            return Task.FromResult(true);
        }

        public Task<PlayerSaveData?> LoadAsync(string playerName, string saveSlotName = "auto_save")
        {
            _store.TryGetValue((playerName, saveSlotName), out var data);
            return Task.FromResult(data);
        }

        public Task<List<PlayerSaveData>> GetSaveListAsync(string playerName)
        {
            var list = _store
                .Where(kv => kv.Key.PlayerName == playerName)
                .Select(kv => kv.Value)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<bool> DeleteAsync(string playerName, string saveSlotName)
        {
            var removed = _store.Remove((playerName, saveSlotName));
            return Task.FromResult(removed);
        }

        public Task<bool> TestConnectionAsync()
        {
            return Task.FromResult(true);
        }
    }
}
