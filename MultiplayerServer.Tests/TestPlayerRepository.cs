using System;
using System.Threading.Tasks;
using MultiplayerServer.Data;
using MultiplayerServer.Data.Repositories;
using MultiplayerServer.Data.Services;
using Xunit;

namespace MultiplayerServer.Tests
{
    public class TestPlayerRepository
    {
        private readonly GameDbContext _context;
        private readonly PlayerRepository _repository;
        private readonly PlayerAuthService _authService;

        public TestPlayerRepository()
        {
            _context = new GameDbContext();
            _context.Database.EnsureCreated();
            _repository = new PlayerRepository(_context);
            _authService = new PlayerAuthService(_repository);
        }

        [Fact]
        public async Task CreatePlayer_SavesPlayerToDatabase()
        {
            var username = $"DbTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "TestPassword123";

            var (success, player, _) = await _authService.RegisterPlayerAsync(username, email, password);

            Assert.True(success);
            Assert.NotNull(player);
            Assert.Equal(username, player.Username);
            Assert.Equal(email, player.Email);
            Assert.True(player.Id > 0);
            Console.WriteLine($"[DB TEST] Player created: ID {player.Id}");
        }

        [Fact]
        public async Task GetPlayerById_ReturnsCorrectPlayer()
        {
            var username = $"GetTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var (_, player, _) = await _authService.RegisterPlayerAsync(username, email, "Password123");
            var playerId = player!.Id;

            var retrievedPlayer = await _repository.GetPlayerByIdAsync(playerId);

            Assert.NotNull(retrievedPlayer);
            Assert.Equal(username, retrievedPlayer.Username);
            Assert.Equal(email, retrievedPlayer.Email);
            Console.WriteLine($"[DB TEST] Player retrieved by ID: {retrievedPlayer.Username}");
        }

        [Fact]
        public async Task GetPlayerByUsername_ReturnsCorrectPlayer()
        {
            var username = $"UsernameTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            await _authService.RegisterPlayerAsync(username, email, "Password123");

            var player = await _repository.GetPlayerByUsernameAsync(username);

            Assert.NotNull(player);
            Assert.Equal(username, player.Username);
            Console.WriteLine($"[DB TEST] Player retrieved by username: {player.Username}");
        }

        [Fact]
        public async Task UpdatePlayer_UpdatesStatistics()
        {
            var username = $"StatsTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var (_, player, _) = await _authService.RegisterPlayerAsync(username, email, "Password123");

            player!.Kills = 25;
            player.Deaths = 10;
            player.Level = 5;
            await _repository.UpdatePlayerAsync(player);

            var updatedPlayer = await _repository.GetPlayerByIdAsync(player.Id);
            Assert.NotNull(updatedPlayer);
            Assert.Equal(25, updatedPlayer.Kills);
            Assert.Equal(10, updatedPlayer.Deaths);
            Assert.Equal(5, updatedPlayer.Level);
            Console.WriteLine($"[DB TEST] Player stats updated: {updatedPlayer.Kills}K/{updatedPlayer.Deaths}D");
        }

        [Fact]
        public async Task GetLeaderboard_ReturnsTopPlayers()
        {
            var players = new[] 
            { 
                (username: $"Leader1_{Guid.NewGuid().ToString()[..8]}", kills: 100, deaths: 20),
                (username: $"Leader2_{Guid.NewGuid().ToString()[..8]}", kills: 80, deaths: 25),
                (username: $"Leader3_{Guid.NewGuid().ToString()[..8]}", kills: 60, deaths: 30),
            };

            foreach (var (username, kills, deaths) in players)
            {
                var (_, player, _) = await _authService.RegisterPlayerAsync(username, $"{username}@test.com", "Password123");
                player!.Kills = kills;
                player.Deaths = deaths;
                await _repository.UpdatePlayerAsync(player);
                await Task.Delay(50); // For different timestamps
            }

            var leaderboard = await _repository.GetLeaderboardAsync(10);

            Assert.NotEmpty(leaderboard);
            Assert.True(leaderboard.Count <= 10);

            for (int i = 0; i < leaderboard.Count - 1; i++)
            {
                Assert.True(leaderboard[i].Kills >= leaderboard[i + 1].Kills);
            }

            Console.WriteLine($"[DB TEST] Leaderboard retrieved: {leaderboard.Count} players");

            foreach (var p in leaderboard)
            {
                Console.WriteLine($"  {p.Username}: {p.Kills}K/{p.Deaths}D");
            }
        }

        [Fact]
        public async Task PasswordHashing_DifferentFromPlaintext()
        {
            var password = "MySecurePassword123";

            var hashedPassword = PlayerAuthService.HashPassword(password);

            Assert.NotEqual(password, hashedPassword);
            Assert.True(PlayerAuthService.VerifyPassword(password, hashedPassword));
            Assert.False(PlayerAuthService.VerifyPassword("WrongPassword", hashedPassword));
            Console.WriteLine("[DB TEST] Password hashing works correctly");
        }

        [Fact]
        public async Task PlayerPersistence_DataSurvivesNewContext()
        {
            var username = $"PersistTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var (_, player, _) = await _authService.RegisterPlayerAsync(username, email, "Password123");
            var playerId = player!.Id;

            var newContext = new GameDbContext();
            var newRepository = new PlayerRepository(newContext);
            var persistedPlayer = await newRepository.GetPlayerByIdAsync(playerId);

            Assert.NotNull(persistedPlayer);
            Assert.Equal(username, persistedPlayer.Username);
            Assert.Equal(email, persistedPlayer.Email);
            Console.WriteLine("[DB TEST] Player data persisted across contexts");
        }
    }
}