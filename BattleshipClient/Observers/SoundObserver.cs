using BattleshipClient.Factory;
using BattleshipClient.Services;

namespace BattleshipClient.Observers
{
    public class SoundObserver : IGameObserver
    {
        private readonly SoundService _soundService;

        public SoundObserver(SoundService soundService)
        {
            _soundService = soundService;
        }

        public void OnGameEvent(string eventType, string playerName, object? data = null)
        {
            switch (eventType)
            {
                case "HIT":
                    _soundService.PlayHit(HitType.Hit);
                    break;
                case "MISS":
                    _soundService.PlayHit(HitType.Miss);
                    break;
                case "EXPLOSION":
                    _soundService.PlayHit(HitType.Explosion);
                    break;
                case "WIN":
                case "LOSE":
                    _soundService.PlayMusic(MusicType.GameEnd);
                    break;
            }
        }
    }
}