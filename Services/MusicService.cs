using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace StoryLauncher.Services
{
    public sealed record MusicTrackDefinition(
        string Id,
        string DisplayName,
        string Subtitle,
        string FileName);

    public static class MusicService
    {
        private static readonly MediaPlayer Player = new();

        private static readonly List<MusicTrackDefinition>
            TracksInternal = new()
            {
                new(
                    "MenuMusic",
                    "Story Menu",
                    "Основная музыка лаунчера",
                    "MenuMusic.mp3"),
                new(
                    "GoldenBrown",
                    "Golden Brown × Love Story",
                    "Атмосферная версия",
                    "Neizvesten_-_Golden_Brown_x_Love_Story_(SkySound.cc).mp3"),
                new(
                    "HideSlowed",
                    "Hide — slowed",
                    "CS01 Version",
                    "-_Hide_CS01_Version_slowed_(SkySound.cc).mp3"),
                new(
                    "PlayDate",
                    "Play Date",
                    "Melanie",
                    "Melanie_-_Play_date_(SkySound.cc).mp3")
            };

        private static bool _isInitialized;
        private static MusicTrackDefinition? _currentTrack;

        public static event Action? TrackChanged;

        public static IReadOnlyList<MusicTrackDefinition> Tracks =>
            TracksInternal;

        public static string CurrentTrackId =>
            _currentTrack?.Id ?? "MenuMusic";

        public static string CurrentTrackName =>
            _currentTrack?.DisplayName ?? "Story Menu";

        public static bool IsEnabled { get; private set; }

        public static double Volume { get; private set; }

        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            IsEnabled =
                SettingsService.Current.MusicEnabled;

            Volume = Math.Clamp(
                SettingsService.Current.MusicVolume,
                0.0,
                1.0);

            Player.Volume = Volume;

            Player.MediaEnded += (_, _) =>
            {
                if (IsEnabled)
                {
                    PlayNextTrack();
                }
            };

            _isInitialized = true;

            string savedTrackId =
                SettingsService.Current.SelectedMusicTrack;

            if (!SelectTrack(savedTrackId))
            {
                SelectTrack("MenuMusic");
            }
        }

        public static bool SelectTrack(
            string? trackId)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            MusicTrackDefinition track =
                TracksInternal.FirstOrDefault(
                    item => string.Equals(
                        item.Id,
                        trackId,
                        StringComparison.OrdinalIgnoreCase))
                ?? TracksInternal[0];

            string musicPath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Audio",
                track.FileName);

            if (!File.Exists(musicPath))
            {
                return false;
            }

            Player.Stop();
            Player.Close();

            Player.Open(new Uri(
                musicPath,
                UriKind.Absolute));

            Player.Volume = Volume;
            _currentTrack = track;

            SettingsService.Current.SelectedMusicTrack =
                track.Id;

            SettingsService.Save();

            if (IsEnabled)
            {
                Player.Play();
            }

            TrackChanged?.Invoke();

            return true;
        }

        public static void PlayNextTrack()
        {
            if (!_isInitialized ||
                TracksInternal.Count == 0)
            {
                return;
            }

            int currentIndex =
                _currentTrack == null
                    ? -1
                    : TracksInternal.FindIndex(
                        item => string.Equals(
                            item.Id,
                            _currentTrack.Id,
                            StringComparison.OrdinalIgnoreCase));

            for (int offset = 1;
                 offset <= TracksInternal.Count;
                 offset++)
            {
                int nextIndex =
                    (currentIndex + offset) %
                    TracksInternal.Count;

                if (SelectTrack(
                        TracksInternal[nextIndex].Id))
                {
                    return;
                }
            }

            Player.Position = TimeSpan.Zero;

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

        public static void Pause()
        {
            if (!_isInitialized)
            {
                return;
            }

            Player.Pause();
        }

        public static void Resume()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (IsEnabled)
            {
                Player.Play();
            }
        }
    }
}
