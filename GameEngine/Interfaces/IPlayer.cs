using GameEngine.DTOs;

namespace GameEngine.Interfaces
{
    public interface IPlayer : ICharacter
    {
        int MaxHP { get; }
        int AP { get; }
        int DP { get; }
        int Level { get; }
        int TotalExperience { get; }
        string EquippedWeaponName { get; }

        void DefeatEnemy(IEnemy enemy);
        void GainGold(int amount);
        void BuyPotion(int amount);
        void EquipWeapon(IWeapon weapon);
        void ShowInfo();
        void UsePotion(int amount);
        int ReturnTotalPotions();
        int ReturnTotalGold();

        /// <summary>
        /// プレイヤーの現在の状態からPlayerSaveDataを作成する
        /// </summary>
        PlayerSaveData GetSaveData(string saveSlotName = "auto_save");
    }
}
