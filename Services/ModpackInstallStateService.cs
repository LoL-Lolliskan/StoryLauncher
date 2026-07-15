using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StoryLauncher.Services
{
    public sealed class InstalledModpackPackage
    {
        public string AssetName { get; set; } =
            string.Empty;

        public string Sha256 { get; set; } =
            string.Empty;

        public DateTime InstalledAtUtc { get; set; }
    }

    public sealed class ModpackInstallState
    {
        public string InstalledVersion { get; set; } =
            "0.0.0";

        public Dictionary<string, InstalledModpackPackage> Packages
        {
            get;
            set;
        } = new(
            StringComparer.OrdinalIgnoreCase);
    }

    public static class ModpackInstallStateService
    {
        private static readonly string StateFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "StoryLauncher");

        private static readonly string StateFile =
            Path.Combine(
                StateFolder,
                "modpack-install-state.json");

        public static ModpackInstallState Current
        {
            get;
            private set;
        } = new();

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(
                    StateFolder);

                if (!File.Exists(StateFile))
                {
                    Current =
                        new ModpackInstallState();

                    Save();
                    return;
                }

                string json =
                    File.ReadAllText(
                        StateFile);

                ModpackInstallState? loaded =
                    JsonSerializer.Deserialize<ModpackInstallState>(
                        json);

                Current =
                    loaded ??
                    new ModpackInstallState();

                Current.Packages ??=
                    new Dictionary<string, InstalledModpackPackage>(
                        StringComparer.OrdinalIgnoreCase);

                Current.Packages =
                    new Dictionary<string, InstalledModpackPackage>(
                        Current.Packages,
                        StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                Current =
                    new ModpackInstallState();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(
                    StateFolder);

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
                // Игнорируем ошибки сохранения.
            }
        }

        public static bool IsPackageCurrent(
            string assetName,
            string sha256)
        {
            if (string.IsNullOrWhiteSpace(assetName) ||
                string.IsNullOrWhiteSpace(sha256))
            {
                return false;
            }

            if (!Current.Packages.TryGetValue(
                    assetName,
                    out InstalledModpackPackage? installed))
            {
                return false;
            }

            return string.Equals(
                installed.Sha256,
                sha256,
                StringComparison.OrdinalIgnoreCase);
        }

        public static void MarkPackageInstalled(
            ModpackReleaseFile file)
        {
            if (file == null ||
                string.IsNullOrWhiteSpace(file.AssetName))
            {
                return;
            }

            Current.Packages[file.AssetName] =
                new InstalledModpackPackage
                {
                    AssetName =
                        file.AssetName,

                    Sha256 =
                        file.Sha256,

                    InstalledAtUtc =
                        DateTime.UtcNow
                };

            Save();
        }

        public static void RemovePackage(
            string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return;
            }

            if (Current.Packages.Remove(assetName))
            {
                Save();
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