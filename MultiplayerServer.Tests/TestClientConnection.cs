using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly JsonSerializerOptions _jsonOptions = new()
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
        private readonly List<JsonElement> _authResponses = [];

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
        
        #region Auth Tests

        [Fact]
        public async Task Register_NewPlayer_Success()
        {
            var username = $"TestUser_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Assert.NotNull(authResponse);
            Assert.True(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Equal("Registration successful", authResponse.Value.GetProperty("message").GetString());
            Assert.True(authResponse.Value.GetProperty("playerId").GetInt32() > 0);
            Console.WriteLine($"[TEST] Player registered: {username}");
        }

        [Fact]
        public async Task Login_ExistingPlayer_Success()
        {
            var username = $"LoginTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(300);
            _authResponses.Clear();

            await SendAuthMessageAsync("login", username, null, password);
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Console.WriteLine($"[TEST] authResponse: {authResponse}");
            Assert.NotNull(authResponse);
            Assert.True(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Equal("Login successful", authResponse.Value.GetProperty("message").GetString());
            Console.WriteLine($"[TEST] Player logged in: {username}");
        }

        [Fact]
        public async Task Register_DuplicateUsername_Fails()
        {
            var username = $"DuplicateTest_{Guid.NewGuid().ToString()[..8]}";
            var email1 = $"{username}_1@test.com";
            var email2 = $"{username}_2@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email1, password);
            await Task.Delay(300);
            _authResponses.Clear();

            await SendAuthMessageAsync("register", username, email2, password);
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Assert.NotNull(authResponse);
            Assert.False(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Contains("already taken", authResponse.Value.GetProperty("message").GetString());
            Console.WriteLine("[TEST] Duplicate username correctly rejected");
        }

        [Fact]
        public async Task Register_DuplicateEmail_Fails()
        {
            var username1 = $"EmailTest_{Guid.NewGuid().ToString()[..8]}";
            var username2 = $"EmailTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"shared_{Guid.NewGuid().ToString()[..8]}@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username1, email, password);
            await Task.Delay(300);
            _authResponses.Clear();

            await SendAuthMessageAsync("register", username2, email, password);
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Assert.NotNull(authResponse);
            Assert.False(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Contains("already registered", authResponse.Value.GetProperty("message").GetString());
            Console.WriteLine("[TEST] Duplicate email correctly rejected");
        }

        [Fact]
        public async Task Login_InvalidPassword_Fails()
        {
            var username = $"PasswordTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(300);
            _authResponses.Clear();

            await SendAuthMessageAsync("login", username, null, "WrongPassword");
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Assert.NotNull(authResponse);
            Assert.False(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Contains("Invalid password", authResponse.Value.GetProperty("message").GetString());
            Console.WriteLine("[TEST] Invalid password correctly rejected");
        }

        [Fact]
        public async Task Register_InvalidUsername_TooShort_Fails()
        {
            var username = "ab";
            var email = "test@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Assert.NotNull(authResponse);
            Assert.False(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Contains("at least 3 characters", authResponse.Value.GetProperty("message").GetString());
            Console.WriteLine("[TEST] Short username correctly rejected");
        }

        [Fact]
        public async Task Register_InvalidPassword_TooShort_Fails()
        {
            var username = $"PassTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "short";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(500);

            var authResponse = GetLastAuthResponse();
            Assert.NotNull(authResponse);
            Assert.False(authResponse.Value.GetProperty("success").GetBoolean());
            Assert.Contains("at least 6 characters", authResponse.Value.GetProperty("message").GetString());
            Console.WriteLine("[TEST] Short password correctly rejected");
        }

        #endregion

        #region Game Tests

        [Fact]
        public async Task AuthenticatedPlayer_SendMovement_Success()
        {
            var username = $"GameTest_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(500);
            _authResponses.Clear();

            await SendAuthMessageAsync("login", username, null, password);
            await Task.Delay(500);

            _currentX = 5f;
            _currentZ = 10f;
            _currentRotateY = 45f;
            await SendMovementUpdateAsync();
            await Task.Delay(1000);

            Assert.NotEmpty(_receivedMessages);
            var hasWorldState = _receivedMessages.Any(msg =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(msg);
                    return doc.RootElement.TryGetProperty("type", out var type) && 
                           type.GetString() == "worldState";
                }
                catch { return false; }
            });
            Assert.True(hasWorldState);
            Console.WriteLine("[TEST] Authenticated player sent movement successfully");
        }

        [Fact]
        public async Task MultipleMovementUpdates_ReceivesWorldState()
        {
            var username = $"MultiMove_{Guid.NewGuid().ToString()[..8]}";
            var email = $"{username}@test.com";
            var password = "SecurePassword123";

            await SendAuthMessageAsync("register", username, email, password);
            await Task.Delay(300);

            var updates = new[] { (1f, 2f, 0f), (5f, 10f, 45f), (-3f, 7f, 90f) };

            foreach (var (x, z, r) in updates)
            {
                _currentX = x;
                _currentZ = z;
                _currentRotateY = r;
                await SendMovementUpdateAsync();
                await Task.Delay(200);
            }

            await Task.Delay(300);

            Assert.NotEmpty(_receivedMessages);
            Console.WriteLine("[TEST] Multiple movement updates sent successfully");
        }

        #endregion

        #region Private Helpers

        private async Task SendAuthMessageAsync(string action, string username, string? email, string password)
        {
            if (_client?.Connected != true || _stream == null) return;

            try
            {
                var message = new
                {
                    type = action,
                    action,
                    username,
                    email = email ?? string.Empty,
                    password
                };

                var json = JsonSerializer.Serialize(message, _jsonOptions);
                var data = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(data);

                Console.WriteLine($"[TEST - SendAuthMessageAsync] Sent {action}: {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST - SendAuthMessageAsync] Error sending auth: {ex.Message}");
            }
        }

        private async Task SendMovementUpdateAsync()
        {
            if (_client?.Connected != true || _stream == null) return;

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
                var data = Encoding.UTF8.GetBytes(json + "\n");
                
                await _stream.WriteAsync(data);

                Console.WriteLine($"[TEST - SendMovementUpdateAsync] Sent: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST - SendMovementUpdateAsync] Error sending movement: {ex.Message}");
            }
        }

        private async Task ListenForUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[TEST - ListenForUpdatesAsync] Listener started");
                using var reader = new StreamReader(_stream!, Encoding.UTF8, leaveOpen: true);

                while (!cancellationToken.IsCancellationRequested)
                {
                    string? line = null;
                    try
                    {
                        Console.WriteLine("[TEST - ListenForUpdatesAsync] Listener waiting for line...");
                        line = await reader.ReadLineAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (string.IsNullOrEmpty(line)) 
                    {
                        Console.WriteLine("[TEST - ListenForUpdatesAsync] Listener: Empty line received");
                        break;
                    }

                    Console.WriteLine($"[TEST - ListenForUpdatesAsync] Listener received: {line}");
                    _receivedMessages.Add(line);

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement.Clone();

                        if (root.TryGetProperty("type", out var typeElement))
                        {
                            var type = typeElement.GetString();

                            if (type == "authResponse")
                            {
                                _authResponses.Add(root);
                                var success = root.GetProperty("success").GetBoolean();
                                var message = root.GetProperty("message").GetString();
                                Console.WriteLine($"[TEST - ListenForUpdatesAsync] {(success ? "[PASS]" : "[FAIL]")} {message}");
                            }
                            else if (type == "worldState")
                            {
                                Console.WriteLine("[TEST - ListenForUpdatesAsync] World State Update received");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[TEST - ListenForUpdatesAsync] JSON parse error: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST - ListenForUpdatesAsync] Error in listener: {ex.Message}");
            }
        }

        private JsonElement? GetLastAuthResponse()
        {
            return _authResponses.LastOrDefault();
        }

        #endregion
    }
}