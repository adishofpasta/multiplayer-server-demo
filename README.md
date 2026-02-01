# Multiplayer Server Demo

A simple authoritative multiplayer game server built in C# with .NET.

Designed as a learning example and foundation for potential support of Unity-based multiplayer games (the design of which will likely require refactoring).\
Provides core networking patterns, socket communication, and server-side game logic.

------------------------------------------------------------------------

## Features

- TCP-based client-server communication
- JSON message serialization  
- User registration and authentication
- SQLite database with [EF Core](https://github.com/dotnet/efcore)
- Unit tests for core functionality

------------------------------------------------------------------------

## Project Structure
```
MultiplayerServer/        # Server implementation
MultiplayerServer.Tests/  # Unit tests
```

------------------------------------------------------------------------

## Getting Started

### Prerequisites
- .NET 8 SDK or higher (solution targets .NET 10)
- Visual Studio 2022 (or any C# IDE)

### Build and Run
```
git clone https://github.com/adishofpasta/multiplayer-server-demo
cd multiplayer-server-demo
dotnet build
dotnet run --project MultiplayerServer
```

The server will start on port 7777.

------------------------------------------------------------------------

## How It Works

**Server Architecture:**
- Repository pattern for data access
- Authoritative server model, with server-side logic to facilitate syncing and to hamper cheating
- JSON communication for packets to/from the client

**Client Flow:**
1. Connect to server
2. Register/login with credentials
3. Send movement input
4. Receive world state updates

------------------------------------------------------------------------

## Testing
```
dotnet test
```

A basic version of the database is provided.\
It contains pre-generated user credentials and statistics, for testing purposes.

------------------------------------------------------------------------

## Disclaimer

This is a demo project for learning purposes, not production-ready. Security and performance optimizations are intentionally simplified to keep the code clear and educational.
