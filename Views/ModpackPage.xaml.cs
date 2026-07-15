using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using StoryLauncher.Services;

namespace StoryLauncher.Views
{
    public partial class ModpackPage : Page
    {
        private readonly MinecraftInstallService
            _minecraftInstallService;

        private readonly FabricInstallService
            _fabricInstallService;

        private readonly MinecraftLaunchService
            _minecraftLaunchService;

        private readonly ModpackUpdateService
            _modpackUpdateService;

        private readonly ModpackInstallerService
            _modpackInstallerService;

        private bool _isInstalling;
        private bool _isGameRunning;

        public ModpackPage()
        {
            InitializeComponent();

            _minecraftInstallService =
                new MinecraftInstallService();

            _fabricInstallService =
                new FabricInstallService();

            _minecraftLaunchService =
                new MinecraftLaunchService();

            _modpackUpdateService =
                new ModpackUpdateService();

            _modpackInstallerService =
                new ModpackInstallerService();

            _minecraftInstallService.StatusChanged +=
                InstallService_StatusChanged;

            _minecraftInstallService.ProgressChanged +=
                InstallService_ProgressChanged;

            _fabricInstallService.StatusChanged +=
                InstallService_StatusChanged;

            _fabricInstallService.ProgressChanged +=
                InstallService_ProgressChanged;

            _minecraftLaunchService.StatusChanged +=
                InstallService_StatusChanged;

            _minecraftLaunchService.ProgressChanged +=
                InstallService_ProgressChanged;
        }

        private void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            PlayPageAnimation();

            GamePathService.CreateDirectories();

            GamePathTextBox.Text =
                GamePathService.GameDirectory;

            UpdateInstallationState();
        }

        private void PlayPageAnimation()
        {
            var fade =
                new DoubleAnimation(
                    0,
                    1,
                    TimeSpan.FromMilliseconds(350));

            var move =
                new DoubleAnimation(
                    18,
                    0,
                    TimeSpan.FromMilliseconds(350))
                {
                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };

            BeginAnimation(
                Page.OpacityProperty,
                fade);

            if (RootGrid.RenderTransform
                is TranslateTransform transform)
            {
                transform.BeginAnimation(
                    TranslateTransform.YProperty,
                    move);
            }
        }

