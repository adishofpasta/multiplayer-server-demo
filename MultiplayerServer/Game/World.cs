using System.Collections.Concurrent;

namespace MultiplayerServer.Game
{
    public class World
    {
        private readonly ConcurrentDictionary<int, Player> _players = new();

        public IReadOnlyDictionary<int, Player> Players => _players;

        public Player AddPlayer(int playerId)
        {
            var player = new Player(playerId)
            {
                X = 0,
                Y = 0
            };
            _players[playerId] = player;

            return player;
        }

        public void RemovePlayer(int playerId)
        {
            _players.TryRemove(playerId, out _);
        }
    }
}
