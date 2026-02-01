using MultiplayerServer.Data.Models;

namespace MultiplayerServer.Data.Repositories
{
    public interface IPlayerRepository
    {
        Task<PlayerProfile?> GetPlayerByIdAsync(int id);
        Task<PlayerProfile?> GetPlayerByUsernameAsync(string username);
        Task<PlayerProfile?> GetPlayerByEmailAsync(string email);
        Task<List<PlayerProfile>> GetLeaderboardAsync(int top = 10);
        Task<PlayerProfile> CreatePlayerAsync(string username, string email, string passwordHash);
        Task UpdatePlayerAsync(PlayerProfile player);
        Task<bool> DeletePlayerAsync(int id);
        Task SaveChangesAsync();
    }
}