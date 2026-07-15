using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installers;

namespace StoryLauncher.Services
{
    public sealed class MinecraftInstallService
    {
        public const string MinecraftVersion = "1.21.1";

        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressChanged;

        public async Task InstallVanillaAsync(
            CancellationToken cancellationToken = default)
        {
            GamePathService.CreateDirectories();

            var minecraftPath =
                new MinecraftPath(GamePathService.GameDirectory);

            var launcher =
                new MinecraftLauncher(minecraftPath);

            launcher.FileProgressChanged += (_, e) =>
            {
                int total = e.TotalTasks;
                int completed = e.ProgressedTasks;

                if (total > 0)
                {
                    int percentage = (int)Math.Round(
                        completed * 100.0 / total);

                    ProgressChanged?.Invoke(
                        Math.Clamp(percentage, 0, 100));
                }

                if (!string.IsNullOrWhiteSpace(e.Name))
                {
                    StatusChanged?.Invoke(
                        $"Загрузка: {e.Name}");
                }
            };

            launcher.ByteProgressChanged += (_, e) =>
            {
                if (e.TotalBytes > 0)
                {
                    int percentage = (int)Math.Round(
                        e.ProgressedBytes * 100.0 /
                        e.TotalBytes);

                    ProgressChanged?.Invoke(
                        Math.Clamp(percentage, 0, 100));
                }
            };

            StatusChanged?.Invoke(
                "Получение информации о Minecraft 1.21.1...");

            cancellationToken.ThrowIfCancellationRequested();

            await launcher.InstallAsync(MinecraftVersion);

            cancellationToken.ThrowIfCancellationRequested();

            StatusChanged?.Invoke(
                "Minecraft 1.21.1 успешно установлен.");

            ProgressChanged?.Invoke(100);
        }

        public bool IsVanillaInstalled()
        {
            string versionJson = Path.Combine(
                GamePathService.GameDirectory,
                "versions",
                MinecraftVersion,
                $"{MinecraftVersion}.json");

            return File.Exists(versionJson);
        }
    }
}