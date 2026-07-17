using System;
using System.IO;
using System.Text.Json;

namespace StoryLauncher.Services
{
    public sealed class LauncherSettings
    {
        // Музыка
        public double MusicVolume { get; set; } = 0.35;
        public bool MusicEnabled { get; set; } = true;
        public string SelectedMusicTrack { get; set; } =
            "MenuMusic";

        // Профиль
        public string Nickname { get; set; } = "Player";

        // Своя аватарка профиля. Если false, используется голова скина.
        public bool UseCustomAvatar { get; set; }

        public string CustomAvatarPath { get; set; } = string.Empty;

        // Свой PNG-скин используется для предпросмотра локального профиля.
        public bool UseCustomSkin { get; set; }

        public string CustomSkinPath { get; set; } = string.Empty;

        // Minecraft
        public int AllocatedRamMb { get; set; } = 6144;

        // Java
        public string JavaPath { get; set; } =
            @"C:\Program Files\Eclipse Adoptium\jdk-21";

        // Оформление лаунчера
        public string LauncherTheme { get; set; } =
            "StoryPurple";

        // Видео-аватарка в левом верхнем углу.
        public bool AnimationsEnabled { get; set; } = true;
    }

    public static class SettingsService
    {
        private static readonly string SettingsFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "StoryLauncher");

        private static readonly string SettingsFile =
            Path.Combine(
                SettingsFolder,
                "settings.json");

        public static LauncherSettings Current
        {
            get;
            private set;
        } = new LauncherSettings();

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);

                if (!File.Exists(SettingsFile))
                {
                    Current = new LauncherSettings();

                    Save();

                    return;
                }

                string json =
                    File.ReadAllText(SettingsFile);

                LauncherSettings? settings =
                    JsonSerializer.Deserialize<LauncherSettings>(
                        json);

                if (settings != null)
                {
                    Current = settings;
                }
                else
                {
                    Current = new LauncherSettings();
                }
            }
            catch
            {
                Current = new LauncherSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);

                string json =
                    JsonSerializer.Serialize(
                        Current,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                File.WriteAllText(
                    SettingsFile,
                    json);
            }
            catch
            {
                // Пока игнорируем ошибки сохранения.
            }
        }
    }
}
