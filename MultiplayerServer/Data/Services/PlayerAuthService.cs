using System;
using System.Security.Cryptography;
using System.Text;
using MultiplayerServer.Data.Models;
using MultiplayerServer.Data.Repositories;

namespace MultiplayerServer.Data.Services
{
    public class PlayerAuthService(IPlayerRepository playerRepository)
    {
        private readonly IPlayerRepository _playerRepository = playerRepository;

        public static string HashPassword(string password)
        {
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }

        public async Task<(bool Success, PlayerProfile? Player, string Message)> RegisterPlayerAsync(string username, string email, string password)
        {
            Console.WriteLine($"[AUTH] RegisterPlayerAsync called for: {username}");
            
            // Sanity checks

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                Console.WriteLine($"[AUTH] Invalid username: {username}");
                return (false, null, "Username must be at least 3 characters");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                Console.WriteLine($"[AUTH] Invalid email: {email}");
                return (false, null, "Invalid email format");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                Console.WriteLine($"[AUTH] Invalid password length");
                return (false, null, "Password must be at least 6 characters");
            }

            var existingUsername = await _playerRepository.GetPlayerByUsernameAsync(username);
            if (existingUsername != null)
            {
                Console.WriteLine($"[AUTH] Username already taken: {username}");
                return (false, null, "Username already taken");
            }

            var existingEmail = await _playerRepository.GetPlayerByEmailAsync(email);
            if (existingEmail != null)
            {
                Console.WriteLine($"[AUTH] Email already registered: {email}");
                return (false, null, "Email already registered");
            }

            // Create the new player and accept registration
            var passwordHash = HashPassword(password);
            var player = await _playerRepository.CreatePlayerAsync(username, email, passwordHash);
            Console.WriteLine($"[AUTH] Player registered successfully: {username} (ID: {player.Id})");

            return (true, player, "Registration successful");
        }

        public async Task<(bool Success, PlayerProfile? Player, string Message)> LoginPlayerAsync(string username, string password)
        {
            Console.WriteLine($"[AUTH] LoginPlayerAsync called for: {username}");
            
            // Validation

            var player = await _playerRepository.GetPlayerByUsernameAsync(username);
            if (player == null)
            {
                Console.WriteLine($"[AUTH] Player not found: {username}");
                return (false, null, "Player not found");
            }

            Console.WriteLine($"[AUTH] Player found: {username}, validating password...");

            if (!VerifyPassword(password, player.PasswordHash))
            {
                Console.WriteLine($"[AUTH] Invalid password for: {username}");
                return (false, null, "Invalid password");
            }

            // Update last login and accept the login request
            player.LastLogin = DateTime.UtcNow;
            await _playerRepository.UpdatePlayerAsync(player);
            Console.WriteLine($"[AUTH] Login successful: {username} (ID: {player.Id})");

            return (true, player, "Login successful");
        }
    }
}