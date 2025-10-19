namespace BattleshipClient.Observers
{
    public class SoundObserver : IGameObserver
    {
        public void OnGameEvent(string eventType, string playerName, object? data = null)
        {
            switch (eventType)
            {
                case "HIT":
                    SoundFactory.Play(HitType.Hit);
                    break;
                case "MISS":
                    SoundFactory.Play(HitType.Miss);
                    break;
                case "EXPLOSION":
                    SoundFactory.Play(HitType.Explosion);
                    break;
                case "WIN":
                case "LOSE":
                    SoundFactory.Play(MusicType.GameEnd);
                    break;
            }
        }
    }
}
