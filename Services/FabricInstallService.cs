using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;

namespace StoryLauncher.Services
{
    public sealed class FabricInstallService
    {
        public const string MinecraftVersion = "1.21.1";
        public const string FabricLoaderVersion = "0.19.3";

        public static string FabricVersionId =>
            $"fabric-loader-{FabricLoaderVersion}-{MinecraftVersion}";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressChanged;

        public bool IsFabricInstalled()
        {
            string versionJsonPath = Path.Combine(
                GamePathService.GameDirectory,
                "versions",
                FabricVersionId,
                $"{FabricVersionId}.json");

            return File.Exists(versionJsonPath);
        }

        public async Task InstallFabricAsync(
            CancellationToken cancellationToken = default)
        {
            GamePathService.CreateDirectories();

            EnsureMinecraftInstalled();

            StatusChanged?.Invoke(
                $"Получение Fabric Loader {FabricLoaderVersion}...");

            ProgressChanged?.Invoke(5);

            string profileUrl =
                $"https://meta.fabricmc.net/v2/versions/loader/" +
                $"{MinecraftVersion}/{FabricLoaderVersion}/profile/json";

            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    profileUrl,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Fabric Loader {FabricLoaderVersion} для " +
                    $"Minecraft {MinecraftVersion} не найден.\n\n" +
                    $"Код ответа: {(int)response.StatusCode} " +
                    response.ReasonPhrase);
            }

            string profileJson =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            string versionId = GetVersionId(profileJson);

            StatusChanged?.Invoke(
                $"Создание версии {versionId}...");

            ProgressChanged?.Invoke(20);

            string versionDirectory = Path.Combine(
                GamePathService.GameDirectory,
                "versions",
                versionId);

            Directory.CreateDirectory(versionDirectory);

            string versionJsonPath = Path.Combine(
                versionDirectory,
                $"{versionId}.json");

            await File.WriteAllTextAsync(
                versionJsonPath,
                profileJson,
                cancellationToken);

            StatusChanged?.Invoke(
                "Загрузка библиотек Fabric...");

            var minecraftPath =
                new MinecraftPath(GamePathService.GameDirectory);

            var launcher =
                new MinecraftLauncher(minecraftPath);

            launcher.FileProgressChanged += (_, e) =>
            {
                if (e.TotalTasks > 0)
                {
                    int progress = 20 + (int)Math.Round(
                        e.ProgressedTasks * 75.0 /
                        e.TotalTasks);

                    ProgressChanged?.Invoke(
                        Math.Clamp(progress, 20, 95));
                }

                if (!string.IsNullOrWhiteSpace(e.Name))
                {
                    StatusChanged?.Invoke(
                        $"Fabric: {e.Name}");
                }
            };

            launcher.ByteProgressChanged += (_, e) =>
            {
                if (e.TotalBytes > 0)
                {
                    int progress = 20 + (int)Math.Round(
                        e.ProgressedBytes * 75.0 /
                        e.TotalBytes);

                    ProgressChanged?.Invoke(
                        Math.Clamp(progress, 20, 95));
                }
            };

            await launcher.InstallAsync(versionId);

            cancellationToken.ThrowIfCancellationRequested();

            if (!IsFabricInstalled())
            {
                throw new InvalidOperationException(
                    "Установка Fabric завершилась, но файл версии не найден.");
            }

            ProgressChanged?.Invoke(100);

            StatusChanged?.Invoke(
                $"Fabric Loader {FabricLoaderVersion} установлен.");
        }

        private static string GetVersionId(string profileJson)
        {
            using JsonDocument document =
                JsonDocument.Parse(profileJson);

            if (!document.RootElement.TryGetProperty(
                    "id",
                    out JsonElement idElement))
            {
                throw new InvalidOperationException(
                    "Fabric вернул профиль без идентификатора версии.");
            }

            string? versionId = idElement.GetString();

            if (string.IsNullOrWhiteSpace(versionId))
            {
                throw new InvalidOperationException(
                    "Fabric вернул пустой идентификатор версии.");
            }

            return versionId;
        }

        private static void EnsureMinecraftInstalled()
        {
            string vanillaJsonPath = Path.Combine(
                GamePathService.GameDirectory,
                "versions",
                MinecraftVersion,
                $"{MinecraftVersion}.json");

            if (!File.Exists(vanillaJsonPath))
            {
                throw new InvalidOperationException(
                    "Сначала установи Minecraft 1.21.1.");
            }
        }
    }
}