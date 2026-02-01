using System.Text.Json.Serialization;

namespace MultiplayerServer.Net.Messages
{
    public class ClientMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } // Only movement for now.

        [JsonPropertyName("moveX")]
        public float MoveX { get; set; }

        [JsonPropertyName("moveZ")]
        public float MoveZ { get; set; }

        [JsonPropertyName("rotateY")]
        public float RotateY { get; set; }
    }
}