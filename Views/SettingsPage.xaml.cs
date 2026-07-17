using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using StoryLauncher.Services;

namespace StoryLauncher.Views
{
    public partial class SettingsPage : Page
    {
        private bool _isLoadingSettings = true;

        public SettingsPage()
        {
            InitializeComponent();

            LoadSettings();
        }

        private void LoadSettings()
        {
            _isLoadingSettings = true;

            MusicCheckBox.IsChecked =
                SettingsService.Current.MusicEnabled;

            int savedRamMb =
                SettingsService.Current.AllocatedRamMb;

            double savedRamGb =
                savedRamMb / 1024.0;

            savedRamGb = Math.Clamp(
                savedRamGb,
                RamSlider.Minimum,
                RamSlider.Maximum);

            RamSlider.Value = savedRamGb;

            JavaPathTextBox.Text =
                SettingsService.Current.JavaPath;

            UpdateRamText();

            _isLoadingSettings = false;
        }

        private void Page_Loaded(
            object sender,
            RoutedEventArgs e)
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

            UpdateRamText();
        }

        private void RamSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateRamText();

            if (_isLoadingSettings)
                return;

            int selectedRamMb =
                (int)Math.Round(
                    RamSlider.Value * 1024);

            SettingsService.Current.AllocatedRamMb =
                selectedRamMb;

            SettingsService.Save();
        }

        private void UpdateRamText()
        {
            if (RamSlider == null ||
                RamValueText == null ||
                RamMbText == null)
            {
                return;
            }

            double selectedRamGb =
                RamSlider.Value;

            int selectedRamMb =
                (int)Math.Round(
                    selectedRamGb * 1024);

            string gbText =
                selectedRamGb % 1 == 0
                    ? $"{selectedRamGb:0} ГБ"
                    : $"{selectedRamGb:0.0} ГБ";

            RamValueText.Text =
                gbText;

            RamMbText.Text =
                $"Выделено: {selectedRamMb} МБ";
        }

        private void MusicCheckBox_Checked(
            object sender,
            RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            MusicService.SetEnabled(true);
        }

        private void MusicCheckBox_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            MusicService.SetEnabled(false);
        }
    }
}
