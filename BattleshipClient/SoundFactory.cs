using System;
using System.IO;
using NAudio.Wave;

namespace BattleshipClient
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

    public static class SoundFactory
    {
        private static IWavePlayer? backgroundPlayer;
        private static AudioFileReader? backgroundReader;

        // 🎯 Garsiniai efektai
        public static void Play(HitType type)
        {
            string? path = type switch
            {
                HitType.Hit => "Sounds/hit.wav",
                HitType.Miss => "Sounds/miss.wav",
                HitType.Explosion => "Sounds/explosion.wav",
                _ => null
            };

            if (path != null)
                PlayEffect(path);
        }

        // 🎵 Muzika (fonas, pradžia, pabaiga)
        public static void Play(MusicType type)
        {
            string? path = type switch
            {
                MusicType.Background => "Sounds/background.wav",
                MusicType.GameStart => "Sounds/game_start.wav",
                MusicType.GameEnd => "Sounds/game_end.wav",
                _ => null
            };

            if (path == null) return;

            if (type == MusicType.Background)
                PlayBackground(path);
            else
                PlayEffect(path);
        }

        private static void PlayBackground(string path)
        {
            try
            {
                StopBackground();

                backgroundReader = new AudioFileReader(path);
                backgroundPlayer = new WaveOutEvent();
                backgroundPlayer.Init(backgroundReader);
                backgroundPlayer.Play();

                // Grojam cikle – kai baigiasi, grąžinam į pradžią
                backgroundPlayer.PlaybackStopped += (s, e) =>
                {
                    backgroundReader.Position = 0;
                    backgroundPlayer.Play();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to play background: " + ex.Message);
            }
        }

        public static void StopBackground()
        {
            try
            {
                backgroundPlayer?.Stop();
                backgroundPlayer?.Dispose();
                backgroundReader?.Dispose();
                backgroundPlayer = null;
                backgroundReader = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to stop background: " + ex.Message);
            }
        }

        private static void PlayEffect(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Sound file not found: {path}");
                    return;
                }

                var reader = new AudioFileReader(path);
                var player = new WaveOutEvent();
                player.Init(reader);
                player.Play();

                // automatiškai išvalom kai baigia groti
                player.PlaybackStopped += (s, e) =>
                {
                    player.Dispose();
                    reader.Dispose();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sound effect error: " + ex.Message);
            }
        }
    }
}
