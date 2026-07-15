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
    public partial class HomePage : Page
    {
        private readonly MinecraftInstallService _minecraftInstallService;
        private readonly FabricInstallService _fabricInstallService;
        private readonly MinecraftLaunchService _minecraftLaunchService;

        private bool _isGameRunning;

        public HomePage()
        {
            InitializeComponent();

            _minecraftInstallService =
                new MinecraftInstallService();

            _fabricInstallService =
                new FabricInstallService();

            _minecraftLaunchService =
                new MinecraftLaunchService();

            _minecraftLaunchService.StatusChanged +=
                MinecraftLaunchService_StatusChanged;

            _minecraftLaunchService.ProgressChanged +=
                MinecraftLaunchService_ProgressChanged;
        }

        private void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            PlayPageAnimation();

            GamePathService.CreateDirectories();

            UpdateProfileInformation();

            UpdateModpackState();
        }

        private void PlayPageAnimation()
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350)
            };

            var moveAnimation = new DoubleAnimation
            {
                From = 18,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),

                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            BeginAnimation(
                OpacityProperty,
                fadeAnimation);

            if (RootGrid.RenderTransform
                is TranslateTransform transform)
            {
                transform.BeginAnimation(
                    TranslateTransform.YProperty,
                    moveAnimation);
            }
        }

        private void UpdateProfileInformation()
        {
            string nickname =
                SettingsService.Current.Nickname?.Trim()
                ?? string.Empty;

            int allocatedRamMb =
                SettingsService.Current.AllocatedRamMb;

            double allocatedRamGb =
                allocatedRamMb / 1024.0;

            string ramText =
                allocatedRamGb % 1 == 0
                    ? $"{allocatedRamGb:0} ГБ"
                    : $"{allocatedRamGb:0.0} ГБ";

            if (string.IsNullOrWhiteSpace(nickname))
            {
                nickname = "Ник не указан";
            }

            PlayerInfoTextBlock.Text =
                $"{nickname} • RAM: {ramText}";
        }

        private void UpdateModpackState()
        {
            bool minecraftInstalled =
                _minecraftInstallService.IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService.IsFabricInstalled();

            int modsCount = CountFiles(
                GamePathService.ModsDirectory,
                "*.jar");

            int worldsCount = CountDirectories(
                GamePathService.SavesDirectory);

            GameProgressBar.IsIndeterminate = false;

            if (!minecraftInstalled)
            {
                PlayButton.Content =
                    "УСТАНОВИТЬ";

                GameStatusTextBlock.Text =
                    "Minecraft 1.21.1 не установлен.";

                GameProgressBar.Value = 0;
                GameProgressTextBlock.Text = "0%";

                ModpackStateTextBlock.Text =
                    "Требуется установка";

                ModpackDetailsTextBlock.Text =
                    "Открой раздел «Модпак» и установи Minecraft.";

                return;
            }

            if (!fabricInstalled)
            {
                PlayButton.Content =
                    "УСТАНОВИТЬ FABRIC";

                GameStatusTextBlock.Text =
                    "Minecraft установлен, но Fabric не найден.";

                GameProgressBar.Value = 50;
                GameProgressTextBlock.Text = "50%";

                ModpackStateTextBlock.Text =
                    "Fabric не установлен";

                ModpackDetailsTextBlock.Text =
                    "Открой раздел «Модпак» и установи Fabric Loader.";

                return;
            }

            PlayButton.Content = "ИГРАТЬ";

            GameStatusTextBlock.Text =
                "Сборка готова к запуску.";

            GameProgressBar.Value = 100;
            GameProgressTextBlock.Text = "100%";

            ModpackStateTextBlock.Text =
                "Готова к запуску";

            ModpackDetailsTextBlock.Text =
                $"Модов: {modsCount} • Миров: {worldsCount}";
        }

        private async void PlayButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isGameRunning)
            {
                return;
            }

            bool minecraftInstalled =
                _minecraftInstallService.IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService.IsFabricInstalled();

            if (!minecraftInstalled || !fabricInstalled)
            {
                MessageBox.Show(
                    "Сначала открой раздел «Модпак» и установи " +
                    "Minecraft 1.21.1 и Fabric Loader.",
                    "Сборка не установлена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            await LaunchGameAsync();
        }

        private async Task LaunchGameAsync()
        {
            if (_isGameRunning)
            {
                return;
            }

            Window? mainWindow =
                Window.GetWindow(this);

            try
            {
                string username =
                    SettingsService.Current.Nickname?.Trim()
                    ?? string.Empty;

                if (!IsValidUsername(username))
                {
                    MessageBox.Show(
                        "Сначала открой профиль и введи ник.\n\n" +
                        "Ник должен содержать от 3 до 16 символов. " +
                        "Разрешены английские буквы, цифры и знак _. ",
                        "Ник не настроен",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                int ramMb =
                    SettingsService.Current.AllocatedRamMb;

                ramMb = Math.Clamp(
                    ramMb,
                    2048,
                    16384);

                _isGameRunning = true;

                SetControlsEnabled(false);

                PlayButton.Content =
                    "ЗАПУСК...";

                GameStatusTextBlock.Text =
                    "Подготовка Fabric-сборки...";

                GameProgressBar.IsIndeterminate = false;
                GameProgressBar.Value = 0;

                GameProgressTextBlock.Text =
                    "0%";

                Process process =
                    await _minecraftLaunchService.LaunchAsync(
                        username,
                        ramMb);

                GameProgressBar.Value = 100;
                GameProgressTextBlock.Text = "100%";

                GameStatusTextBlock.Text =
                    "Minecraft запущен.";

                PlayButton.Content =
                    "ИГРА ЗАПУЩЕНА";

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

                GameStatusTextBlock.Text =
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

                GameProgressBar.IsIndeterminate = false;

                GameStatusTextBlock.Text =
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

                UpdateProfileInformation();

                UpdateModpackState();
            }
        }

        private void MinecraftLaunchService_StatusChanged(
            string status)
        {
            Dispatcher.Invoke(() =>
            {
                GameStatusTextBlock.Text = status;
            });
        }

        private void MinecraftLaunchService_ProgressChanged(
            int progress)
        {
            Dispatcher.Invoke(() =>
            {
                int safeProgress =
                    Math.Clamp(progress, 0, 100);

                GameProgressBar.IsIndeterminate = false;
                GameProgressBar.Value = safeProgress;

                GameProgressTextBlock.Text =
                    $"{safeProgress}%";
            });
        }

        private void SetControlsEnabled(
            bool isEnabled)
        {
            PlayButton.IsEnabled = isEnabled;
        }

        private static bool IsValidUsername(
            string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            if (username.Length < 3 ||
                username.Length > 16)
            {
                return false;
            }

            foreach (char character in username)
            {
                bool isAllowed =
                    character >= 'A' &&
                    character <= 'Z' ||

                    character >= 'a' &&
                    character <= 'z' ||

                    char.IsDigit(character) ||

                    character == '_';

                if (!isAllowed)
                {
                    return false;
                }
            }

            return true;
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
            {
                return 0;
            }

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