using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Windows;
using StoryLauncher.Services;
using StoryLauncher.Views;

namespace StoryLauncher
{
    public partial class MainWindow : Window
    {
        private bool _isVolumeSliderReady;
        private bool _updateCheckStarted;

        private readonly LauncherUpdateService
            _launcherUpdateService;

        public MainWindow()
        {
            InitializeComponent();

            InitializeSidebarAvatarVideo();

            /*
             * Сначала создаём сервис обновлений.
             * После этого можно получать текущую версию.
             */
            _launcherUpdateService =
                new LauncherUpdateService();

            UpdateLauncherVersionDisplay();

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

        private void InitializeSidebarAvatarVideo()
        {
            try
            {
                if (!SettingsService.Current.AnimationsEnabled)
                {
                    ShowSidebarAvatarFallback();
                    return;
                }

                string videoPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets",
                    "Videos",
                    "SidebarAvatar.mp4");

                if (!File.Exists(videoPath))
                {
                    SidebarAvatarVideo.Visibility = Visibility.Collapsed;
                    SidebarAvatarFallback.Visibility = Visibility.Visible;
                    return;
                }

                SidebarAvatarVideo.Source =
                    new Uri(videoPath, UriKind.Absolute);

                SidebarAvatarVideo.Visibility = Visibility.Visible;
                SidebarAvatarVideo.Opacity = 0;
                SidebarAvatarVideo.SpeedRatio = 0.8;
                SidebarAvatarVideo.Play();
            }
            catch
            {
                ShowSidebarAvatarFallback();
            }
        }

        private void SidebarAvatarVideo_MediaOpened(
            object sender,
            RoutedEventArgs e)
        {
            if (!SettingsService.Current.AnimationsEnabled)
            {
                ShowSidebarAvatarFallback();
                return;
            }

            SidebarAvatarFallback.Visibility = Visibility.Collapsed;
            SidebarAvatarVideo.Visibility = Visibility.Visible;
            SidebarAvatarVideo.Opacity = 1;
            SidebarAvatarVideo.SpeedRatio = 0.8;
            SidebarAvatarVideo.Play();
        }

        private void SidebarAvatarVideo_MediaEnded(
            object sender,
            RoutedEventArgs e)
        {
            if (!SettingsService.Current.AnimationsEnabled)
            {
                ShowSidebarAvatarFallback();
                return;
            }

            SidebarAvatarVideo.Position = TimeSpan.Zero;
            SidebarAvatarVideo.Play();
        }

        private void SidebarAvatarVideo_MediaFailed(
            object sender,
            ExceptionRoutedEventArgs e)
        {
            ShowSidebarAvatarFallback();
        }

        private void ShowSidebarAvatarFallback()
        {
            SidebarAvatarVideo.Stop();
            SidebarAvatarVideo.Opacity = 0;
            SidebarAvatarVideo.Visibility = Visibility.Collapsed;
            SidebarAvatarFallback.Visibility = Visibility.Visible;
        }

        public void ApplySidebarAnimationSetting(bool enabled)
        {
            SettingsService.Current.AnimationsEnabled = enabled;
            SettingsService.Save();

            if (enabled)
            {
                InitializeSidebarAvatarVideo();
            }
            else
            {
                ShowSidebarAvatarFallback();
            }
        }

        private void UpdateLauncherVersionDisplay()
        {
            try
            {
                string currentVersion =
                    _launcherUpdateService.CurrentVersion;

                if (string.IsNullOrWhiteSpace(
                        currentVersion) ||
                    currentVersion == "Неизвестно")
                {
                    LauncherVersionText.Text =
                        $"Версия лаунчера {GetAssemblyVersion()}";

                    return;
                }

                LauncherVersionText.Text =
                    $"Версия лаунчера {currentVersion}";
            }
            catch
            {
                LauncherVersionText.Text =
                    $"Версия лаунчера {GetAssemblyVersion()}";
            }
        }

        private static string GetAssemblyVersion()
        {
            Version? version = Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version;

            return version == null
                ? "0.1.3"
                : $"{version.Major}.{version.Minor}.{version.Build}";
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

            CustomLauncherButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");
        }

        private void CustomLauncherButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainFrame.Navigate(
                new CustomLauncherPage());

            PageTitle.Text = "Custom Launcher";

            HomeButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            CustomLauncherButton.Style =
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

            CustomLauncherButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource(
                    "SelectedNavigationButtonStyle");
        }

        private void DiscordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            "https://discord.gg/nGfcn4qv58",
                        UseShellExecute = true
                    });
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Не удалось открыть Discord.\n\n" +
                    exception.Message,
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ShowHomePage()
        {
            MainFrame.Navigate(
                new HomePage());

            PageTitle.Text = "Новости";

            HomeButton.Style =
                (Style)FindResource(
                    "SelectedNavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource(
                    "NavigationButtonStyle");

            CustomLauncherButton.Style =
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
                VolumeSlider.Value =
                    volume;
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

            profileWindow.ShowDialog();

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

            string avatarPath =
                SettingsService.Current.UseCustomAvatar
                    ? SettingsService.Current.CustomAvatarPath
                    : ProfileImageService.MinecraftAvatarPath;

            if (!string.IsNullOrWhiteSpace(avatarPath) &&
                File.Exists(avatarPath))
            {
                try
                {
                    ProfileAvatarImage.Source =
                        ProfileImageService.LoadBitmap(avatarPath);

                    ProfileAvatarImage.Visibility =
                        Visibility.Visible;

                    ProfileFallbackIcon.Visibility =
                        Visibility.Collapsed;

                    return;
                }
                catch
                {
                    // Если файл повреждён, покажем обычную иконку.
                }
            }

            ProfileAvatarImage.Source = null;
            ProfileAvatarImage.Visibility = Visibility.Collapsed;
            ProfileFallbackIcon.Visibility = Visibility.Visible;
        }
    }
}
