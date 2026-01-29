using System.Text.Json.Serialization;

namespace MultiplayerServer.Net.Messages
{
    public class ServerMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class PlayerStateMessage : ServerMessage
    {
        [JsonPropertyName("playerId")]
        public int PlayerId { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonPropertyName("rotationY")]
        public float RotationY { get; set; }
    }

    public class WorldStateMessage : ServerMessage
    {
        [JsonPropertyName("players")]
        public List<PlayerStateMessage> Players { get; set; } = new();
    }
}