using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;

namespace StoryLauncher.Services
{
    public sealed class MinecraftLaunchService
    {
        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressChanged;

        public async Task<Process> LaunchAsync(
            string username,
            int maximumRamMb = 4096)
        {
            ValidateUsername(username);

            GamePathService.CreateDirectories();

            string fabricVersionId =
                FabricInstallService.FabricVersionId;

            EnsureFabricInstalled(fabricVersionId);

            StatusChanged?.Invoke(
                "Проверка файлов Minecraft и Fabric...");

            ProgressChanged?.Invoke(0);

            var minecraftPath =
                new MinecraftPath(GamePathService.GameDirectory);

            var launcher =
                new MinecraftLauncher(minecraftPath);

            launcher.FileProgressChanged += (_, e) =>
            {
                if (e.TotalTasks > 0)
                {
                    int progress = (int)Math.Round(
                        e.ProgressedTasks * 100.0 /
                        e.TotalTasks);

                    ProgressChanged?.Invoke(
                        Math.Clamp(progress, 0, 100));
                }

                if (!string.IsNullOrWhiteSpace(e.Name))
                {
                    StatusChanged?.Invoke(
                        $"Проверка: {e.Name}");
                }
            };

            launcher.ByteProgressChanged += (_, e) =>
            {
                if (e.TotalBytes > 0)
                {
                    int progress = (int)Math.Round(
                        e.ProgressedBytes * 100.0 /
                        e.TotalBytes);

                    ProgressChanged?.Invoke(
                        Math.Clamp(progress, 0, 100));
                }
            };

            /*
             * Проверяет и при необходимости докачивает
             * игровые библиотеки и Java.
             */
            await launcher.InstallAsync(fabricVersionId);

            StatusChanged?.Invoke(
                "Подготовка запуска Minecraft...");

            int safeRam = Math.Clamp(
                maximumRamMb,
                2048,
                16384);

            var launchOptions = new MLaunchOption
            {
                Session =
                    MSession.CreateOfflineSession(username),

                MinimumRamMb = 1024,
                MaximumRamMb = safeRam,

                GameLauncherName = "StoryLauncher",
                GameLauncherVersion = "1.0.0"
            };

            Process process =
                await launcher.BuildProcessAsync(
                    fabricVersionId,
                    launchOptions);

            StatusChanged?.Invoke(
                "Запуск Minecraft...");

            process.Start();

            ProgressChanged?.Invoke(100);

            StatusChanged?.Invoke(
                "Minecraft запущен.");

            return process;
        }

        private static void EnsureFabricInstalled(
            string fabricVersionId)
        {
            string fabricJsonPath = Path.Combine(
                GamePathService.GameDirectory,
                "versions",
                fabricVersionId,
                $"{fabricVersionId}.json");

            if (!File.Exists(fabricJsonPath))
            {
                throw new InvalidOperationException(
                    "Fabric Loader не установлен.\n\n" +
                    "Сначала установи Fabric на странице модпака.");
            }
        }

        private static void ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException(
                    "Введите ник игрока.");
            }

            string trimmedUsername = username.Trim();

            if (trimmedUsername.Length < 3 ||
                trimmedUsername.Length > 16)
            {
                throw new ArgumentException(
                    "Ник должен содержать от 3 до 16 символов.");
            }

            foreach (char character in trimmedUsername)
            {
                bool allowed =
                    char.IsLetterOrDigit(character) ||
                    character == '_';

                if (!allowed)
                {
                    throw new ArgumentException(
                        "В нике разрешены только английские буквы, " +
                        "цифры и символ подчёркивания.");
                }
            }
        }
    }
}