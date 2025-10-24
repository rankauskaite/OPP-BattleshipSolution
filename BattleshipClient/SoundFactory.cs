using System;
using System.IO;
using NAudio.Wave;

namespace BattleshipClient
{
    // Abstraktus kūrėjas (Creator)
    public interface ISoundFactory
    {
        ISound factoryMethod(HitType hitType);
        ISound factoryMethod(MusicType musicType);
        void Play(ISound sound);
        void StopBackground();
    }

    // Konkretus kūrėjas (ConcreteCreator) su pagrindine logika
    public class SoundFactory : ISoundFactory
    {
        private IWavePlayer? backgroundPlayer;
        private AudioFileReader? backgroundReader;
        private bool loopBackground = true;

        public ISound factoryMethod(HitType hitType)
        {
            return hitType switch
            {
                HitType.Hit => new Sound("Sounds/hit.wav"),
                HitType.Miss => new Sound("Sounds/miss.wav"),
                HitType.Explosion => new Sound("Sounds/explosion.wav"),
                _ => throw new ArgumentException("Unknown hit type")
            };
        }

        public ISound factoryMethod(MusicType musicType)
        {
            return musicType switch
            {
                MusicType.Background => new Sound("Sounds/background.wav", true),
                MusicType.GameStart => new Sound("Sounds/game_start.wav"),
                MusicType.GameEnd => new Sound("Sounds/game_end.wav"),
                _ => throw new ArgumentException("Unknown music type")
            };
        }

        public void Play(ISound sound)
        {
            if (sound.IsBackground)
                PlayBackground(sound.Path);
            else
                PlayEffect(sound.Path);
        }

        public void StopBackground()
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

        private void PlayBackground(string path)
        {
            try
            {
                StopBackground();

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

        private void PlayEffect(string path)
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
    }
}