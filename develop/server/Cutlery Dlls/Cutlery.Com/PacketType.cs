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
        // start match
        StartMatch,
        // sent both ways
        PlayerPosition, PlayerAction,
        AddForceTO,
        // sent both ways
        PlayerScore, ResetPlayerPosition,
        // types for objects in game, NPCs
        SetObjPosition, DestroyObj, SetObjectRotation, SpawnObj,
        GameEnd, RematchGame,
        // sent to players
        ConnectionRefused
    }
}