using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace StoryLauncher.Services
{
    public sealed class ModpackInstallProgress
    {
        public int CurrentFile { get; set; }

        public int TotalFiles { get; set; }

        public int Percent { get; set; }

        public string FileName { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;
    }

    public sealed class ModpackInstallerService
    {
        private readonly HttpClient _httpClient;

        public ModpackInstallerService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(30)
            };

            _httpClient
                .DefaultRequestHeaders
                .UserAgent
                .ParseAdd("StoryLauncher/1.0");
        }

        public async Task InstallUpdateAsync(
            ModpackUpdatePlan plan,
            string gameDirectory,
            IProgress<ModpackInstallProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(
                    nameof(plan));
            }

            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                throw new ArgumentException(
                    "Папка игры не указана.",
                    nameof(gameDirectory));
            }

            Directory.CreateDirectory(gameDirectory);

            string tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "StoryLauncher",
                "ModpackUpdate");

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);
            }

            Directory.CreateDirectory(tempDirectory);

            try
            {
                int totalFiles =
                    plan.FilesToDownload.Count;

                for (int index = 0;
                     index < totalFiles;
                     index++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    ModpackReleaseFile file =
                        plan.FilesToDownload[index].File;

                    int currentFile =
                        index + 1;

                    ReportProgress(
                        progress,
                        currentFile,
                        totalFiles,
                        0,
                        file.AssetName,
                        "Скачивание файла...");

                    string downloadedFile =
                        Path.Combine(
                            tempDirectory,
                            file.AssetName);

                    await DownloadFileAsync(
                        file,
                        downloadedFile,
                        currentFile,
                        totalFiles,
                        progress,
                        cancellationToken);

                    ReportProgress(
                        progress,
                        currentFile,
                        totalFiles,
                        100,
                        file.AssetName,
                        "Проверка файла...");

                    string downloadedSha256 =
                        await CalculateSha256Async(
                            downloadedFile,
                            cancellationToken);

                    if (!string.Equals(
                            downloadedSha256,
                            file.Sha256,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Контрольная сумма скачанного файла " +
                            "не совпадает.\n\n" +
                            file.AssetName);
                    }

                    if (string.Equals(
                            file.Action,
                            "extract",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ReportProgress(
                            progress,
                            currentFile,
                            totalFiles,
                            100,
                            file.AssetName,
                            "Распаковка архива...");

                        InstallArchive(
                            downloadedFile,
                            gameDirectory,
                            file);
                    }
                    else
                    {
                        ReportProgress(
                            progress,
                            currentFile,
                            totalFiles,
                            100,
                            file.AssetName,
                            "Установка файла...");

                        InstallRegularFile(
                            downloadedFile,
                            gameDirectory,
                            file);
                    }

                    ModpackInstallStateService
                        .MarkPackageInstalled(file);

                    ReportProgress(
                        progress,
                        currentFile,
                        totalFiles,
                        100,
                        file.AssetName,
                        "Готово");
                }

                foreach (string relativePath
                         in plan.FilesToDelete)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    string deletePath =
                        GetSafeDestinationPath(
                            gameDirectory,
                            relativePath);

                    if (File.Exists(deletePath))
                    {
                        File.Delete(deletePath);
                    }
                }

                ModpackInstallStateService
                    .SetInstalledVersion(
                        plan.LatestVersion);

                ModpackStateService
                    .SetInstalledVersion(
                        plan.LatestVersion);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(
                            tempDirectory,
                            recursive: true);
                    }
                }
                catch
                {
                    // Временная папка удалится позднее.
                }
            }
        }

        private async Task DownloadFileAsync(
            ModpackReleaseFile file,
            string destinationPath,
            int currentFile,
            int totalFiles,
            IProgress<ModpackInstallProgress>? progress,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    file.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            long? totalBytes =
                response.Content.Headers.ContentLength;

            await using Stream networkStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            await using FileStream fileStream =
                new FileStream(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024 * 1024,
                    useAsync: true);

            byte[] buffer =
                new byte[1024 * 1024];

            long downloadedBytes = 0;

            while (true)
            {
                int bytesRead =
                    await networkStream.ReadAsync(
                        buffer,
                        cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead),
                    cancellationToken);

                downloadedBytes += bytesRead;

                int percent = 0;

                if (totalBytes.HasValue &&
                    totalBytes.Value > 0)
                {
                    percent = (int)Math.Clamp(
                        downloadedBytes * 100L /
                        totalBytes.Value,
                        0,
                        100);
                }

                ReportProgress(
                    progress,
                    currentFile,
                    totalFiles,
                    percent,
                    file.AssetName,
                    "Скачивание файла...");
            }
        }

        private static void InstallRegularFile(
            string downloadedFile,
            string gameDirectory,
            ModpackReleaseFile file)
        {
            string destinationPath =
                GetSafeDestinationPath(
                    gameDirectory,
                    file.InstallPath);

            string? destinationDirectory =
                Path.GetDirectoryName(
                    destinationPath);

            if (!string.IsNullOrWhiteSpace(
                    destinationDirectory))
            {
                Directory.CreateDirectory(
                    destinationDirectory);
            }

            if (string.Equals(
                    file.InstallMode,
                    "copyIfMissing",
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(destinationPath))
            {
                return;
            }

            File.Copy(
                downloadedFile,
                destinationPath,
                overwrite: true);
        }

        private static void InstallArchive(
            string archivePath,
            string gameDirectory,
            ModpackReleaseFile file)
        {
            string destinationDirectory =
                GetSafeDestinationPath(
                    gameDirectory,
                    file.InstallPath);

            Directory.CreateDirectory(
                destinationDirectory);

            using ZipArchive archive =
                ZipFile.OpenRead(archivePath);

            foreach (ZipArchiveEntry entry
                     in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                string entryDestination =
                    GetSafeDestinationPath(
                        destinationDirectory,
                        entry.FullName);

                string? entryDirectory =
                    Path.GetDirectoryName(
                        entryDestination);

                if (!string.IsNullOrWhiteSpace(
                        entryDirectory))
                {
                    Directory.CreateDirectory(
                        entryDirectory);
                }

                if (string.Equals(
                        file.InstallMode,
                        "copyIfMissing",
                        StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(entryDestination))
                {
                    continue;
                }

                entry.ExtractToFile(
                    entryDestination,
                    overwrite: true);
            }
        }

        private static string GetSafeDestinationPath(
            string rootDirectory,
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new InvalidOperationException(
                    "Путь установки файла пуст.");
            }

            string fullRoot =
                Path.GetFullPath(rootDirectory);

            string normalizedPath =
                relativePath
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar)
                    .Replace(
                        '\\',
                        Path.DirectorySeparatorChar);

            string destinationPath =
                Path.GetFullPath(
                    Path.Combine(
                        fullRoot,
                        normalizedPath));

            string rootWithSeparator =
                fullRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!destinationPath.StartsWith(
                    rootWithSeparator,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Обнаружен небезопасный путь:\n" +
                    relativePath);
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

        private static void ReportProgress(
            IProgress<ModpackInstallProgress>? progress,
            int currentFile,
            int totalFiles,
            int percent,
            string fileName,
            string status)
        {
            progress?.Report(
                new ModpackInstallProgress
                {
                    CurrentFile =
                        currentFile,

                    TotalFiles =
                        totalFiles,

                    Percent =
                        Math.Clamp(
                            percent,
                            0,
                            100),

                    FileName =
                        fileName,

                    Status =
                        status
                });
        }
    }
}