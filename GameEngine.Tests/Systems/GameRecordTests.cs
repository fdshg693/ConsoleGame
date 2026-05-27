using GameEngine.Interfaces;
using GameEngine.Systems;
using Xunit;

namespace GameEngine.Tests.Systems
{
    /// <summary>
    /// フェーズ3で静的からインスタンスへサービス化した <see cref="GameRecord"/>（<see cref="IGameRecord"/>）の検証。
    /// 並行リクエストでの混線を避けるためインスタンス単位で勝敗を保持し、復元できることを担保する。
    /// </summary>
    public class GameRecordTests
    {
        [Fact]
        public void RecordWinAndLoss_UpdateCountsAndTotals()
        {
            IGameRecord record = new GameRecord();

            record.RecordWin();
            record.RecordWin();
            record.RecordLoss();

            Assert.Equal(2, record.TotalWins);
            Assert.Equal(1, record.TotalLosses);
            Assert.Equal(3, record.TotalGames);
        }

        [Fact]
        public void Instances_AreIndependent()
        {
            IGameRecord a = new GameRecord();
            IGameRecord b = new GameRecord();

            a.RecordWin();

            Assert.Equal(1, a.TotalWins);
            Assert.Equal(0, b.TotalWins);
        }

        [Fact]
        public void Restore_OverwritesCounts_AndClampsNegatives()
        {
            IGameRecord record = new GameRecord();
            record.RecordWin();

            record.Restore(wins: 5, losses: 3);
            Assert.Equal(5, record.TotalWins);
            Assert.Equal(3, record.TotalLosses);

            record.Restore(wins: -1, losses: -2);
            Assert.Equal(0, record.TotalWins);
            Assert.Equal(0, record.TotalLosses);
        }

        [Fact]
        public void GetRecordMessages_IncludesWinRate()
        {
            IGameRecord record = new GameRecord();
            record.RecordWin();
            record.RecordWin();
            record.RecordLoss();

            var messages = record.GetRecordMessages();

            Assert.Contains(messages, m => m.Text.Contains("Total Wins: 2"));
            Assert.Contains(messages, m => m.Text.Contains("Total Losses: 1"));
            Assert.Contains(messages, m => m.Text.Contains("Win Rate"));
        }
    }
}
