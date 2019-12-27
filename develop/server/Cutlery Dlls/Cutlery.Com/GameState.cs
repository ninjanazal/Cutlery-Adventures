namespace Cutlery.Com
{
    // player states to the server
    public enum GameState
    {
        Disconnected, Connecting, Connected, Sync,  // Setting the player to the server
        WaitingStart, CountDown,                     // Setting payler on all others
        GameStarted, GameEnded                      // match started
    }
}