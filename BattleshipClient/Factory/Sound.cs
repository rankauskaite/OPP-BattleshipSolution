using System;

namespace BattleshipClient.Factory
{
    public enum HitType
    {
        Hit,
        Miss,
        Explosion
    }

    public enum MusicType
    {
        Background,
        GameStart,
        GameEnd
    }

    // Abstrakti sąsaja (Product)
    public interface ISound
    {
        string Path { get; }
        bool IsBackground { get; }
    }

    // Konkretus produktas (ConcreteProduct) kaip duomenų konteineris
    public class Sound : ISound
    {
        public string Path { get; }
        public bool IsBackground { get; }

        public Sound(string path, bool isBackground = false)
        {
            Path = path;
            IsBackground = isBackground;
        }
    }
}