using System.Net.Sockets;

namespace MultiplayerServer.Net
{
    public class TcpServer
    {
        private TcpListener? _listener;
        private bool _running;
        private int _nextPlayerId = 1;

        private CancellationTokenSource? _cts;
        private Task? _acceptTask;

        public event Action<ClientConnection>? ClientConnected;
        public event Action<ClientConnection>? ClientDisconnected;

        // We keep a list of connected clients to manage their packet reading requirements.
        private readonly List<ClientConnection> _clients = [];

        public void Start(int port)
        {
            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _listener.Start();
            _running = true;

            Console.WriteLine($"TCP Server started on port {port}. Listening...");

            _cts = new CancellationTokenSource();
            _acceptTask = AcceptClientAsync(_cts.Token);
        }

        public void Stop()
        {
            _running = false;

            _cts?.Cancel();
            _acceptTask?.Wait(TimeSpan.FromSeconds(2));

            // Gracefully disconnect all clients.
            // Important due to CancellationToken being checked against, on ClientConnection::ReadPacket
            foreach (var client in _clients.ToArray())
            {
                client.Dispose();
            }
            _clients.Clear();

            _listener?.Stop();

            Console.WriteLine("TCP Server stopped.");
        }

        private async Task AcceptClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_running && !cancellationToken.IsCancellationRequested)
                {
                    if (_listener == null)
                        throw new InvalidOperationException("[TcpServer::AcceptClientAsync] Server running on unitialized listener.");

                    var tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                    var clientConnection = new ClientConnection(tcpClient, _nextPlayerId++);
                    _clients.Add(clientConnection);

                    ClientConnected?.Invoke(clientConnection);
                    clientConnection.Disconnected += c =>
                    {
                        _clients.Remove(c);
                        ClientDisconnected?.Invoke(c);
                    };

                    Console.WriteLine($"[TcpServer::AcceptClientAsync] Client connected with PlayerId: {clientConnection.PlayerId}");
                }
            }
            // Ignore exceptions caused by stopping the listener.
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }

            catch (Exception ex)
            {
                Console.WriteLine($"[TcpServer::AcceptClientAsync] Error accepting client: {ex.Message}");
            }
        }
    }
}