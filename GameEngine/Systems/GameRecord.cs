using GameEngine.Mappers;
using GameEngine.Models;

namespace GameEngine.Systems
{
    public static class GameRecord
    {
        public static int TotalWins { get; private set; }
        public static int TotalLosses { get; private set; }
        public static int TotalGames => TotalWins + TotalLosses;
        public static void RecordWin()
        {
            TotalWins++;
        }
        public static void RecordLoss()
        {
            TotalLosses++;
        }
        public static List<GameMessage> GetRecordMessages()
        {
            return GameStateMapper.CreateMessages(
                ($"Total Wins: {TotalWins}", MessageType.Info),
                ($"Total Losses: {TotalLosses}", MessageType.Info),
                ($"Total Games: {TotalGames}", MessageType.Info),
                ($"Win Rate: {(TotalGames == 0 ? 0 : (double)TotalWins / TotalGames * 100):F2}%", MessageType.Info));
        }
    }
}
