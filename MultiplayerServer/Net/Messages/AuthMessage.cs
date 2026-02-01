using System.Text.Json.Serialization;

namespace MultiplayerServer.Net.Messages
{
    public class AuthMessage : ClientMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty; // "login" / "register"

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}