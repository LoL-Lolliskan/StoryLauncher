using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StoryLauncher.Services;

namespace StoryLauncher.Views
{
    public partial class ProfileWindow : Window
    {
        private BitmapSource? _minecraftAvatar;
        private BitmapSource? _currentSkinAvatar;
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
            await RefreshSavedProfileImagesAsync();
        }

        private async Task RefreshSavedProfileImagesAsync()
        {
            string nickname =
                SettingsService.Current.Nickname?.Trim()
                ?? string.Empty;

            if (SettingsService.Current.UseCustomSkin &&
                File.Exists(SettingsService.Current.CustomSkinPath))
            {
                LoadCustomSkin(SettingsService.Current.CustomSkinPath);
            }
            else if (IsValidNickname(nickname) && nickname != "Player")
            {
                await LoadPlayerSkinAsync(nickname);
            }
            else
            {
                ShowSkinPlaceholder();
            }

            ApplySavedAvatar();
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

            var scaleAnimation = new DoubleAnimation
            {
                From = 0.96,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            BeginAnimation(OpacityProperty, fadeAnimation);

            if (RootGrid.RenderTransform is TransformGroup transformGroup)
            {
                if (transformGroup.Children[0] is ScaleTransform scaleTransform)
                {
                    scaleTransform.BeginAnimation(
                        ScaleTransform.ScaleXProperty,
                        scaleAnimation);
                    scaleTransform.BeginAnimation(
                        ScaleTransform.ScaleYProperty,
                        scaleAnimation);
                }

                if (transformGroup.Children[1] is TranslateTransform translateTransform)
                {
                    translateTransform.BeginAnimation(
                        TranslateTransform.YProperty,
                        moveAnimation);
                }
            }
        }

        private async Task LoadPlayerSkinAsync(string nickname)
        {
            try
            {
                ProfileMessageText.Text =
                    "Ищу официальный Minecraft Java-профиль и загружаю скин...";

                MinecraftPlayerSkin? player =
                    await MinecraftSkinService.GetPlayerSkinAsync(nickname);

                if (player == null)
                {
                    ShowSkinPlaceholder();
                    ProfileNameText.Text = nickname;
                    ProfileTypeText.Text =
                        "Локальный профиль — официальный скин не найден";
                    ProfileMessageText.Text =
                        "Официальный профиль не найден. Пробую веб-скин по такому же нику или можно выбрать свой файл.";
                    return;
                }

                ProfileNameText.Text = player.Username;
                ProfileTypeText.Text = player.SourceName;

                _minecraftAvatar = player.Avatar;
                _currentSkinAvatar = player.Avatar;

                ProfileImageService.SaveMinecraftAvatar(player.Avatar);

                PlayerSkinImage.Source = player.BodyRender;
                PlayerSkinImage.Visibility = Visibility.Visible;
                SkinPlaceholderText.Visibility = Visibility.Collapsed;

                if (!SettingsService.Current.UseCustomAvatar)
                {
                    ShowAvatar(player.Avatar);
                }

                ProfileMessageText.Text =
                    "Скин найден. Локальный вход сохранён на этом компьютере.";
            }
            catch
            {
                ShowSkinPlaceholder();
                ProfileNameText.Text = nickname;
                ProfileTypeText.Text =
                    "Локальный профиль — не удалось загрузить скин";
                ProfileMessageText.Text =
                    "Проверь интернет или выбери свой файл скина. Сам локальный вход продолжит работать.";
            }
        }

        private void LoadCustomSkin(string filePath)
        {
            BitmapImage skin = ProfileImageService.LoadBitmap(filePath);
            BitmapSource head = ProfileImageService.RenderHeadFromSkin(skin);
            BitmapSource body = ProfileImageService.RenderBodyFromSkin(skin);

            _currentSkinAvatar = head;

            ProfileImageService.SaveMinecraftAvatar(head);

            PlayerSkinImage.Source = body;
            PlayerSkinImage.Visibility = Visibility.Visible;
            SkinPlaceholderText.Visibility = Visibility.Collapsed;

            if (!SettingsService.Current.UseCustomAvatar)
            {
                ShowAvatar(head);
            }

            ProfileTypeText.Text = "Локальный профиль — свой файл скина";
            ProfileMessageText.Text =
                "Свой скин сохранён. Сейчас он используется в профиле лаунчера.";
        }

        private void ApplySavedAvatar()
        {
            if (SettingsService.Current.UseCustomAvatar &&
                File.Exists(SettingsService.Current.CustomAvatarPath))
            {
                ShowAvatar(
                    ProfileImageService.LoadBitmap(
                        SettingsService.Current.CustomAvatarPath));
                return;
            }

            if (_currentSkinAvatar != null)
            {
                ShowAvatar(_currentSkinAvatar);
            }
        }

        private void ShowAvatar(BitmapSource avatar)
        {
            PlayerAvatarImage.Source = avatar;
            PlayerAvatarImage.Visibility = Visibility.Visible;
            AvatarPlaceholderText.Visibility = Visibility.Collapsed;
        }

        private void ShowSkinPlaceholder()
        {
            _currentSkinAvatar = null;

            PlayerSkinImage.Source = null;
            PlayerSkinImage.Visibility = Visibility.Collapsed;
            SkinPlaceholderText.Visibility = Visibility.Visible;

            if (!SettingsService.Current.UseCustomAvatar)
            {
                PlayerAvatarImage.Source = null;
                PlayerAvatarImage.Visibility = Visibility.Collapsed;
                AvatarPlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void MicrosoftLoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Это следующий этап профиля. Мы подключим официальный вход через браузер Microsoft, сохранение сессии и кнопку выхода.\n\n" +
                "Пароль Microsoft внутри StoryLauncher вводить не потребуется.",
                "Microsoft-вход",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void OfflineLoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string nickname = NicknameTextBox.Text.Trim();

            if (!IsValidNickname(nickname))
            {
                MessageBox.Show(
                    "Ник должен содержать от 3 до 16 символов.\n\n" +
                    "Разрешены только английские буквы, цифры и символ подчёркивания _.",
                    "Неверный ник",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                NicknameTextBox.Focus();
                NicknameTextBox.SelectAll();
                return;
            }

            SettingsService.Current.Nickname = nickname;
            SettingsService.Current.UseCustomSkin = false;
            SettingsService.Save();

            SelectedNickname = nickname;
            ProfileNameText.Text = nickname;
            ProfileTypeText.Text = "Локальный профиль";
            _profileChanged = true;

            await LoadPlayerSkinAsync(nickname);
            ApplySavedAvatar();
        }

        private void ChooseAvatarButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выбери аватарку StoryLauncher",
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                string savedPath =
                    ProfileImageService.SaveCustomAvatar(dialog.FileName);

                SettingsService.Current.CustomAvatarPath = savedPath;
                SettingsService.Current.UseCustomAvatar = true;
                SettingsService.Save();

                ShowAvatar(ProfileImageService.LoadBitmap(savedPath));
                ProfileMessageText.Text =
                    "Своя аватарка сохранена и будет восстановлена после перезапуска.";
                _profileChanged = true;
            }
            catch (Exception exception)
            {
                ShowImageError(exception.Message);
            }
        }

        private async void RestoreAvatarButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SettingsService.Current.UseCustomAvatar = false;
            SettingsService.Save();

            if (_currentSkinAvatar != null)
            {
                ShowAvatar(_currentSkinAvatar);
                ProfileMessageText.Text =
                    "Аватарка восстановлена из текущего Minecraft-скина.";
            }
            else
            {
                string nickname = SettingsService.Current.Nickname.Trim();

                if (IsValidNickname(nickname) && nickname != "Player")
                {
                    await LoadPlayerSkinAsync(nickname);
                }
                else
                {
                    ShowSkinPlaceholder();
                }
            }

            _profileChanged = true;
        }

        private void ChooseSkinButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выбери Minecraft-развёртку 64×64 или 64×32",
                Filter = "Изображения скина (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                string savedPath =
                    ProfileImageService.SaveCustomSkin(dialog.FileName);

                SettingsService.Current.CustomSkinPath = savedPath;
                SettingsService.Current.UseCustomSkin = true;
                SettingsService.Save();

                LoadCustomSkin(savedPath);
                _profileChanged = true;
            }
            catch (Exception exception)
            {
                ShowImageError(exception.Message);
            }
        }

        private async void RestoreNicknameSkinButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string nickname = NicknameTextBox.Text.Trim();

            if (!IsValidNickname(nickname) || nickname == "Player")
            {
                MessageBox.Show(
                    "Сначала введи существующий Minecraft Java-ник.",
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            SettingsService.Current.UseCustomSkin = false;
            SettingsService.Save();

            await LoadPlayerSkinAsync(nickname);
            ApplySavedAvatar();
            _profileChanged = true;
        }

        private void ShowImageError(string message)
        {
            MessageBox.Show(
                message,
                "Не удалось открыть изображение",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static bool IsValidNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname) ||
                nickname.Length < 3 ||
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

                if (!isEnglishLetter && !isDigit && symbol != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
