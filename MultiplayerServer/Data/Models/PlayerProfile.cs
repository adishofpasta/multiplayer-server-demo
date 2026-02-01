namespace MultiplayerServer.Data.Models
{
    public class PlayerProfile
    {
        public int Id { get; set; }
        
        public string Username { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string PasswordHash { get; set; } = string.Empty;
        
        public int Kills { get; set; }
        
        public int Deaths { get; set; }
        
        public float WinRate { get; set; }
        
        public int Level { get; set; }
        
        public long TotalPlayTime { get; set; } // in milliseconds
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime LastLogin { get; set; }
        
        public bool IsActive { get; set; } = true;

        public int GetRank() => Kills > 0 ? (int)((Kills / (float)(Kills + Deaths)) * 100) : 0;
    }
}