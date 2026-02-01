using Microsoft.EntityFrameworkCore;
using MultiplayerServer.Data.Models;

namespace MultiplayerServer.Data.Repositories
{
    public class PlayerRepository(GameDbContext context) : IPlayerRepository
    {
        private readonly GameDbContext _context = context;

        public async Task<PlayerProfile?> GetPlayerByIdAsync(int id)
        {
            return await _context.PlayerProfiles.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PlayerProfile?> GetPlayerByUsernameAsync(string username)
        {
            return await _context.PlayerProfiles.FirstOrDefaultAsync(p => p.Username == username);
        }

        public async Task<PlayerProfile?> GetPlayerByEmailAsync(string email)
        {
            return await _context.PlayerProfiles.FirstOrDefaultAsync(p => p.Email == email);
        }

        public async Task<List<PlayerProfile>> GetLeaderboardAsync(int top = 10)
        {
            return await _context.PlayerProfiles
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Kills)
                .ThenBy(p => p.Deaths)
                .Take(top)
                .ToListAsync();
        }

        public async Task<PlayerProfile> CreatePlayerAsync(string username, string email, string passwordHash)
        {
            var player = new PlayerProfile
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Kills = 0,
                Deaths = 0,
                Level = 1,
                TotalPlayTime = 0
            };

            _context.PlayerProfiles.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        public async Task UpdatePlayerAsync(PlayerProfile player)
        {
            player.LastLogin = DateTime.UtcNow;
            _context.PlayerProfiles.Update(player);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            var player = await GetPlayerByIdAsync(id);
            if (player == null) return false;

            _context.PlayerProfiles.Remove(player);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}