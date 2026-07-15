using System;
using System.IO;
using System.Windows.Media;

namespace StoryLauncher.Services
{
    public static class MusicService
    {
        private static readonly MediaPlayer Player = new();
        private static bool _isInitialized;

        public static bool IsEnabled { get; private set; }

        public static double Volume { get; private set; }

        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            SettingsService.Load();

            IsEnabled = SettingsService.Current.MusicEnabled;

            Volume = Math.Clamp(
                SettingsService.Current.MusicVolume,
                0.0,
                1.0);

            string musicPath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Audio",
                "MenuMusic.mp3");

            if (!File.Exists(musicPath))
            {
                return;
            }

            Player.Open(new Uri(
                musicPath,
                UriKind.Absolute));

            Player.Volume = Volume;

            Player.MediaEnded += (_, _) =>
            {
                Player.Position = TimeSpan.Zero;

                if (IsEnabled)
                {
                    Player.Play();
                }
            };

            _isInitialized = true;

            if (IsEnabled)
            {
                Player.Play();
            }
        }

        public static void Toggle()
        {
            SetEnabled(!IsEnabled);
        }

        public static void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            SettingsService.Current.MusicEnabled =
                enabled;

            SettingsService.Save();

            if (!_isInitialized)
            {
                Initialize();
            }

            if (!_isInitialized)
            {
                return;
            }

            if (IsEnabled)
            {
                Player.Play();
            }
            else
            {
                Player.Pause();
            }
        }

        public static void SetVolume(double volume)
        {
            Volume = Math.Clamp(
                volume,
                0.0,
                1.0);

            SettingsService.Current.MusicVolume =
                Volume;

            SettingsService.Save();

            if (_isInitialized)
            {
                Player.Volume = Volume;
            }
        }

        // ==========================
        // НОВЫЕ МЕТОДЫ
        // ==========================

        public static void Pause()
        {
            if (!_isInitialized)
                return;

            Player.Pause();
        }

        public static void Resume()
        {
            if (!_isInitialized)
                return;

            if (IsEnabled)
            {
                Player.Play();
            }
        }
    }
}