        private async void InstallButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isInstalling ||
                _isGameRunning)
            {
                return;
            }

            bool minecraftInstalled =
                _minecraftInstallService
                    .IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService
                    .IsFabricInstalled();

            try
            {
                /*
                 * Шаг 1 — установка Minecraft.
                 */
                if (!minecraftInstalled)
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "Начать установку Minecraft 1.21.1?",
                            "Установка Minecraft",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    SetInstallingState(true);

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    StatusTextBlock.Text =
                        "Подготовка установки Minecraft 1.21.1...";

                    await _minecraftInstallService
                        .InstallVanillaAsync();

                    InstallProgressBar.Value = 100;
                    ProgressPercentTextBlock.Text = "100%";

                    StatusTextBlock.Text =
                        "Minecraft 1.21.1 успешно установлен.";

                    return;
                }

                /*
                 * Шаг 2 — установка Fabric.
                 */
                if (!fabricInstalled)
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "Minecraft уже установлен.\n\n" +
                            "Установить Fabric Loader 0.19.3?",
                            "Установка Fabric",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    SetInstallingState(true);

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    StatusTextBlock.Text =
                        "Подготовка установки Fabric Loader...";

                    await _fabricInstallService
                        .InstallFabricAsync();

                    InstallProgressBar.Value = 100;
                    ProgressPercentTextBlock.Text = "100%";

                    StatusTextBlock.Text =
                        "Fabric Loader 0.19.3 успешно установлен.";

                    return;
                }

                /*
                 * Шаг 3 — проверка и обновление модпака.
                 */
                SetInstallingState(true);

                bool modpackReady =
                    await EnsureModpackReadyAsync(
                        askForConfirmation: true);

                if (!modpackReady)
                {
                    return;
                }

                /*
                 * Шаг 4 — запуск Minecraft.
                 */
                await LaunchGameAsync();
            }
            catch (Exception exception)
            {
                InstallProgressBar.IsIndeterminate =
                    false;

                StatusTextBlock.Text =
                    "Произошла ошибка.";

                MessageBox.Show(
                    "Не удалось завершить операцию.\n\n" +
                    exception.Message,
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (!_isGameRunning)
                {
                    SetInstallingState(false);
                    UpdateInstallationState();
                }
            }
        }

        /// <summary>
        /// Проверяет файлы модпака и устанавливает
        /// только отсутствующие или изменённые пакеты.
        /// </summary>
        private async Task<bool>
            EnsureModpackReadyAsync(
                bool askForConfirmation)
        {
            StatusTextBlock.Text =
                "Проверка версии модпака...";

            InstallProgressBar.IsIndeterminate =
                true;

            ProgressPercentTextBlock.Text =
                "...";

            /*
             * Получаем информацию о последней версии.
             */
            ModpackVersionInfo versionInfo =
                await _modpackUpdateService
                    .DownloadVersionInfoAsync();

            StatusTextBlock.Text =
                "Загрузка списка файлов модпака...";

            /*
             * Получаем release-manifest.json.
             *
             * Мы скачиваем его даже при совпадении версии,
             * чтобы найти случайно удалённые или повреждённые
             * файлы сборки.
             */
            ModpackReleaseManifest manifest =
                await _modpackUpdateService
                    .DownloadManifestAsync(
                        versionInfo.ManifestUrl);

            StatusTextBlock.Text =
                "Проверка файлов модпака...";

            InstallProgressBar.IsIndeterminate =
                true;

            /*
             * Сравниваем файлы игрока с manifest.
             */
            ModpackUpdatePlan plan =
                await _modpackUpdateService
                    .CreateUpdatePlanAsync(
                        manifest,
                        GamePathService.GameDirectory);

            InstallProgressBar.IsIndeterminate =
                false;

            /*
             * Ничего скачивать не нужно.
             */
            if (!plan.HasChanges)
            {
                InstallProgressBar.Value = 100;
                ProgressPercentTextBlock.Text = "100%";

                StatusTextBlock.Text =
                    $"Модпак обновлён. Версия {manifest.Version}.";

                return true;
            }

            /*
             * Версия изменилась, но сами файлы
             * уже совпадают. Просто сохраняем версию.
             */
            if (plan.DownloadFileCount == 0 &&
                plan.FilesToDelete.Count == 0)
            {
                ModpackInstallStateService
                    .SetInstalledVersion(
                        manifest.Version);

                ModpackStateService
                    .SetInstalledVersion(
                        manifest.Version);

                InstallProgressBar.Value = 100;
                ProgressPercentTextBlock.Text = "100%";

                StatusTextBlock.Text =
                    $"Версия модпака обновлена до " +
                    $"{manifest.Version}.";

                return true;
            }

            string downloadSize =
                FormatFileSize(
                    plan.DownloadSize);

            if (askForConfirmation)
            {
                MessageBoxResult result =
                    MessageBox.Show(
                        "Доступно обновление модпака.\n\n" +
                        $"Установлено: {plan.InstalledVersion}\n" +
                        $"Новая версия: {plan.LatestVersion}\n" +
                        $"Файлов для загрузки: " +
                        $"{plan.DownloadFileCount}\n" +
                        $"Размер загрузки: {downloadSize}\n\n" +
                        "Скачать и установить обновление?",
                        "Обновление Story Modpack",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                {
                    StatusTextBlock.Text =
                        "Обновление модпака отменено.";

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    return false;
                }
            }

            InstallButton.Content =
                "ОБНОВЛЕНИЕ...";

            StatusTextBlock.Text =
                "Подготовка обновления модпака...";

            InstallProgressBar.Value = 0;
            ProgressPercentTextBlock.Text = "0%";

            var progress =
                new Progress<ModpackInstallProgress>(
                    update =>
                    {
                        int safePercent =
                            Math.Clamp(
                                update.Percent,
                                0,
                                100);

                        InstallProgressBar
                            .IsIndeterminate = false;

                        InstallProgressBar.Value =
                            safePercent;

                        ProgressPercentTextBlock.Text =
                            $"{safePercent}%";

                        StatusTextBlock.Text =
                            $"{update.Status}\n" +
                            $"{update.CurrentFile}/" +
                            $"{update.TotalFiles}: " +
                            $"{update.FileName}";
                    });

            /*
             * Скачиваем и устанавливаем только файлы,
             * указанные в плане обновления.
             */
            await _modpackInstallerService
                .InstallUpdateAsync(
                    plan,
                    GamePathService.GameDirectory,
                    progress);

            InstallProgressBar.Value = 100;
            ProgressPercentTextBlock.Text = "100%";

            StatusTextBlock.Text =
                $"Модпак успешно обновлён до версии " +
                $"{plan.LatestVersion}.";

            return true;
        }

        private async Task LaunchGameAsync()
        {
            if (_isGameRunning)
            {
                return;
            }

            Window? mainWindow =
                Window.GetWindow(this);

            bool musicPaused =
                false;

            try
            {
                _isGameRunning = true;

                SetControlsEnabled(false);

                InstallButton.Content =
                    "ЗАПУСК...";

                InstallProgressBar.IsIndeterminate =
                    false;

                InstallProgressBar.Value =
                    0;

                ProgressPercentTextBlock.Text =
                    "0%";

                StatusTextBlock.Text =
                    "Подготовка Fabric-сборки к запуску...";

                string username =
                    SettingsService.Current
                        .Nickname?
                        .Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        username))
                {
                    MessageBox.Show(
                        "Сначала открой профиль и введи свой ник.",
                        "Ник не указан",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                int ramMb =
                    SettingsService.Current
                        .AllocatedRamMb;

                Process process =
                    await _minecraftLaunchService
                        .LaunchAsync(
                            username,
                            ramMb);

                InstallProgressBar.Value =
                    100;

                ProgressPercentTextBlock.Text =
                    "100%";

                StatusTextBlock.Text =
                    "Minecraft запущен.";

                bool shouldResumeMusic =
                    SettingsService.Current
                        .MusicEnabled;

                MusicService.Pause();
                musicPaused = true;

                if (mainWindow != null)
                {
                    mainWindow.Hide();
                }

                await process.WaitForExitAsync();

                if (shouldResumeMusic)
                {
                    MusicService.Resume();
                    musicPaused = false;
                }

                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }

                StatusTextBlock.Text =
                    "Minecraft закрыт. Можно запускать снова.";
            }
            catch (Exception exception)
            {
                if (musicPaused &&
                    SettingsService.Current.MusicEnabled)
                {
                    MusicService.Resume();
                }

                if (mainWindow != null &&
                    !mainWindow.IsVisible)
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }

                InstallProgressBar.IsIndeterminate =
                    false;

                StatusTextBlock.Text =
                    "Не удалось запустить Minecraft.";

                MessageBox.Show(
                    "Ошибка запуска Minecraft.\n\n" +
                    exception.Message,
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isGameRunning = false;

                SetControlsEnabled(true);

                UpdateInstallationState();
            }
        }

        private async void CheckFilesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isInstalling ||
                _isGameRunning)
            {
                return;
            }

            try
            {
                SetInstallingState(true);

                CheckFilesButton.IsEnabled =
                    false;

                StatusTextBlock.Text =
                    "Проверка Minecraft и Fabric...";

                InstallProgressBar.IsIndeterminate =
                    true;

                ProgressPercentTextBlock.Text =
                    "...";

                bool minecraftInstalled =
                    _minecraftInstallService
                        .IsVanillaInstalled();

                bool fabricInstalled =
                    _fabricInstallService
                        .IsFabricInstalled();

                if (!minecraftInstalled)
                {
                    InstallProgressBar
                        .IsIndeterminate = false;

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    StatusTextBlock.Text =
                        "Minecraft 1.21.1 не установлен.";

                    MessageBox.Show(
                        "Minecraft 1.21.1 не найден.",
                        "Проверка файлов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (!fabricInstalled)
                {
                    InstallProgressBar
                        .IsIndeterminate = false;

                    InstallProgressBar.Value = 50;
                    ProgressPercentTextBlock.Text = "50%";

                    StatusTextBlock.Text =
                        "Fabric Loader не установлен.";

                    MessageBox.Show(
                        "Minecraft 1.21.1 установлен.\n" +
                        "Fabric Loader 0.19.3 не найден.",
                        "Проверка файлов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                bool modpackReady =
                    await EnsureModpackReadyAsync(
                        askForConfirmation: true);

                if (!modpackReady)
                {
                    return;
                }

                int modsCount =
                    CountFiles(
                        GamePathService.ModsDirectory,
                        "*.jar");

                int worldsCount =
                    CountDirectories(
                        GamePathService.SavesDirectory);

                MessageBox.Show(
                    "Проверка завершена.\n\n" +
                    "Minecraft 1.21.1: установлен\n" +
                    "Fabric Loader 0.19.3: установлен\n" +
                    $"Версия модпака: " +
                    $"{ModpackInstallStateService.Current.InstalledVersion}\n" +
                    $"Количество модов: {modsCount}\n" +
                    $"Количество миров: {worldsCount}",
                    "Проверка файлов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                InstallProgressBar.IsIndeterminate =
                    false;

                StatusTextBlock.Text =
                    "Не удалось проверить файлы.";

                MessageBox.Show(
                    "Ошибка проверки файлов.\n\n" +
                    exception.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetInstallingState(false);

                if (!_isGameRunning)
                {
                    CheckFilesButton.IsEnabled =
                        true;
                }

                UpdateInstallationState();
            }
        }

        private void OpenGameFolderButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                GamePathService.CreateDirectories();

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            GamePathService
                                .GameDirectory,

                        UseShellExecute =
                            true
                    });
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Не удалось открыть папку игры.\n\n" +
                    exception.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void InstallService_StatusChanged(
            string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text =
                    status;
            });
        }

        private void InstallService_ProgressChanged(
            int progress)
        {
            Dispatcher.Invoke(() =>
            {
                int safeProgress =
                    Math.Clamp(
                        progress,
                        0,
                        100);

                InstallProgressBar
                    .IsIndeterminate = false;

                InstallProgressBar.Value =
                    safeProgress;

                ProgressPercentTextBlock.Text =
                    $"{safeProgress}%";
            });
        }

        private void SetInstallingState(
            bool isInstalling)
        {
            _isInstalling =
                isInstalling;

            bool controlsEnabled =
                !isInstalling &&
                !_isGameRunning;

            SetControlsEnabled(
                controlsEnabled);

            if (isInstalling)
            {
                InstallButton.Content =
                    "ПОДОЖДИТЕ...";
            }
        }

        private void SetControlsEnabled(
            bool isEnabled)
        {
            InstallButton.IsEnabled =
                isEnabled;

            CheckFilesButton.IsEnabled =
                isEnabled;

            OpenGameFolderButton.IsEnabled =
                isEnabled;
        }

        private void UpdateInstallationState()
        {
            bool minecraftInstalled =
                _minecraftInstallService
                    .IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService
                    .IsFabricInstalled();

            InstallProgressBar.IsIndeterminate =
                false;

            if (!minecraftInstalled)
            {
                StatusTextBlock.Text =
                    "Minecraft 1.21.1 ещё не установлен.";

                InstallProgressBar.Value = 0;
                ProgressPercentTextBlock.Text = "0%";

                InstallButton.Content =
                    "УСТАНОВИТЬ MINECRAFT";

                return;
            }

            if (!fabricInstalled)
            {
                StatusTextBlock.Text =
                    "Minecraft установлен. Теперь установи Fabric.";

                InstallProgressBar.Value = 50;
                ProgressPercentTextBlock.Text = "50%";

                InstallButton.Content =
                    "УСТАНОВИТЬ FABRIC";

                return;
            }

            string installedVersion =
                ModpackInstallStateService
                    .Current
                    .InstalledVersion;

            StatusTextBlock.Text =
                "Minecraft и Fabric установлены. " +
                $"Версия модпака: {installedVersion}.";

            InstallProgressBar.Value = 100;
            ProgressPercentTextBlock.Text = "100%";

            InstallButton.Content =
                "ИГРАТЬ";
        }

        private static string FormatFileSize(
            long bytes)
        {
            string[] units =
            {
                "Б",
                "КБ",
                "МБ",
                "ГБ",
                "ТБ"
            };

            double size =
                bytes;

            int unitIndex =
                0;

            while (size >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return
                $"{size:0.##} {units[unitIndex]}";
        }

        private static int CountFiles(
            string directory,
            string searchPattern)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            try
            {
                return Directory.GetFiles(
                    directory,
                    searchPattern,
                    SearchOption.TopDirectoryOnly)
                    .Length;
            }
            catch
            {
                return 0;
            }
        }

        private static int CountDirectories(
            string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            try
            {
                return Directory.GetDirectories(
                    directory,
                    "*",
                    SearchOption.TopDirectoryOnly)
                    .Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}