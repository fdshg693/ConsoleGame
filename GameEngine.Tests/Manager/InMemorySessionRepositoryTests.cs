using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Manager;
using Xunit;

namespace GameEngine.Tests.Manager
{
    /// <summary>
    /// フェーズ3で新設した <see cref="InMemorySessionRepository"/>（<see cref="ISessionRepository"/>）の検証。
    /// 保存/復元/削除に加え、注入したクロックで TTL 失効を決定的に再現する。
    /// </summary>
    public class InMemorySessionRepositoryTests
    {
        private static GameSessionState NewState(string id) => new GameSessionState
        {
            SessionId = id,
            PlayerName = "Hero",
            Player = new PlayerSaveData { PlayerName = "Hero" }
        };

        [Fact]
        public async Task SaveThenLoad_ReturnsSameSession()
        {
            ISessionRepository repo = new InMemorySessionRepository();
            var state = NewState("s1");

            await repo.SaveAsync(state);
            var loaded = await repo.LoadAsync("s1");

            Assert.NotNull(loaded);
            Assert.Equal("s1", loaded!.SessionId);
            Assert.Equal("Hero", loaded.PlayerName);
        }

        [Fact]
        public async Task Load_UnknownSession_ReturnsNull()
        {
            ISessionRepository repo = new InMemorySessionRepository();

            Assert.Null(await repo.LoadAsync("missing"));
        }

        [Fact]
        public async Task Save_SameId_Overwrites()
        {
            ISessionRepository repo = new InMemorySessionRepository();
            await repo.SaveAsync(NewState("s1"));

            var updated = NewState("s1");
            updated.TotalWins = 9;
            await repo.SaveAsync(updated);

            var loaded = await repo.LoadAsync("s1");
            Assert.Equal(9, loaded!.TotalWins);
        }

        [Fact]
        public async Task Delete_RemovesSession()
        {
            ISessionRepository repo = new InMemorySessionRepository();
            await repo.SaveAsync(NewState("s1"));

            Assert.True(await repo.DeleteAsync("s1"));
            Assert.Null(await repo.LoadAsync("s1"));
            Assert.False(await repo.DeleteAsync("s1"));
        }

        [Fact]
        public async Task Load_AfterTtlExpires_ReturnsNull()
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var clock = new MutableClock(now);
            ISessionRepository repo = new InMemorySessionRepository(
                ttl: TimeSpan.FromMinutes(30),
                clock: () => clock.Now);

            await repo.SaveAsync(NewState("s1"));

            // TTL 内なら取得できる
            clock.Now = now.AddMinutes(29);
            Assert.NotNull(await repo.LoadAsync("s1"));

            // TTL を過ぎると失効して null
            clock.Now = now.AddMinutes(31);
            Assert.Null(await repo.LoadAsync("s1"));
        }

        private sealed class MutableClock
        {
            public DateTime Now { get; set; }
            public MutableClock(DateTime now) => Now = now;
        }
    }
}
