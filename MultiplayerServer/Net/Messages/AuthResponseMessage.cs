using System.Text.Json.Serialization;

namespace MultiplayerServer.Net.Messages
{
    public class AuthResponseMessage : ServerMessage
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("playerId")]
        public int PlayerId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }
}