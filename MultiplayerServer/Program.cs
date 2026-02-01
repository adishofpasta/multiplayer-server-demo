using MultiplayerServer;
using MultiplayerServer.Data;
using MultiplayerServer.Data.Repositories;
using MultiplayerServer.Data.Services;
using Microsoft.EntityFrameworkCore;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting server...");

        var context = new GameDbContext();
        context.Database.Migrate();

        var authService = new PlayerAuthService(new PlayerRepository(context));

        var server = new Server(authService);
        try
        {
            server.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("Press ENTER to stop.");
        Console.ReadLine();

        server.Stop();
    }
}
