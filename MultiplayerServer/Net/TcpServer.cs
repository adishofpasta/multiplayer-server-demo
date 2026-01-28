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

            _listener?.Stop();

            Console.WriteLine("TCP Server stopped.");
        }

        private async Task AcceptClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_running && !cancellationToken.IsCancellationRequested)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                    var clientConnection = new ClientConnection(tcpClient, _nextPlayerId++);

                    ClientConnected?.Invoke(clientConnection);
                    clientConnection.Disconnected += c =>
                    {
                        ClientDisconnected?.Invoke(c);
                    };

                    Console.WriteLine($"Client connected with PlayerId: {clientConnection.PlayerId}");
                }
            }
            // Ignore exception caused by stopping the listener
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }

            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }
}