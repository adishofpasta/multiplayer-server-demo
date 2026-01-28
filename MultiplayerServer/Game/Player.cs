namespace MultiplayerServer.Game
{
    public class Player(int id)
    {
        public int Id { get; } = id;
        public float X { get; set; }
        public float Y { get; set; }
    }
}
