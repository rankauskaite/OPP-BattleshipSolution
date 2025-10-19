namespace BattleshipClient.Observers
{
    public interface IGameObserver
    {
        void OnGameEvent(string eventType, string playerName, object? data = null);
    }
}
