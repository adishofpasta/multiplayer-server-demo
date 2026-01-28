using MultiplayerServer;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting server...");

        var server = new Server();
        server.Start();

        Console.WriteLine("Press ENTER to stop.");
        Console.ReadLine();

        server.Stop();
    }
}
