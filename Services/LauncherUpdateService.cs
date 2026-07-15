using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace StoryLauncher.Services
{
    public sealed class LauncherUpdateService
    {
        private const string RepositoryUrl =
            "https://github.com/LoL-Lolliskan/StoryLauncher";

        private readonly UpdateManager _updateManager;

        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressChanged;

        public LauncherUpdateService()
        {
            var updateSource = new GithubSource(
                RepositoryUrl,
                accessToken: null,
                prerelease: false);

            _updateManager =
                new UpdateManager(updateSource);
        }

        public bool IsInstalledVersion =>
            _updateManager.IsInstalled;

        public string CurrentVersion =>
            _updateManager.CurrentVersion?.ToString()
            ?? "Неизвестно";

        public async Task<bool> CheckAndInstallUpdateAsync(
            CancellationToken cancellationToken = default)
        {
            /*
             * Обновления работают только у версии,
             * установленной через Velopack Setup.exe.
             */
            if (!_updateManager.IsInstalled)
            {
                StatusChanged?.Invoke(
                    "Проверка обновлений доступна только " +
                    "в установленной версии лаунчера.");

                return false;
            }

            StatusChanged?.Invoke(
                "Проверка обновлений лаунчера...");

            ProgressChanged?.Invoke(0);

            UpdateInfo? update =
                await _updateManager.CheckForUpdatesAsync();

            cancellationToken.ThrowIfCancellationRequested();

            if (update == null)
            {
                StatusChanged?.Invoke(
                    "Установлена последняя версия лаунчера.");

                ProgressChanged?.Invoke(100);

                return false;
            }

            StatusChanged?.Invoke(
                $"Загрузка обновления " +
                $"{update.TargetFullRelease.Version}...");

            await _updateManager.DownloadUpdatesAsync(
                update,
                progress =>
                {
                    ProgressChanged?.Invoke(
                        Math.Clamp(progress, 0, 100));
                },
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            StatusChanged?.Invoke(
                "Обновление загружено. Перезапуск...");

            ProgressChanged?.Invoke(100);

            SettingsService.Save();

            /*
             * Лаунчер закроется, установит обновление
             * и автоматически запустится снова.
             */
            _updateManager.ApplyUpdatesAndRestart(
                update.TargetFullRelease);

            return true;
        }
    }
}