using System;
using System.IO;
using NAudio.Wave;
using BattleshipClient.Factory;

namespace BattleshipClient.Services
{
    public class SoundService
    {
        private readonly ISoundFactory _factory;
        private IWavePlayer? _backgroundPlayer;
        private AudioFileReader? _backgroundReader;
        private bool _loopBackground = true;

        public SoundService(ISoundFactory factory)
        {
            _factory = factory;
        }

        public void PlayHit(HitType hitType)
        {
            var sound = _factory.CreateHitSound(hitType);
            Play(sound);
        }

        public void PlayMusic(MusicType musicType)
        {
            var sound = _factory.CreateMusicSound(musicType);
            Play(sound);
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
                _loopBackground = false;
                _backgroundPlayer?.Stop();
                _backgroundPlayer?.Dispose();
                _backgroundPlayer = null;

                _backgroundReader?.Dispose();
                _backgroundReader = null;
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

                _backgroundReader = new AudioFileReader(path);
                _backgroundPlayer = new WaveOutEvent();
                _backgroundPlayer.Init(_backgroundReader);
                _loopBackground = true;

                _backgroundPlayer.PlaybackStopped += (s, e) =>
                {
                    if (_loopBackground && _backgroundReader != null && _backgroundPlayer != null)
                    {
                        _backgroundReader.Position = 0;
                        _backgroundPlayer.Play();
                    }
                };

                _backgroundPlayer.Play();
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