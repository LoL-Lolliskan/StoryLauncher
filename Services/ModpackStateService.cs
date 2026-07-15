using System;
using System.IO;
using System.Text.Json;

namespace StoryLauncher.Services
{
    public sealed class ModpackState
    {
        public string InstalledVersion { get; set; } = "0.0.0";
    }

    public static class ModpackStateService
    {
        private static readonly string StateFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "StoryLauncher");

        private static readonly string StateFile =
            Path.Combine(
                StateFolder,
                "modpack-state.json");

        public static ModpackState Current { get; private set; } =
            new ModpackState();

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(StateFolder);

                if (!File.Exists(StateFile))
                {
                    Current = new ModpackState();
                    Save();
                    return;
                }

                string json =
                    File.ReadAllText(StateFile);

                ModpackState? loaded =
                    JsonSerializer.Deserialize<ModpackState>(
                        json);

                Current =
                    loaded ?? new ModpackState();
            }
            catch
            {
                Current = new ModpackState();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(StateFolder);

                string json =
                    JsonSerializer.Serialize(
                        Current,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                File.WriteAllText(
                    StateFile,
                    json);
            }
            catch
            {
                // Пока просто не прерываем работу лаунчера.
            }
        }

        public static void SetInstalledVersion(
            string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return;
            }

            Current.InstalledVersion =
                version.Trim();

            Save();
        }
    }
}