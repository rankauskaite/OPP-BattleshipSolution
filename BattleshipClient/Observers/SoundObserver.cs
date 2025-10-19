namespace BattleshipClient.Observers
{
    public class SoundObserver : IGameObserver
    {
        public void OnGameEvent(string eventType, string playerName, object? data = null)
        {
            SoundFactory factory = new SoundFactory();
            switch (eventType)
            {
                case "HIT":
                    factory.Play(factory.factoryMethod(HitType.Hit));
                    break;
                case "MISS":
                    factory.Play(factory.factoryMethod(HitType.Miss));
                    break;
                case "EXPLOSION":
                    factory.Play(factory.factoryMethod(HitType.Explosion));
                    break;
                case "WIN":
                case "LOSE":
                    factory.Play(factory.factoryMethod(MusicType.GameEnd));
                    break;
            }
        }
    }
}
