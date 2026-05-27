using GameEngine.Interfaces;

namespace GameEngine.Models
{
    public class Weapon : IWeapon
    {
        public int HP { get; private set; } // Health Points
        public int AP { get; private set; } // Attack Power
        public int DP { get; private set; } // Defense Power
        public string Name { get; private set; } // Name of the weapon
        public Weapon(int hp, int ap, int dp, string name)
        {
            HP = hp;
            AP = ap;
            DP = dp;
            Name = name;
        }
    }
}
