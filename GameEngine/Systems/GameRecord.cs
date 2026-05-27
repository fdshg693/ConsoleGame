using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;

namespace GameEngine.Systems
{
    /// <summary>
    /// 勝敗記録の実装（インスタンスベース）。<see cref="IGameRecord"/> を実装し、
    /// <c>AddGameEngine</c> が Singleton 登録する。静的状態を排除して並行リクエストでの混線を防ぐ。
    /// </summary>
    public class GameRecord : IGameRecord
    {
        public int TotalWins { get; private set; }
        public int TotalLosses { get; private set; }
        public int TotalGames => TotalWins + TotalLosses;

        public void RecordWin()
        {
            TotalWins++;
        }

        public void RecordLoss()
        {
            TotalLosses++;
        }

        public void Restore(int wins, int losses)
        {
            TotalWins = wins < 0 ? 0 : wins;
            TotalLosses = losses < 0 ? 0 : losses;
        }

        public List<GameMessage> GetRecordMessages()
        {
            return GameStateMapper.CreateMessages(
                ($"Total Wins: {TotalWins}", MessageType.Info),
                ($"Total Losses: {TotalLosses}", MessageType.Info),
                ($"Total Games: {TotalGames}", MessageType.Info),
                ($"Win Rate: {(TotalGames == 0 ? 0 : (double)TotalWins / TotalGames * 100):F2}%", MessageType.Info));
        }
    }
}
