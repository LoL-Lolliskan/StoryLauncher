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
        private readonly MinecraftInstallService _minecraftInstallService;
        private readonly FabricInstallService _fabricInstallService;
        private readonly MinecraftLaunchService _minecraftLaunchService;

        private bool _isInstalling;
        private bool _isGameRunning;

        public ModpackPage()
        {
            InitializeComponent();

            _minecraftInstallService = new MinecraftInstallService();
            _fabricInstallService = new FabricInstallService();
            _minecraftLaunchService = new MinecraftLaunchService();

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
            var fade = new DoubleAnimation(
                0,
                1,
                TimeSpan.FromMilliseconds(350));

            var move = new DoubleAnimation(
                18,
                0,
                TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
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
            if (_isInstalling || _isGameRunning)
                return;

            bool minecraftInstalled =
                _minecraftInstallService.IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService.IsFabricInstalled();

            try
            {
                /*
                 * Шаг 1.
                 * Устанавливаем Minecraft 1.21.1.
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
                        return;

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
                 * Шаг 2.
                 * Устанавливаем Fabric Loader.
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
                        return;

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
                 * Шаг 3.
                 * Minecraft и Fabric установлены — запускаем игру.
                 */
                await LaunchGameAsync();
            }
            catch (Exception exception)
            {
                InstallProgressBar.IsIndeterminate = false;

                StatusTextBlock.Text =
                    "Произошла ошибка установки.";

                MessageBox.Show(
                    "Не удалось завершить установку.\n\n" +
                    exception.Message,
                    "Ошибка установки",
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

        private async Task LaunchGameAsync()
        {
            if (_isGameRunning)
                return;

            Window? mainWindow = Window.GetWindow(this);

            try
            {
                _isGameRunning = true;

                SetControlsEnabled(false);

                InstallButton.Content = "ЗАПУСК...";

                InstallProgressBar.IsIndeterminate = false;
                InstallProgressBar.Value = 0;

                ProgressPercentTextBlock.Text = "0%";

                StatusTextBlock.Text =
                    "Подготовка Fabric-сборки к запуску...";

                string username =
                    SettingsService.Current.Nickname?.Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show(
                        "Сначала открой профиль и введи свой ник.",
                        "Ник не указан",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                /*
                 * Выделяем игре оперативную память
                 */
                int ramMb = SettingsService.Current.AllocatedRamMb;

                Process process =
                    await _minecraftLaunchService.LaunchAsync(
                        username,
                        ramMb);

                InstallProgressBar.IsIndeterminate = false;
                InstallProgressBar.Value = 100;

                ProgressPercentTextBlock.Text = "100%";

                StatusTextBlock.Text =
                    "Minecraft запущен.";

                bool shouldResumeMusic =
                    SettingsService.Current.MusicEnabled;

                MusicService.Pause();

                if (mainWindow != null)
                {
                    mainWindow.Hide();
                }

                await process.WaitForExitAsync();

                if (shouldResumeMusic)
                {
                    MusicService.Resume();
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
                if (mainWindow != null &&
                    !mainWindow.IsVisible)
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }

                InstallProgressBar.IsIndeterminate = false;

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
            if (_isInstalling || _isGameRunning)
                return;

            try
            {
                CheckFilesButton.IsEnabled = false;

                StatusTextBlock.Text =
                    "Проверка файлов сборки...";

                InstallProgressBar.IsIndeterminate = true;
                ProgressPercentTextBlock.Text = "...";

                await Task.Delay(500);

                bool minecraftInstalled =
                    _minecraftInstallService
                        .IsVanillaInstalled();

                bool fabricInstalled =
                    _fabricInstallService
                        .IsFabricInstalled();

                int modsCount = CountFiles(
                    GamePathService.ModsDirectory,
                    "*.jar");

                int worldsCount = CountDirectories(
                    GamePathService.SavesDirectory);

                InstallProgressBar.IsIndeterminate = false;

                if (!minecraftInstalled)
                {
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
                    InstallProgressBar.Value = 50;
                    ProgressPercentTextBlock.Text = "50%";

                    StatusTextBlock.Text =
                        "Minecraft установлен, но Fabric не установлен.";

                    MessageBox.Show(
                        "Minecraft 1.21.1 установлен.\n" +
                        "Fabric Loader 0.19.3 не найден.\n\n" +
                        "Нажми кнопку «Установить Fabric».",
                        "Проверка файлов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                InstallProgressBar.Value = 100;
                ProgressPercentTextBlock.Text = "100%";

                StatusTextBlock.Text =
                    $"Сборка готова. Модов: {modsCount}. " +
                    $"Миров: {worldsCount}.";

                MessageBox.Show(
                    "Проверка завершена.\n\n" +
                    "Minecraft 1.21.1: установлен\n" +
                    "Fabric Loader 0.19.3: установлен\n" +
                    $"Количество модов: {modsCount}\n" +
                    $"Количество миров: {worldsCount}",
                    "Проверка файлов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                InstallProgressBar.IsIndeterminate = false;

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
                if (!_isInstalling && !_isGameRunning)
                {
                    CheckFilesButton.IsEnabled = true;
                }
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
                            GamePathService.GameDirectory,

                        UseShellExecute = true
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
                StatusTextBlock.Text = status;
            });
        }

        private void InstallService_ProgressChanged(
            int progress)
        {
            Dispatcher.Invoke(() =>
            {
                int safeProgress =
                    Math.Clamp(progress, 0, 100);

                InstallProgressBar.IsIndeterminate = false;
                InstallProgressBar.Value = safeProgress;

                ProgressPercentTextBlock.Text =
                    $"{safeProgress}%";
            });
        }

        private void SetInstallingState(
            bool isInstalling)
        {
            _isInstalling = isInstalling;

            bool controlsEnabled =
                !isInstalling && !_isGameRunning;

            SetControlsEnabled(controlsEnabled);

            if (isInstalling)
            {
                InstallButton.Content =
                    "УСТАНОВКА...";
            }
        }

        private void SetControlsEnabled(
            bool isEnabled)
        {
            InstallButton.IsEnabled = isEnabled;
            CheckFilesButton.IsEnabled = isEnabled;
            OpenGameFolderButton.IsEnabled = isEnabled;
        }

        private void UpdateInstallationState()
        {
            bool minecraftInstalled =
                _minecraftInstallService
                    .IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService
                    .IsFabricInstalled();

            InstallProgressBar.IsIndeterminate = false;

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

            StatusTextBlock.Text =
                "Minecraft и Fabric установлены. " +
                "Сборка готова к запуску.";

            InstallProgressBar.Value = 100;
            ProgressPercentTextBlock.Text = "100%";

            InstallButton.Content = "ИГРАТЬ";
        }

        private static int CountFiles(
            string directory,
            string searchPattern)
        {
            if (!Directory.Exists(directory))
                return 0;

            try
            {
                return Directory.GetFiles(
                    directory,
                    searchPattern,
                    SearchOption.TopDirectoryOnly).Length;
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
                return 0;

            try
            {
                return Directory.GetDirectories(
                    directory,
                    "*",
                    SearchOption.TopDirectoryOnly).Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}