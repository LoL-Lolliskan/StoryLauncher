using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using StoryLauncher.Services;

namespace StoryLauncher.Views
{
    public partial class CustomLauncherPage : Page
    {
        public CustomLauncherPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            MusicService.TrackChanged -=
                MusicService_TrackChanged;

            MusicService.TrackChanged +=
                MusicService_TrackChanged;

            PlayPageAnimation();
            UpdateThemeSelection();
            UpdateMusicSelection();
            UpdateAnimationControls();
        }

        private void Page_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            MusicService.TrackChanged -=
                MusicService_TrackChanged;
        }

        private void MusicService_TrackChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                    UpdateMusicSelection);

                return;
            }

            UpdateMusicSelection();
        }

        private void ThemeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not string themeId)
            {
                return;
            }

            ThemeService.Apply(themeId);
            UpdateThemeSelection();
            UpdateMusicSelection();
        }

        private void ResetThemeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ThemeService.Apply("StoryPurple");
            UpdateThemeSelection();
            UpdateMusicSelection();
        }

        private void MusicTrackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not string trackId)
            {
                return;
            }

            if (!MusicService.SelectTrack(trackId))
            {
                MessageBox.Show(
                    "Аудиофайл этого трека не найден.\n\n" +
                    "Положи MP3-файл в папку " +
                    "StoryLauncher\\Assets\\Audio и не меняй его название.",
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            UpdateMusicSelection();
        }

        private void AnimationsCheckBox_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool enabled =
                AnimationsCheckBox.IsChecked == true;

            SettingsService.Current.AnimationsEnabled = enabled;
            SettingsService.Save();

            if (Window.GetWindow(this)
                is StoryLauncher.MainWindow mainWindow)
            {
                mainWindow.ApplySidebarAnimationSetting(enabled);
            }

            UpdateAnimationControls();
        }

        private void UpdateAnimationControls()
        {
            bool enabled =
                SettingsService.Current.AnimationsEnabled;

            AnimationsCheckBox.IsChecked = enabled;

            AnimationStatusText.Text =
                enabled
                    ? "Включена — показывается видео в круге"
                    : "Выключена — показывается обычная картинка";
        }

        private void UpdateMusicSelection()
        {
            CurrentMusicText.Text =
                MusicService.IsEnabled
                    ? $"Сейчас играет: {MusicService.CurrentTrackName}"
                    : $"Выбран трек: {MusicService.CurrentTrackName}";

            Brush selectedBorder =
                Application.Current.TryFindResource(
                    "PrimaryBrush") as Brush
                ?? Brushes.MediumPurple;

            Brush selectedBackground =
                Application.Current.TryFindResource(
                    "SelectedNavigationBrush") as Brush
                ?? new SolidColorBrush(
                    Color.FromRgb(41, 35, 67));

            foreach (object child
                     in MusicPanel.Children)
            {
                if (child is not Button button)
                {
                    continue;
                }

                bool selected =
                    string.Equals(
                        button.Tag as string,
                        MusicService.CurrentTrackId,
                        StringComparison.OrdinalIgnoreCase);

                button.BorderBrush =
                    selected
                        ? selectedBorder
                        : new SolidColorBrush(
                            Color.FromArgb(
                                77,
                                52,
                                58,
                                72));

                button.BorderThickness =
                    selected
                        ? new Thickness(2)
                        : new Thickness(1);

                button.Background =
                    selected
                        ? selectedBackground
                        : new SolidColorBrush(
                            Color.FromArgb(
                                153,
                                17,
                                21,
                                29));
            }
        }

        private void UpdateThemeSelection()
        {
            CurrentThemeText.Text =
                $"Текущая тема: " +
                $"{ThemeService.Current.DisplayName}";

            Brush selectedBorder =
                Application.Current.TryFindResource(
                    "PrimaryBrush") as Brush
                ?? Brushes.MediumPurple;

            Brush selectedBackground =
                Application.Current.TryFindResource(
                    "SelectedNavigationBrush") as Brush
                ?? new SolidColorBrush(
                    Color.FromRgb(41, 35, 67));

            foreach (object child
                     in ThemesPanel.Children)
            {
                if (child is not Button button)
                {
                    continue;
                }

                bool selected =
                    string.Equals(
                        button.Tag as string,
                        ThemeService.Current.Id,
                        StringComparison.OrdinalIgnoreCase);

                button.BorderBrush =
                    selected
                        ? selectedBorder
                        : new SolidColorBrush(
                            Color.FromRgb(43, 48, 61));

                button.BorderThickness =
                    selected
                        ? new Thickness(2)
                        : new Thickness(1);

                button.Background =
                    selected
                        ? selectedBackground
                        : new SolidColorBrush(
                            Color.FromRgb(17, 21, 29));
            }
        }

        private void PlayPageAnimation()
        {
            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(
                    0,
                    1,
                    TimeSpan.FromMilliseconds(350)));

            if (RootGrid.RenderTransform
                is TranslateTransform transform)
            {
                transform.BeginAnimation(
                    TranslateTransform.YProperty,
                    new DoubleAnimation(
                        18,
                        0,
                        TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new CubicEase
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                    });
            }
        }
    }
}
