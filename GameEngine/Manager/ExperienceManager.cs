using GameEngine.Models;

namespace GameEngine.Manager
{
    public class ExperienceManager
    {
        private readonly int _experienceRequiredForLevelUp;

        public int TotalExperience { get; private set; } = 0;
        public int Level { get; private set; } = 1;

        public ExperienceManager(int experienceRequiredForLevelUp)
        {
            if (experienceRequiredForLevelUp <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(experienceRequiredForLevelUp),
                    "Experience required for level up must be positive");

            _experienceRequiredForLevelUp = experienceRequiredForLevelUp;
        }

        /// <summary>
        /// Gain experience points and check for level up.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns>LevelUp</returns>
        public int GainExperience(int amount)
        {
            TotalExperience += amount;
            GameMessageBus.Publish($"You gain {amount} experience", MessageType.Experience);
            if (TotalExperience >= _experienceRequiredForLevelUp)
            {
                Level++;
                TotalExperience -= _experienceRequiredForLevelUp;
                GameMessageBus.Publish($"Level UP to level {Level}!", MessageType.Experience);
                return 1;
            }
            return 0;
        }
        public void ShowInfo()
        {
            GameMessageBus.Publish($"Total Experience: {TotalExperience}", MessageType.Info);
            GameMessageBus.Publish($"Level: {Level}", MessageType.Info);
        }
    }
}
