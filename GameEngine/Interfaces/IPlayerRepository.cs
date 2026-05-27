using GameEngine.DTOs;

namespace GameEngine.Interfaces
{
    public interface IPlayerRepository
    {
        Task<bool> SaveAsync(IPlayer player, string saveSlotName = "auto_save");
        Task<PlayerSaveData?> LoadAsync(string playerName, string saveSlotName = "auto_save");
        Task<List<PlayerSaveData>> GetSaveListAsync(string playerName);
        Task<bool> DeleteAsync(string playerName, string saveSlotName);
        Task<bool> TestConnectionAsync();
    }
}
