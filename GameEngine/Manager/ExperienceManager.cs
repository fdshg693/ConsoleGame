using GameEngine.Interfaces;
using GameEngine.Models;

namespace GameEngine.Manager
{
    public class ExperienceManager
    {
        private readonly int _experienceRequiredForLevelUp;
        private readonly IGameMessageBus _bus;

        public int TotalExperience { get; private set; } = 0;
        public int Level { get; private set; } = 1;

        public ExperienceManager(int experienceRequiredForLevelUp, IGameMessageBus bus)
        {
            if (experienceRequiredForLevelUp <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(experienceRequiredForLevelUp),
                    "Experience required for level up must be positive");

            _experienceRequiredForLevelUp = experienceRequiredForLevelUp;
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>
        /// Gain experience points and check for level up.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns>LevelUp</returns>
        public int GainExperience(int amount)
        {
            TotalExperience += amount;
            _bus.Publish($"You gain {amount} experience", MessageType.Experience);
            if (TotalExperience >= _experienceRequiredForLevelUp)
            {
                Level++;
                TotalExperience -= _experienceRequiredForLevelUp;
                _bus.Publish($"Level UP to level {Level}!", MessageType.Experience);
                return 1;
            }
            return 0;
        }
        public void ShowInfo()
        {
            _bus.Publish($"Total Experience: {TotalExperience}", MessageType.Info);
            _bus.Publish($"Level: {Level}", MessageType.Info);
        }
    }
}
