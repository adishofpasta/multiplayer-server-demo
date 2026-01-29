using MultiplayerServer.Core;
using MultiplayerServer.Game;
using MultiplayerServer.Net;
using MultiplayerServer.Net.Messages;

namespace MultiplayerServer
{
    /// <summary>
    /// The Server handles the connection between the Clients and the Game World.
    /// </summary>
    /// <remarks>
    /// This is a TCP server that connects on port 7777, and awaits for client connections.
    /// </remarks>
    public class Server
    {
        private readonly TcpServer _tcpServer;
        private readonly World _world;
        private readonly Loop _gameLoop;
        private readonly Dictionary<int, ClientConnection> _connections = [];

        /// <summary>
        /// Initializes a new instance of the Server class and prepares it to accept client connections.
        /// </summary>
        /// <remarks>Sets up the World and TCP instances, and registers the client connect/disconnect events.</remarks>
        public Server()
        {
            _world = new World();
            _gameLoop = new Loop(_world);
            _gameLoop.OnTick += BroadcastWorldState;
            
            _tcpServer = new TcpServer();
            _tcpServer.ClientConnected += OnClientConnected;
            _tcpServer.ClientDisconnected += OnClientDisconnected;
        }

        public void Start()
        {
            Console.WriteLine("Server starting...");
            _gameLoop.Start();
            _tcpServer.Start(7777);
        }

        public void Stop()
        {
            Console.WriteLine("Server stopping...");
            _gameLoop.Stop();
            _tcpServer.Stop();
        }

        private void OnClientConnected(ClientConnection connection)
        {
            var player = _world.AddPlayer(connection.PlayerId);
            _connections[connection.PlayerId] = connection;
            
            connection.MessageReceived += (conn, msg) => OnPlayerInput(player.Id, msg);

            Console.WriteLine($"Player {connection.PlayerId} connected [spawn: X {player.X} Y {player.Y} Z {player.Z}]");
        }

        private void OnClientDisconnected(ClientConnection connection)
        {
            _world.RemovePlayer(connection.PlayerId);
            _connections.Remove(connection.PlayerId);

            Console.WriteLine($"Player {connection.PlayerId} disconnected.");
        }

        private void OnPlayerInput(int playerId, string json)
        {
            var message = MessageParser.Parse(json);
            if (message == null) return;

            if (_world.Players.TryGetValue(playerId, out var player))
            {
                player.MoveX = message.MoveX;
                player.MoveZ = message.MoveZ;
                player.RotateY = message.RotateY;
            }
        }

        private void BroadcastWorldState()
        {
            var worldState = new WorldStateMessage
            {
                Type = "worldState",
                Players = [.. _world.Players.Values
                    .Select(p => new PlayerStateMessage
                    {
                        Type = "playerState",
                        PlayerId = p.Id,
                        X = p.X,
                        Y = p.Y,
                        Z = p.Z,
                        RotationY = p.RotationY
                    })]
            };

            var json = MessageParser.Serialize(worldState);

            foreach (var connection in _connections.Values)
            {
                _ = connection.SendPacket(json);
            }
        }
    }
}
