using System.Text.Json;
using MultiplayerServer.Net.Messages;

namespace MultiplayerServer.Net
{
    public static class MessageParser
    {
        // Cached
        private static readonly JsonSerializerOptions ClientOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions ServerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static ClientMessage? Parse(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ClientMessage>(json, ClientOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing message: {ex.Message}");
                return null;
            }
        }

        public static string Serialize(ServerMessage message)
        {
            return JsonSerializer.Serialize(message, message.GetType(), ServerOptions);
        }
    }
}