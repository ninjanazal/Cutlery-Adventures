namespace Cutlery.Com
{
    public enum PacketType
    {
        // Data sent to the client (Connection)
        RequestPlayerInfo, RegistationOK, NewPlayer,
        // Data sent to the server (Connection)
        PlayerInfo, PlayerState,
        // sent to client Setting up the match (pre-Match)
        CountDown, GameStart,
        // sent both ways
        PlayerPosition, PlayerAction,
        // sent both ways
        PlayerScore, ResetPlayerPosition,
        // types for objects in game, NPCs
        SetObjPosition, DestroyObj,
        GameEnd, RematchGame,
        // sent to players
        ConnectionRefused
    }
}