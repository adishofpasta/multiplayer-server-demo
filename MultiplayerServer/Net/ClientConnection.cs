using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MultiplayerServer.Net
{
    public class ClientConnection
    {
        public int PlayerId { get; }

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();

        public event Func<ClientConnection, string, Task>? MessageReceived;
        public event Action<ClientConnection>? Disconnected;

        public ClientConnection(TcpClient client, int playerId)
        {
            _client = client;
            _stream = _client.GetStream();
            _stream.ReadTimeout = 5000;

            PlayerId = playerId;

            _ = ReadPacket(_cts.Token);
        }

        private async Task ReadPacket(CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);

                while (!cancellationToken.IsCancellationRequested)
                {
                    string? line;
                    try
                    {
                        line = await reader.ReadLineAsync(cancellationToken);
                    }
                    // Server disconnected the client
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // Client closed
                    if (line == null) break;

                    Console.WriteLine($"[ClientConnection::ReadPacket] Received from Player {PlayerId}: {line}");

                    // MessageReceived is OnPlayerInput.
                    var handler = MessageReceived;
                    if (handler != null)
                    {
                        await handler(this, line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientConnection::ReadPacket] Error reading from Player {PlayerId}: {ex.Message}");
            }
            finally
            {
                _client.Close();
                Disconnected?.Invoke(this);
            }
        }

        public async Task SendPacket(string message)
        {
            if (_client.Connected)
            {
                var data = Encoding.UTF8.GetBytes(message + "\n");
                await _stream.WriteAsync(data);
            }
        }

        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();

            _client.Dispose();
        }
    }
}
