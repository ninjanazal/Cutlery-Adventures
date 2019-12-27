namespace Cutlery.Com
{
    public enum PacketType
    {
        RequestPlayerInfo, RegistationOK,    // Data sent to the client (Connection)
        PlayerInfo,                          // Data sent to the server (Connection)  
        CountDown, GameStart,                // sent to client Setting um the match (pre-Match)


    }
}