using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StoryLauncher.Services
{
    public sealed class ModpackInstallProgress
    {
        public int CurrentFile { get; set; }

        public int TotalFiles { get; set; }

        public int Percent { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public long DownloadedBytes { get; set; }

        public long TotalBytes { get; set; }

        public long BytesPerSecond { get; set; }

        public TimeSpan? EstimatedRemaining { get; set; }

        public bool IsResumed { get; set; }
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

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "StoryLauncher/1.0");
        }

        public async Task InstallUpdateAsync(
            ModpackUpdatePlan plan,
            string gameDirectory,
            IProgress<ModpackInstallProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(plan);

            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                throw new ArgumentException(
                    "Папка игры не указана.",
                    nameof(gameDirectory));
            }

            Directory.CreateDirectory(gameDirectory);

            long requiredBytes =
                StorageSpaceService.CalculateModpackRequiredBytes(
                    plan.DownloadSize);

            StorageSpaceService.EnsureEnoughSpace(
                gameDirectory,
                requiredBytes);

            string cacheDirectory = GetDownloadCacheDirectory();
            Directory.CreateDirectory(cacheDirectory);

            StorageSpaceService.EnsureEnoughSpace(
                cacheDirectory,
                Math.Max(0, plan.DownloadSize) +
                256L * 1024L * 1024L);

            int totalFiles = plan.FilesToDownload.Count;
            long totalBytes = Math.Max(0, plan.DownloadSize);
            long completedBytes = 0;

            for (int index = 0; index < totalFiles; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ModpackReleaseFile file =
                    plan.FilesToDownload[index].File;

                int currentFile = index + 1;
                string downloadedFile = Path.Combine(
                    cacheDirectory,
                    GetCacheFileName(file));

                long cachedBytes = File.Exists(downloadedFile)
                    ? new FileInfo(downloadedFile).Length
                    : 0;

                if (file.Size > 0 && cachedBytes > file.Size)
                {
                    File.Delete(downloadedFile);
                    cachedBytes = 0;
                }

                ReportProgress(
                    progress,
                    currentFile,
                    totalFiles,
                    CalculateOverallPercent(
                        completedBytes + cachedBytes,
                        totalBytes),
                    file.AssetName,
                    cachedBytes > 0
                        ? "Продолжение прерванной загрузки..."
                        : "Скачивание файла...",
                    completedBytes + cachedBytes,
                    totalBytes,
                    isResumed: cachedBytes > 0);

                await DownloadFileAsync(
                    file,
                    downloadedFile,
                    currentFile,
                    totalFiles,
                    completedBytes,
                    totalBytes,
                    progress,
                    cancellationToken);

                ReportProgress(
                    progress,
                    currentFile,
                    totalFiles,
                    CalculateOverallPercent(
                        completedBytes + Math.Max(0, file.Size),
                        totalBytes),
                    file.AssetName,
                    "Проверка контрольной суммы...",
                    completedBytes + Math.Max(0, file.Size),
                    totalBytes);

                string downloadedSha256 =
                    await CalculateSha256Async(
                        downloadedFile,
                        cancellationToken);

                if (!string.Equals(
                        downloadedSha256,
                        file.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Повреждённый остаток не используется повторно.
                    File.Delete(downloadedFile);

                    ReportProgress(
                        progress,
                        currentFile,
                        totalFiles,
                        CalculateOverallPercent(completedBytes, totalBytes),
                        file.AssetName,
                        "Файл повреждён. Загружаю его заново...",
                        completedBytes,
                        totalBytes);

                    await DownloadFileAsync(
                        file,
                        downloadedFile,
                        currentFile,
                        totalFiles,
                        completedBytes,
                        totalBytes,
                        progress,
                        cancellationToken);

                    downloadedSha256 =
                        await CalculateSha256Async(
                            downloadedFile,
                            cancellationToken);

                    if (!string.Equals(
                            downloadedSha256,
                            file.Sha256,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(downloadedFile);

                        throw new InvalidOperationException(
                            "Контрольная сумма скачанного файла не совпадает.\n\n" +
                            file.AssetName);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                string installStatus = string.Equals(
                    file.Action,
                    "extract",
                    StringComparison.OrdinalIgnoreCase)
                        ? "Распаковка архива..."
                        : "Установка файла...";

                ReportProgress(
                    progress,
                    currentFile,
                    totalFiles,
                    CalculateOverallPercent(
                        completedBytes + Math.Max(0, file.Size),
                        totalBytes),
                    file.AssetName,
                    installStatus,
                    completedBytes + Math.Max(0, file.Size),
                    totalBytes);

                if (string.Equals(
                        file.Action,
                        "extract",
                        StringComparison.OrdinalIgnoreCase))
                {
                    InstallArchive(
                        downloadedFile,
                        gameDirectory,
                        file,
                        cancellationToken);
                }
                else
                {
                    InstallRegularFile(
                        downloadedFile,
                        gameDirectory,
                        file);
                }

                ModpackInstallStateService.MarkPackageInstalled(file);

                completedBytes += Math.Max(0, file.Size);

                try
                {
                    File.Delete(downloadedFile);
                }
                catch
                {
                    // Кэш будет безопасно переиспользован или заменён позже.
                }

                ReportProgress(
                    progress,
                    currentFile,
                    totalFiles,
                    CalculateOverallPercent(completedBytes, totalBytes),
                    file.AssetName,
                    "Готово",
                    completedBytes,
                    totalBytes);
            }

            foreach (string relativePath in plan.FilesToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string deletePath = GetSafeDestinationPath(
                    gameDirectory,
                    relativePath);

                if (File.Exists(deletePath))
                {
                    File.Delete(deletePath);
                }
            }

            ModpackInstallStateService.SetInstalledVersion(
                plan.LatestVersion);

            ModpackStateService.SetInstalledVersion(
                plan.LatestVersion);
        }

        private async Task DownloadFileAsync(
            ModpackReleaseFile file,
            string destinationPath,
            int currentFile,
            int totalFiles,
            long previouslyCompletedBytes,
            long totalPlanBytes,
            IProgress<ModpackInstallProgress>? progress,
            CancellationToken cancellationToken)
        {
            long existingBytes = File.Exists(destinationPath)
                ? new FileInfo(destinationPath).Length
                : 0;

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                file.DownloadUrl);

            if (existingBytes > 0)
            {
                request.Headers.Range =
                    new RangeHeaderValue(existingBytes, null);
            }

            using HttpResponseMessage response =
                await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (response.StatusCode ==
                    HttpStatusCode.RequestedRangeNotSatisfiable &&
                file.Size > 0 &&
                existingBytes == file.Size)
            {
                return;
            }

            response.EnsureSuccessStatusCode();

            bool serverAcceptedResume =
                existingBytes > 0 &&
                response.StatusCode == HttpStatusCode.PartialContent;

            if (!serverAcceptedResume)
            {
                existingBytes = 0;
            }

            long responseBytes =
                response.Content.Headers.ContentLength ?? 0;

            long expectedFileBytes =
                response.Content.Headers.ContentRange?.Length ??
                (responseBytes > 0
                    ? existingBytes + responseBytes
                    : Math.Max(0, file.Size));

            await using Stream networkStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            await using var fileStream = new FileStream(
                destinationPath,
                serverAcceptedResume
                    ? FileMode.Append
                    : FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true);

            byte[] buffer = new byte[1024 * 1024];
            long downloadedFileBytes = existingBytes;
            long downloadedThisRequest = 0;
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int bytesRead = await networkStream.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);

                downloadedFileBytes += bytesRead;
                downloadedThisRequest += bytesRead;

                long speed = stopwatch.Elapsed.TotalSeconds >= 0.35
                    ? (long)(downloadedThisRequest /
                        stopwatch.Elapsed.TotalSeconds)
                    : 0;

                long overallDownloaded = Math.Min(
                    totalPlanBytes,
                    previouslyCompletedBytes + downloadedFileBytes);

                TimeSpan? remaining = null;

                if (speed > 0 && totalPlanBytes > overallDownloaded)
                {
                    remaining = TimeSpan.FromSeconds(
                        (totalPlanBytes - overallDownloaded) /
                        (double)speed);
                }

                ReportProgress(
                    progress,
                    currentFile,
                    totalFiles,
                    CalculateOverallPercent(
                        overallDownloaded,
                        totalPlanBytes),
                    file.AssetName,
                    serverAcceptedResume
                        ? "Продолжение загрузки..."
                        : "Скачивание файла...",
                    overallDownloaded,
                    totalPlanBytes,
                    speed,
                    remaining,
                    serverAcceptedResume);
            }

            await fileStream.FlushAsync(cancellationToken);

            if (expectedFileBytes > 0 &&
                downloadedFileBytes != expectedFileBytes)
            {
                throw new IOException(
                    "Загрузка файла завершилась не полностью. " +
                    "При следующем запуске она продолжится с места остановки.");
            }
        }

        private static void InstallRegularFile(
            string downloadedFile,
            string gameDirectory,
            ModpackReleaseFile file)
        {
            string destinationPath = GetSafeDestinationPath(
                gameDirectory,
                file.InstallPath);

            string? destinationDirectory =
                Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
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
            ModpackReleaseFile file,
            CancellationToken cancellationToken)
        {
            string destinationDirectory = GetSafeDestinationPath(
                gameDirectory,
                file.InstallPath);

            Directory.CreateDirectory(destinationDirectory);

            using ZipArchive archive = ZipFile.OpenRead(archivePath);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                string entryDestination = GetSafeDestinationPath(
                    destinationDirectory,
                    entry.FullName);

                string? entryDirectory =
                    Path.GetDirectoryName(entryDestination);

                if (!string.IsNullOrWhiteSpace(entryDirectory))
                {
                    Directory.CreateDirectory(entryDirectory);
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

            string fullRoot = Path.GetFullPath(rootDirectory);

            string normalizedPath = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            string destinationPath = Path.GetFullPath(
                Path.Combine(fullRoot, normalizedPath));

            string rootWithSeparator = fullRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!destinationPath.StartsWith(
                    rootWithSeparator,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Обнаружен небезопасный путь:\n" + relativePath);
            }

            return destinationPath;
        }

        private static async Task<string> CalculateSha256Async(
            string filePath,
            CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true);

            using SHA256 sha256 = SHA256.Create();

            byte[] hash = await sha256.ComputeHashAsync(
                stream,
                cancellationToken);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string GetDownloadCacheDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "StoryLauncher",
                "DownloadCache",
                "Modpack");
        }

        private static string GetCacheFileName(
            ModpackReleaseFile file)
        {
            string key = file.Sha256?.Trim().ToLowerInvariant()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                byte[] hash = SHA256.HashData(
                    Encoding.UTF8.GetBytes(file.DownloadUrl));

                key = Convert.ToHexString(hash).ToLowerInvariant();
            }

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(invalid, '_');
            }

            return key + ".part";
        }

        private static int CalculateOverallPercent(
            long downloadedBytes,
            long totalBytes)
        {
            if (totalBytes <= 0)
            {
                return 0;
            }

            return (int)Math.Clamp(
                downloadedBytes * 100L / totalBytes,
                0,
                100);
        }

        private static void ReportProgress(
            IProgress<ModpackInstallProgress>? progress,
            int currentFile,
            int totalFiles,
            int percent,
            string fileName,
            string status,
            long downloadedBytes = 0,
            long totalBytes = 0,
            long bytesPerSecond = 0,
            TimeSpan? estimatedRemaining = null,
            bool isResumed = false)
        {
            progress?.Report(
                new ModpackInstallProgress
                {
                    CurrentFile = currentFile,
                    TotalFiles = totalFiles,
                    Percent = Math.Clamp(percent, 0, 100),
                    FileName = fileName,
                    Status = status,
                    DownloadedBytes = downloadedBytes,
                    TotalBytes = totalBytes,
                    BytesPerSecond = bytesPerSecond,
                    EstimatedRemaining = estimatedRemaining,
                    IsResumed = isResumed
                });
        }
    }
}
