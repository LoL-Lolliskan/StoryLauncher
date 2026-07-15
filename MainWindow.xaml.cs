using System;
using System.Windows;
using StoryLauncher.Services;
using StoryLauncher.Views;

namespace StoryLauncher
{
    public partial class MainWindow : Window
    {
        private bool _isVolumeSliderReady;

        public MainWindow()
        {
            InitializeComponent();

            MusicService.Initialize();

            VolumeSlider.Value = MusicService.Volume * 100;
            _isVolumeSliderReady = true;

            UpdateMusicControls();
            UpdateProfileDisplay();
            ShowHomePage();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHomePage();
        }

        private void ModpackButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ModpackPage());
            PageTitle.Text = "Модпак";

            HomeButton.Style =
                (Style)FindResource("NavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource("SelectedNavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource("NavigationButtonStyle");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
            PageTitle.Text = "Настройки";

            HomeButton.Style =
                (Style)FindResource("NavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource("NavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource("SelectedNavigationButtonStyle");
        }

        private void ShowHomePage()
        {
            MainFrame.Navigate(new HomePage());
            PageTitle.Text = "Главная";

            HomeButton.Style =
                (Style)FindResource("SelectedNavigationButtonStyle");

            ModpackButton.Style =
                (Style)FindResource("NavigationButtonStyle");

            SettingsButton.Style =
                (Style)FindResource("NavigationButtonStyle");
        }

        private void MusicButton_Click(object sender, RoutedEventArgs e)
        {
            MusicService.Toggle();
            UpdateMusicControls(false);
        }

        private void VolumeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isVolumeSliderReady || VolumeText == null)
            {
                return;
            }

            MusicService.SetVolume(e.NewValue / 100.0);
            UpdateMusicControls(false);
        }

        private void UpdateMusicControls(bool updateSlider = true)
        {
            if (MusicIcon == null ||
                MusicButton == null ||
                VolumeText == null)
            {
                return;
            }

            MusicIcon.Text = MusicService.IsEnabled
                ? "\uE767"
                : "\uE74F";

            MusicButton.ToolTip = MusicService.IsEnabled
                ? "Выключить музыку"
                : "Включить музыку";

            int volume = (int)Math.Round(MusicService.Volume * 100);
            VolumeText.Text = $"{volume}%";

            if (updateSlider && VolumeSlider != null)
            {
                VolumeSlider.Value = volume;
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new ProfileWindow
            {
                Owner = this
            };

            bool? result = profileWindow.ShowDialog();

            if (result == true)
            {
                UpdateProfileDisplay();
            }
        }

        private void UpdateProfileDisplay()
        {
            string nickname = SettingsService.Current.Nickname;

            if (string.IsNullOrWhiteSpace(nickname))
            {
                nickname = "Игрок";
            }

            ProfileNameText.Text = nickname;

            ProfileStatusText.Text =
                nickname == "Игрок"
                    ? "Не авторизован"
                    : "Локальный профиль";

            ProfileAvatarText.Text =
                nickname[0].ToString().ToUpperInvariant();
        }
    }
}