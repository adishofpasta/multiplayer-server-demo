using MultiplayerServer.Core;
using MultiplayerServer.Game;
using MultiplayerServer.Net;
using MultiplayerServer.Net.Messages;
using MultiplayerServer.Data.Repositories;
using MultiplayerServer.Data.Services;
using System.Text.Json;

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
        private readonly PlayerAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the Server class and prepares it to accept client connections.
        /// </summary>
        /// <remarks>Sets up the World and TCP instances, and registers the client connect/disconnect events.</remarks>
        public Server(PlayerAuthService authService)
        {
            _authService = authService;
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
            Console.WriteLine($"[Server::OnClientConnected] Client connected, awaiting authentication. PlayerId: {connection.PlayerId}");
            connection.MessageReceived += OnPlayerInput;
        }

        private void OnClientDisconnected(ClientConnection connection)
        {
            _world.RemovePlayer(connection.PlayerId);
            _connections.Remove(connection.PlayerId);
            Console.WriteLine($"[Server::OnClientDisconnected] Player {connection.PlayerId} disconnected.");
        }

        private async Task OnPlayerInput(ClientConnection connection, string json)
        {
            try
            {
                var message = MessageParser.Parse(json);
                if (message == null) return;

                // AUTHENTICATION:
                // Register or Login.
                if (message.Type == "login" || message.Type == "register")
                {
                    Console.WriteLine($"[Server::OnPlayerInput] Player {connection.PlayerId} sent {message.Type}");
                    await HandleAuthMessage(connection, json);
                    return;
                }

                // Any other message will be invalid without a login authentication (and thus a reference in _connections).
                if (!_connections.ContainsKey(connection.PlayerId))
                {
                    Console.WriteLine($"[Server::OnPlayerInput] Player {connection.PlayerId} sent message without authentication");
                    return;
                }

                // Our only other client message is "move". That is why we don't check against Type.
                // With future expansions, a switch would become a more structured / clean approach.
                if (_world.Players.TryGetValue(connection.PlayerId, out var player))
                {
                    Console.WriteLine($"[Server::OnPlayerInput] Movement: X {message.MoveX} - Z {message.MoveZ} - RotY{message.RotateY}");
                    player.MoveX = message.MoveX;
                    player.MoveZ = message.MoveZ;
                    player.RotateY = message.RotateY;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server::OnPlayerInput] Error processing input: {ex.Message}");
            }
        }

        private async Task HandleAuthMessage(ClientConnection connection, string json)
        {
            try
            {
                // We have received an auth request from the client (ie register/login).
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var username = root.GetProperty("username").GetString() ?? string.Empty;
                var password = root.GetProperty("password").GetString() ?? string.Empty;
                var action = root.GetProperty("type").GetString() ?? "login";

                var (success, player, message) = action == "register" 
                    ? await _authService.RegisterPlayerAsync(username, root.GetProperty("email").GetString() ?? string.Empty, password)
                    : await _authService.LoginPlayerAsync(username, password);

                if (success && player != null)
                {
                    var gamePlayer = _world.AddPlayer(connection.PlayerId);
                    _connections[connection.PlayerId] = connection;

                    var response = new AuthResponseMessage
                    {
                        Type = "authResponse",
                        Success = true,
                        Message = message,  // "Login/registration successful"
                        PlayerId = player.Id,
                        Username = player.Username
                    };

                    var responseJson = MessageParser.Serialize(response);
                    // json display example
                    // Console.WriteLine($"[Server::HandleAuthMessage] Packet sent for auth: {responseJson}");
                    await connection.SendPacket(responseJson);

                    Console.WriteLine($"[Server::HandleAuthMessage] Player {player.Username} ({connection.PlayerId}) authenticated successfully");
                }
                else
                {
                    var response = new AuthResponseMessage
                    {
                        Type = "authResponse",
                        Success = false,
                        Message = message,
                        PlayerId = -1
                    };

                    var responseJson = MessageParser.Serialize(response);
                    await connection.SendPacket(responseJson);

                    Console.WriteLine($"[Server::HandleAuthMessage] Authentication failed: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server::HandleAuthMessage] Error handling auth: {ex.Message}");
            }
        }

        private void BroadcastWorldState()
        {
            // Update the players with the current world state. Currently only sends the player positions.
            // Runs on server update tick.

            //Console.WriteLine($"[DEBUG] BroadcastWorldState called. Connected players: {_connections.Count}");

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
