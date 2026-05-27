namespace GameEngine.Interfaces
{
    // 装備（Weapon）からHP／DPボーナスを取得するだけの抽象
    public interface IEquipmentStatsProvider
    {
        IWeapon Weapon { get; }

        event Action? EquipmentChanged;
    }
}
