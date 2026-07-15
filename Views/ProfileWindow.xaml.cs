using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using StoryLauncher.Services;

namespace StoryLauncher.Views
{
    public partial class ProfileWindow : Window
    {
        private bool _profileChanged;

        public string SelectedNickname { get; private set; } = "Player";

        public ProfileWindow()
        {
            InitializeComponent();

            LoadSavedProfile();
        }

        private void LoadSavedProfile()
        {
            string savedNickname =
                SettingsService.Current.Nickname?.Trim()
                ?? string.Empty;

            if (!IsValidNickname(savedNickname))
            {
                savedNickname = "Player";
            }

            SelectedNickname = savedNickname;

            NicknameTextBox.Text = savedNickname;
            ProfileNameText.Text = savedNickname;

            ProfileTypeText.Text =
                savedNickname == "Player"
                    ? "Введите свой ник"
                    : "Локальный профиль";
        }

        private async void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            PlayOpeningAnimation();

            string nickname =
                SettingsService.Current.Nickname?.Trim()
                ?? string.Empty;

            if (IsValidNickname(nickname) &&
                nickname != "Player")
            {
                await LoadPlayerSkinAsync(nickname);
            }
            else
            {
                ShowSkinPlaceholder();
            }
        }

        private void PlayOpeningAnimation()
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(280)
            };

            var moveAnimation = new DoubleAnimation
            {
                From = 18,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var scaleAnimationX = new DoubleAnimation
            {
                From = 0.96,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var scaleAnimationY = new DoubleAnimation
            {
                From = 0.96,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            BeginAnimation(
                OpacityProperty,
                fadeAnimation);

            if (RootGrid.RenderTransform
                is TransformGroup transformGroup)
            {
                if (transformGroup.Children.Count > 0 &&
                    transformGroup.Children[0]
                        is ScaleTransform scaleTransform)
                {
                    scaleTransform.BeginAnimation(
                        ScaleTransform.ScaleXProperty,
                        scaleAnimationX);

                    scaleTransform.BeginAnimation(
                        ScaleTransform.ScaleYProperty,
                        scaleAnimationY);
                }

                if (transformGroup.Children.Count > 1 &&
                    transformGroup.Children[1]
                        is TranslateTransform translateTransform)
                {
                    translateTransform.BeginAnimation(
                        TranslateTransform.YProperty,
                        moveAnimation);
                }
            }
        }

        private async Task LoadPlayerSkinAsync(
            string nickname)
        {
            try
            {
                ProfileTypeText.Text =
                    "Загрузка скина...";

                MinecraftPlayerSkin? player =
                    await MinecraftSkinService
                        .GetPlayerSkinAsync(nickname);

                if (player == null)
                {
                    ShowSkinPlaceholder();

                    ProfileNameText.Text = nickname;

                    ProfileTypeText.Text =
                        "Локальный профиль — официальный скин не найден";

                    return;
                }

                ProfileNameText.Text =
                    player.Username;

                ProfileTypeText.Text =
                    "Minecraft Java профиль";

                PlayerAvatarImage.Source =
                    player.Avatar;

                PlayerAvatarImage.Visibility =
                    Visibility.Visible;

                AvatarPlaceholderText.Visibility =
                    Visibility.Collapsed;

                PlayerSkinImage.Source =
                    player.BodyRender;

                PlayerSkinImage.Visibility =
                    Visibility.Visible;

                SkinPlaceholderText.Visibility =
                    Visibility.Collapsed;
            }
            catch
            {
                ShowSkinPlaceholder();

                ProfileNameText.Text = nickname;

                ProfileTypeText.Text =
                    "Локальный профиль — не удалось загрузить скин";
            }
        }

        private void ShowSkinPlaceholder()
        {
            PlayerAvatarImage.Source = null;

            PlayerAvatarImage.Visibility =
                Visibility.Collapsed;

            AvatarPlaceholderText.Visibility =
                Visibility.Visible;

            PlayerSkinImage.Source = null;

            PlayerSkinImage.Visibility =
                Visibility.Collapsed;

            SkinPlaceholderText.Visibility =
                Visibility.Visible;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_profileChanged)
            {
                DialogResult = true;
                return;
            }

            Close();
        }

        private void MicrosoftLoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Вход через Microsoft пока не подключён.\n\n" +
                "Сейчас можно использовать локальный профиль.",
                "StoryLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void OfflineLoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string nickname =
                NicknameTextBox.Text.Trim();

            if (!IsValidNickname(nickname))
            {
                MessageBox.Show(
                    "Ник должен содержать от 3 до 16 символов.\n\n" +
                    "Разрешены только английские буквы, цифры " +
                    "и символ подчёркивания _.",
                    "Неверный ник",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                NicknameTextBox.Focus();
                NicknameTextBox.SelectAll();

                return;
            }

            SettingsService.Current.Nickname =
                nickname;

            SettingsService.Save();

            SelectedNickname = nickname;

            NicknameTextBox.Text = nickname;
            ProfileNameText.Text = nickname;
            ProfileTypeText.Text = "Локальный профиль";

            _profileChanged = true;

            await LoadPlayerSkinAsync(nickname);
        }

        private static bool IsValidNickname(
            string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return false;
            }

            if (nickname.Length < 3 ||
                nickname.Length > 16)
            {
                return false;
            }

            foreach (char symbol in nickname)
            {
                bool isEnglishLetter =
                    symbol >= 'A' && symbol <= 'Z' ||
                    symbol >= 'a' && symbol <= 'z';

                bool isDigit =
                    symbol >= '0' && symbol <= '9';

                bool isAllowed =
                    isEnglishLetter ||
                    isDigit ||
                    symbol == '_';

                if (!isAllowed)
                {
                    return false;
                }
            }

            return true;
        }
    }
}