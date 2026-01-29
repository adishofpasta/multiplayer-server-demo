using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MultiplayerServer.Tests
{
    public class TestClientConnection : IAsyncLifetime
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private TcpClient? _client;
        private NetworkStream? _stream;
        private float _currentX = 0f;
        private float _currentZ = 0f;
        private float _currentRotateY = 0f;
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;
        private readonly List<string> _receivedMessages = [];

        public async Task InitializeAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync("localhost", 7777);
                _stream = _client.GetStream();

                _cts = new CancellationTokenSource();
                _receiveTask = ListenForUpdatesAsync(_cts.Token);

                Console.WriteLine("[TEST] Connected to server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Connection failed: {ex.Message}");
            }
        }

        public async Task DisposeAsync()
        {
            _cts?.Cancel();
            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask;
                }
                catch (OperationCanceledException) { }
            }
            _stream?.Dispose();
            _client?.Close();
            Console.WriteLine("[TEST] Disconnected from server\n");
        }

        [Fact]
        public async Task SendMovement_UpdatesPlayerPosition()
        {
            _currentX = 5f;
            _currentZ = 10f;
            _currentRotateY = 45f;

            await SendMovementUpdateAsync();

            await Task.Delay(500);
            Assert.NotEmpty(_receivedMessages);
            Console.WriteLine("[TEST] Movement update sent successfully");
        }

        [Fact]
        public async Task SendMultipleUpdates_ReceivesWorldState()
        {
            var updates = new[]
            {
                (x: 1f, z: 2f, r: 0f),
                (x: 5f, z: 10f, r: 45f),
                (x: -3f, z: 7f, r: 90f),
            };

            foreach (var (x, z, r) in updates)
            {
                _currentX = x;
                _currentZ = z;
                _currentRotateY = r;

                await SendMovementUpdateAsync();
                await Task.Delay(300);
            }

            await Task.Delay(500);
            Assert.NotEmpty(_receivedMessages);
            Console.WriteLine("[TEST] Multiple updates sent successfully");
        }

        private async Task SendMovementUpdateAsync()
        {
            if (_client?.Connected != true || _stream == null)
            {
                Console.WriteLine("[TEST] Not connected to server");
                return;
            }

            try
            {
                var message = new
                {
                    type = "move",
                    moveX = _currentX,
                    moveZ = _currentZ,
                    rotateY = _currentRotateY
                };

                var json = JsonSerializer.Serialize(message, _jsonOptions);

                var data = Encoding.UTF8.GetBytes(json);
                await _stream.WriteAsync(data);

                Console.WriteLine($"[TEST] Sent: Position({_currentX:F2}, {_currentZ:F2}) Rotation({_currentRotateY:F2}°)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Error sending: {ex.Message}");
            }
        }

        private async Task ListenForUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[4096];

                while (_client?.Connected == true && !cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _stream!.ReadAsync(buffer, cancellationToken);

                    if (bytesRead == 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _receivedMessages.Add(json);

                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "worldState")
                        {
                            Console.WriteLine("[SERVER] World State:");
                            if (root.TryGetProperty("players", out var playersElement))
                            {
                                foreach (var player in playersElement.EnumerateArray())
                                {
                                    if (player.TryGetProperty("playerId", out var id) &&
                                        player.TryGetProperty("x", out var x) &&
                                        player.TryGetProperty("z", out var z) &&
                                        player.TryGetProperty("rotationY", out var rot))
                                    {
                                        Console.WriteLine($"  Player {id.GetInt32()}: ({x.GetSingle():F2}, {z.GetSingle():F2}) @ {rot.GetSingle():F2}°");
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException) { }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Error receiving: {ex.Message}");
            }
        }
    }
}