using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Factory
{
    public static class WeaponFactory
    {
        public class WeaponSpec
        {
            public string Name { get; set; } = "";
            public int HP { get; set; }
            public int AP { get; set; }
            public int DP { get; set; }
        }

        private static readonly Dictionary<string, WeaponSpec> _specs;
        private const string DefaultYamlPath = "./weapon-specs.yml";

        static WeaponFactory()
        {
            _specs = LoadWeaponSpecs(DefaultYamlPath);
        }

        private static Dictionary<string, WeaponSpec> LoadWeaponSpecs(string yamlPath)
        {
            return YamlSpecLoader.Load<WeaponSpec>(
                yamlPath,
                specLabelTitle: "Weapon",
                validate: ValidateWeaponSpec,
                comparer: StringComparer.OrdinalIgnoreCase);
        }

        private static void ValidateWeaponSpec(string key, WeaponSpec spec)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(spec.Name))
                errors.Add("Name is required");

            if (spec.HP <= 0)
                errors.Add($"HP must be positive (got {spec.HP})");

            if (spec.AP < 0)
                errors.Add($"AP cannot be negative (got {spec.AP})");

            if (spec.DP < 0)
                errors.Add($"DP cannot be negative (got {spec.DP})");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Invalid weapon spec for key '{key}':\n  - {string.Join("\n  - ", errors)}");
            }
        }

        public static IWeapon CreateWeapon(string weaponType)
        {
            if (string.IsNullOrWhiteSpace(weaponType))
                throw new ArgumentException("Weapon type cannot be null or empty", nameof(weaponType));

            if (!_specs.TryGetValue(weaponType, out var spec))
            {
                throw new ArgumentException(
                    $"Unknown weapon type: '{weaponType}'. Available keys: {string.Join(", ", _specs.Keys)}",
                    nameof(weaponType));
            }

            return new Weapon(spec.HP, spec.AP, spec.DP, spec.Name);
        }
    }
}
