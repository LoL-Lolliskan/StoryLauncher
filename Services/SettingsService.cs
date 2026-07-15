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

        // Профиль
        public string Nickname { get; set; } = "Player";

        // Minecraft
        public int AllocatedRamMb { get; set; } = 6144;

        // Java
        public string JavaPath { get; set; } =
            @"C:\Program Files\Eclipse Adoptium\jdk-21";
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