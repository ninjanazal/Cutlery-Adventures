namespace Cutlery.Com
{
    // player states to the server
    public enum GameState
    {
        Disconnected, Connecting, Connected, Sync,  // Setting the player to the server
        WaitPlayer, WaitingStart, CountDown,        // Setting payler on all others
        GameStarted, GameEnded                      // match started
    }
}