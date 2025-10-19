using System.Collections.Generic;

namespace BattleshipClient.Observers
{
    public class GameEventManager
    {
        private readonly List<IGameObserver> observers = new();

        public void Attach(IGameObserver observer) => observers.Add(observer);
        public void Detach(IGameObserver observer) => observers.Remove(observer);

        public void Notify(string eventType, string playerName, object? data = null)
        {
            foreach (var obs in observers)
                obs.OnGameEvent(eventType, playerName, data);
        }
    }
}
