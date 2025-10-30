using System;

namespace BattleshipClient.Factory
{
    public interface ISoundFactory
    {
        ISound CreateHitSound(HitType hitType);
        ISound CreateMusicSound(MusicType musicType);
    }

    public class SoundFactory : ISoundFactory
    {
        public ISound CreateHitSound(HitType hitType)
        {
            return hitType switch
            {
                HitType.Hit => new Sound("Sounds/hit.wav"),
                HitType.Miss => new Sound("Sounds/miss.wav"),
                HitType.Explosion => new Sound("Sounds/explosion.wav"),
                _ => throw new ArgumentException("Unknown hit type")
            };
        }

        public ISound CreateMusicSound(MusicType musicType)
        {
            return musicType switch
            {
                MusicType.Background => new Sound("Sounds/background.wav", true),
                MusicType.GameStart => new Sound("Sounds/game_start.wav"),
                MusicType.GameEnd => new Sound("Sounds/game_end.wav"),
                _ => throw new ArgumentException("Unknown music type")
            };
        }
    }
}
