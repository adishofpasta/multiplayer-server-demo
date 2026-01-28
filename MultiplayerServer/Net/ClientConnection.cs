using System.Net.Sockets;
using System.Text;

namespace MultiplayerServer.Net
{
    public class ClientConnection
    {
        public int PlayerId { get; }

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        public event Action<ClientConnection>? Disconnected;
        public event Action<ClientConnection, string>? MessageReceived;

        public ClientConnection(TcpClient client, int playerId)
        {
            _client = client;
            _stream = _client.GetStream();
            _stream.ReadTimeout = 5000; // 5 seconds timeout

            PlayerId = playerId;

            _ = ReadPacket();
        }

        private async Task ReadPacket()
        {
            try
            {
                var buffer = new byte[1024];

                while (_client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer);

                    // Connection is closed.
                    if (bytesRead == 0) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received from Player {PlayerId}: {message}");
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from Player {PlayerId}: {ex.Message}");
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
                var data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data);
            }
        }
    }
}
