using GameEngine.Constants;
using GameEngine.Models;

namespace GameEngine.Manager
{
    public class ExperienceManager
    {
        public int TotalExperience { get; private set; } = 0;
        public int Level { get; private set; } = 1;
        /// <summary>
        /// Gain experience points and check for level up.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns>LevelUp</returns>
        public int GainExperience(int amount)
        {
            TotalExperience += amount;
            GameMessageBus.Publish($"You gain {amount} experience", MessageType.Experience);
            if (TotalExperience >= GameConstants.ExperienceRequiredForLevelUp)
            {
                Level++;
                TotalExperience -= GameConstants.ExperienceRequiredForLevelUp;
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
