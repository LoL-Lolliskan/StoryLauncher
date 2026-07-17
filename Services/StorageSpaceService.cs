using System;
using System.IO;

namespace StoryLauncher.Services
{
    public sealed class StorageSpaceInfo
    {
        public string DriveName { get; init; } = string.Empty;

        public long AvailableBytes { get; init; }

        public long RequiredBytes { get; init; }
    }

    public static class StorageSpaceService
    {
        private const long SafetyReserveBytes =
            512L * 1024L * 1024L;

        public static long CalculateModpackRequiredBytes(
            long downloadBytes)
        {
            long safeDownloadBytes = Math.Max(0, downloadBytes);

            // Нужны место для загрузки, распаковки и запас на обновление.
            return checked(
                safeDownloadBytes * 2L +
                SafetyReserveBytes);
        }

        public static StorageSpaceInfo EnsureEnoughSpace(
            string targetDirectory,
            long requiredBytes)
        {
            StorageSpaceInfo info = GetSpaceInfo(
                targetDirectory,
                requiredBytes);

            if (info.AvailableBytes < info.RequiredBytes)
            {
                throw new IOException(
                    "Недостаточно свободного места на диске " +
                    $"{info.DriveName}.\n\n" +
                    $"Нужно: {FormatBytes(info.RequiredBytes)}\n" +
                    $"Свободно: {FormatBytes(info.AvailableBytes)}\n\n" +
                    "Освободи место или измени папку игры.");
            }

            return info;
        }

        public static StorageSpaceInfo GetSpaceInfo(
            string targetDirectory,
            long requiredBytes)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                throw new ArgumentException(
                    "Папка для проверки диска не указана.",
                    nameof(targetDirectory));
            }

            string fullPath = Path.GetFullPath(targetDirectory);
            string? root = Path.GetPathRoot(fullPath);

            if (string.IsNullOrWhiteSpace(root))
            {
                throw new IOException(
                    "Не удалось определить диск папки игры.");
            }

            var drive = new DriveInfo(root);

            if (!drive.IsReady)
            {
                throw new IOException(
                    $"Диск {drive.Name} сейчас недоступен.");
            }

            return new StorageSpaceInfo
            {
                DriveName = drive.Name,
                AvailableBytes = drive.AvailableFreeSpace,
                RequiredBytes = Math.Max(0, requiredBytes)
            };
        }

        public static string FormatBytes(long bytes)
        {
            string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double value = Math.Max(0, bytes);
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:0.##} {units[unitIndex]}";
        }
    }
}
