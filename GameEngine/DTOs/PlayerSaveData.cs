namespace GameEngine.DTOs
{
    /// <summary>
    /// プレイヤーデータの永続化用DTO
    /// MongoDB固有の属性は MongoPlayerRepository 側の BsonClassMap で定義
    /// </summary>
    public class PlayerSaveData
    {
        public string? Id { get; set; }

        public required string PlayerName { get; set; }

        public int CurrentHP { get; set; }

        public int MaxHP { get; set; }

        public int BaseAP { get; set; }

        public int BaseDP { get; set; }

        public int TotalGold { get; set; }

        public int TotalPotions { get; set; }

        public int Level { get; set; }

        public int TotalExperience { get; set; }

        public WeaponData EquippedWeapon { get; set; } = new WeaponData();

        public string AttackStrategy { get; set; } = "Default";

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        public string SaveSlotName { get; set; } = "auto_save";
    }

    /// <summary>
    /// 武器データのサブモデル
    /// </summary>
    public class WeaponData
    {
        public string Name { get; set; } = "Default";

        public int HP { get; set; }

        public int AP { get; set; }

        public int DP { get; set; }
    }
}
