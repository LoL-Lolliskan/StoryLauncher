using System;
using System.Threading.Tasks;
using System.Windows;
using StoryLauncher.Services;
using StoryLauncher.Views;

namespace StoryLauncher
{
    public partial class MainWindow : Window
    {
        private bool _isVolumeSliderReady;
        private bool _updateCheckStarted;

        private readonly LauncherUpdateService _launcherUpdateService;

        public MainWindow()
        {
            InitializeComponent();

            _launcherUpdateService =
                new LauncherUpdateService();

            /*
             * Проверяем обновления после открытия окна,
             * чтобы запуск интерфейса не зависал.
             */
            Loaded += MainWindow_Loaded;

            MusicService.Initialize();

            VolumeSlider.Value =
                MusicService.Volume * 100;

            _isVolumeSliderReady = true;

            UpdateMusicControls();
            UpdateProfileDisplay();
            ShowHomePage();
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_updateCheckStarted)
            {
                return;
            }

            _updateCheckStarted = true;

            await CheckLauncherUpdatesAsync();
        }

        private async Task CheckLauncherUpdatesAsync()
        {
            try
            {
                /*
                 * В Visual Studio и обычной папке publish
                 * обновление не запускается.
                 *
                 * Оно работает у версии, установленной
                 * через StoryLauncher-win-Setup.exe.
                 */
                if (!_launcherUpdateService.IsInstalledVersion)
                {
                    return;
                }

                await _launcherUpdateService
                    .CheckAndInstallUpdateAsync();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Не удалось проверить обновления лаунчера.\n\n" +
                    exception.Message,
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void HomeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowHomePage();
        }

        private void ModpackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainFrame.Navigate(
                new ModpackPage());

            PageTitle.Text = "Модпак";

            HomeButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource(
                    "SelectedNavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");
        }

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainFrame.Navigate(
                new SettingsPage());

            PageTitle.Text = "Настройки";

            HomeButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource(
                    "SelectedNavigationButtonStyle");
        }

        private void ShowHomePage()
        {
            MainFrame.Navigate(
                new HomePage());

            PageTitle.Text = "Главная";

            HomeButton.Style =
                (Style)FindResource(
                    "SelectedNavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");
        }

        private void MusicButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MusicService.Toggle();

            UpdateMusicControls(false);
        }

        private void VolumeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isVolumeSliderReady ||
                VolumeText == null)
            {
                return;
            }

            MusicService.SetVolume(
                e.NewValue / 100.0);

            UpdateMusicControls(false);
        }

        private void UpdateMusicControls(
            bool updateSlider = true)
        {
            if (MusicIcon == null ||
                MusicButton == null ||
                VolumeText == null)
            {
                return;
            }

            MusicIcon.Text =
                MusicService.IsEnabled
                    ? "\uE767"
                    : "\uE74F";

            MusicButton.ToolTip =
                MusicService.IsEnabled
                    ? "Выключить музыку"
                    : "Включить музыку";

            int volume =
                (int)Math.Round(
                    MusicService.Volume * 100);

            VolumeText.Text =
                $"{volume}%";

            if (updateSlider &&
                VolumeSlider != null)
            {
                VolumeSlider.Value = volume;
            }
        }

        private void ProfileButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var profileWindow =
                new ProfileWindow
                {
                    Owner = this
                };

            bool? result =
                profileWindow.ShowDialog();

            if (result == true)
            {
                UpdateProfileDisplay();

                /*
                 * Обновляем главную страницу,
                 * чтобы там сразу появился новый ник.
                 */
                if (MainFrame.Content
                    is HomePage)
                {
                    ShowHomePage();
                }
            }
        }

        private void UpdateProfileDisplay()
        {
            string nickname =
                SettingsService.Current
                    .Nickname?
                    .Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    nickname))
            {
                nickname = "Игрок";
            }

            ProfileNameText.Text =
                nickname;

            ProfileStatusText.Text =
                nickname == "Игрок"
                    ? "Не авторизован"
                    : "Локальный профиль";

            ProfileAvatarText.Text =
                nickname[0]
                    .ToString()
                    .ToUpperInvariant();
        }
    }
}