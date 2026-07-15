using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StoryLauncher.Services
{
    public sealed class ModpackVersionInfo
    {
        public string Version { get; set; } =
            string.Empty;

        public string ManifestUrl { get; set; } =
            string.Empty;
    }

    public sealed class ModpackUpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }

        public string InstalledVersion { get; set; } =
            string.Empty;

        public string LatestVersion { get; set; } =
            string.Empty;

        public string ManifestUrl { get; set; } =
            string.Empty;

        public ModpackReleaseManifest? Manifest { get; set; }
    }

    public sealed class ModpackUpdateService
    {
        private const string VersionUrl =
            "https://github.com/" +
            "LoL-Lolliskan/StoryModpack/" +
            "releases/latest/download/version.json";

        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public ModpackUpdateService()
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromMinutes(10)
                };

            _httpClient
                .DefaultRequestHeaders
                .UserAgent
                .ParseAdd(
                    "StoryLauncher/1.0");
        }

        public async Task<ModpackUpdateCheckResult>
            CheckForUpdatesAsync(
                string installedVersion,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(installedVersion))
            {
                installedVersion = "0.0.0";
            }

            ModpackVersionInfo remoteVersion =
                await DownloadVersionInfoAsync(
                    cancellationToken);

            bool updateAvailable =
                !string.Equals(
                    installedVersion.Trim(),
                    remoteVersion.Version.Trim(),
                    StringComparison.OrdinalIgnoreCase);

            ModpackReleaseManifest? manifest =
                null;

            if (updateAvailable)
            {
                manifest =
                    await DownloadManifestAsync(
                        remoteVersion.ManifestUrl,
                        cancellationToken);
            }

            return new ModpackUpdateCheckResult
            {
                UpdateAvailable =
                    updateAvailable,

                InstalledVersion =
                    installedVersion,

                LatestVersion =
                    remoteVersion.Version,

                ManifestUrl =
                    remoteVersion.ManifestUrl,

                Manifest =
                    manifest
            };
        }

        public async Task<ModpackVersionInfo>
            DownloadVersionInfoAsync(
                CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    VersionUrl,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            ModpackVersionInfo? remoteVersion =
                JsonSerializer.Deserialize<ModpackVersionInfo>(
                    json,
                    _jsonOptions);

            if (remoteVersion == null)
            {
                throw new InvalidOperationException(
                    "Не удалось прочитать version.json модпака.");
            }

            if (string.IsNullOrWhiteSpace(
                    remoteVersion.Version))
            {
                throw new InvalidOperationException(
                    "В version.json отсутствует версия модпака.");
            }

            if (string.IsNullOrWhiteSpace(
                    remoteVersion.ManifestUrl))
            {
                throw new InvalidOperationException(
                    "В version.json отсутствует ссылка на manifest.");
            }

            return remoteVersion;
        }

        public async Task<ModpackReleaseManifest>
            DownloadManifestAsync(
                string manifestUrl,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(manifestUrl))
            {
                throw new ArgumentException(
                    "Ссылка на release-manifest.json не указана.",
                    nameof(manifestUrl));
            }

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    manifestUrl,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            ModpackReleaseManifest? manifest =
                JsonSerializer.Deserialize<ModpackReleaseManifest>(
                    json,
                    _jsonOptions);

            if (manifest == null)
            {
                throw new InvalidOperationException(
                    "Не удалось прочитать release-manifest.json.");
            }

            if (string.IsNullOrWhiteSpace(
                    manifest.Version))
            {
                throw new InvalidOperationException(
                    "В release-manifest.json отсутствует версия.");
            }

            if (manifest.Files == null ||
                manifest.Files.Count == 0)
            {
                throw new InvalidOperationException(
                    "В release-manifest.json отсутствует список файлов.");
            }

            return manifest;
        }

        public async Task<ModpackUpdatePlan>
            CreateUpdatePlanAsync(
                ModpackReleaseManifest manifest,
                string gameDirectory,
                CancellationToken cancellationToken = default)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(
                    nameof(manifest));
            }

            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                throw new ArgumentException(
                    "Папка игры не указана.",
                    nameof(gameDirectory));
            }

            Directory.CreateDirectory(
                gameDirectory);

            string fullGameDirectory =
                Path.GetFullPath(gameDirectory);

            var plan =
                new ModpackUpdatePlan
                {
                    InstalledVersion =
                        ModpackInstallStateService
                            .Current
                            .InstalledVersion,

                    LatestVersion =
                        manifest.Version
                };

            foreach (ModpackReleaseFile file
                     in manifest.Files)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (file == null ||
                    string.IsNullOrWhiteSpace(
                        file.AssetName))
                {
                    continue;
                }

                /*
                 * Архивы после установки распаковываются,
                 * поэтому самого ZIP-файла в папке игры
                 * может уже не быть.
                 *
                 * Их проверяем по сохранённому состоянию.
                 */
                if (string.Equals(
                        file.Action,
                        "extract",
                        StringComparison.OrdinalIgnoreCase))
                {
                    bool archiveIsCurrent =
                        ModpackInstallStateService
                            .IsPackageCurrent(
                                file.AssetName,
                                file.Sha256);

                    if (!archiveIsCurrent)
                    {
                        plan.FilesToDownload.Add(
                            new ModpackUpdatePlanItem
                            {
                                File = file,
                                Reason =
                                    "Архив отсутствует или был обновлён"
                            });
                    }

                    continue;
                }

                /*
                 * Обычные моды, ресурспаки и шейдеры
                 * проверяются прямо в папке игры.
                 */
                string destinationPath =
                    GetSafeDestinationPath(
                        fullGameDirectory,
                        file.InstallPath);

                if (!File.Exists(destinationPath))
                {
                    plan.FilesToDownload.Add(
                        new ModpackUpdatePlanItem
                        {
                            File = file,
                            Reason =
                                "Файл отсутствует"
                        });

                    continue;
                }

                string localSha256 =
                    await CalculateSha256Async(
                        destinationPath,
                        cancellationToken);

                bool hashMatches =
                    string.Equals(
                        localSha256,
                        file.Sha256,
                        StringComparison.OrdinalIgnoreCase);

                if (!hashMatches)
                {
                    plan.FilesToDownload.Add(
                        new ModpackUpdatePlanItem
                        {
                            File = file,
                            Reason =
                                "SHA-256 файла отличается"
                        });
                }
            }

            return plan;
        }

        private static string GetSafeDestinationPath(
            string gameDirectory,
            string installPath)
        {
            if (string.IsNullOrWhiteSpace(installPath))
            {
                throw new InvalidOperationException(
                    "В manifest отсутствует путь установки файла.");
            }

            string normalizedPath =
                installPath
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar)
                    .Replace(
                        '\\',
                        Path.DirectorySeparatorChar);

            string destinationPath =
                Path.GetFullPath(
                    Path.Combine(
                        gameDirectory,
                        normalizedPath));

            string gameDirectoryWithSeparator =
                gameDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!destinationPath.StartsWith(
                    gameDirectoryWithSeparator,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Manifest содержит небезопасный путь:\n" +
                    installPath);
            }

            return destinationPath;
        }

        private static async Task<string>
            CalculateSha256Async(
                string filePath,
                CancellationToken cancellationToken)
        {
            await using FileStream stream =
                new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 1024 * 1024,
                    useAsync: true);

            using SHA256 sha256 =
                SHA256.Create();

            byte[] hash =
                await sha256.ComputeHashAsync(
                    stream,
                    cancellationToken);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }
    }
}