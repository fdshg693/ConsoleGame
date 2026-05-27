namespace GameEngine.Interfaces
{
    public interface IWeapon
    {
        int HP { get; } // Health Points
        int AP { get; } // Attack Power
        int DP { get; } // Defense Power
        string Name { get; } // Name of the weapon
    }
}
