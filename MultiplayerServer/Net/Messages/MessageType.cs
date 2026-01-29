namespace MultiplayerServer.Net.Messages
{
    public enum MessageType
    {
        Move,
        Rotate,
        StateRequest,
        PlayerState,
        WorldState,
        PlayerConnected,
        PlayerDisconnected
    }
}