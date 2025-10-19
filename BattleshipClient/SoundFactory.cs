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
        private static bool loopBackground = true;

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

            switch (type)
            {
                case MusicType.Background:
                    PlayBackground(path);
                    break;

                case MusicType.GameEnd:
                    StopBackground();
                    PlayEffect(path);
                    break;

                default:
                    PlayEffect(path);
                    break;
            }
        }

        private static void PlayBackground(string path)
        {
            try
            {
                StopBackground(); // sustabdom seną muziką

                backgroundReader = new AudioFileReader(path);
                backgroundPlayer = new WaveOutEvent();
                backgroundPlayer.Init(backgroundReader);
                loopBackground = true;

                backgroundPlayer.PlaybackStopped += (s, e) =>
                {
                    if (loopBackground && backgroundReader != null && backgroundPlayer != null)
                    {
                        backgroundReader.Position = 0;
                        backgroundPlayer.Play();
                    }
                };

                backgroundPlayer.Play();
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
                loopBackground = false;
                if (backgroundPlayer != null)
                {
                    backgroundPlayer.Stop();
                    backgroundPlayer.Dispose();
                    backgroundPlayer = null;
                }

                if (backgroundReader != null)
                {
                    backgroundReader.Dispose();
                    backgroundReader = null;
                }
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

        // 💾 Log failo išvalymas žaidimo pradžioje
        public static void ClearLogAtGameStart(string logFile)
        {
            try
            {
                if (File.Exists(logFile))
                {
                    File.WriteAllText(logFile, string.Empty); // išvalom failą
                    Console.WriteLine("Žaidimo log failas išvalytas naujam raundui.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Nepavyko išvalyti log failo: " + ex.Message);
            }
        }
    }
}
