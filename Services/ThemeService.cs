using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StoryLauncher.Services
{
    public sealed record LauncherThemeDefinition(
        string Id,
        string DisplayName,
        string Primary,
        string Hover,
        string Pressed,
        string AccentText,
        string SelectedBackground,
        string BackgroundImage);

    public static class ThemeService
    {
        private static readonly List<LauncherThemeDefinition>
            ThemesInternal = new()
            {
                new("StoryPurple", "Story Purple", "#7057FF", "#816CFF", "#5C43E8", "#A494FF", "#292343", "ThemeStoryPurple.png"),
                new("Crimson", "Crimson", "#D94A64", "#E5657C", "#B93650", "#FF8195", "#3D202A", "ThemeCrimson.png"),
                new("Emerald", "Emerald", "#35B982", "#4DCA95", "#239568", "#6DE1AF", "#19372D", "ThemeEmerald.png"),
                new("IceBlue", "Ice Blue", "#4099E8", "#5AAAF2", "#2E7FC8", "#78C4FF", "#1B3044", "ThemeIceBlue.png"),
                new("Golden", "Golden", "#D8A13A", "#E7B653", "#B98525", "#F5CD72", "#3E3120", "ThemeGolden.png"),
                new("DarkAmethyst", "Dark Amethyst", "#A441C5", "#B75AD5", "#842FA3", "#D17CEA", "#35203F", "ThemeDarkAmethyst.png")
            };

        public static IReadOnlyList<LauncherThemeDefinition> Themes =>
            ThemesInternal;

        public static LauncherThemeDefinition Current { get; private set; } =
            ThemesInternal[0];

        public static event Action? ThemeChanged;

        public static void Apply(
            string? themeId,
            bool saveSettings = true)
        {
            LauncherThemeDefinition theme =
                ThemesInternal.FirstOrDefault(
                    item => string.Equals(
                        item.Id,
                        themeId,
                        StringComparison.OrdinalIgnoreCase))
                ?? ThemesInternal[0];

            Current = theme;

            SetBrushColor("PrimaryBrush", theme.Primary);
            SetBrushColor("PrimaryHoverBrush", theme.Hover);
            SetBrushColor("PrimaryPressedBrush", theme.Pressed);
            SetBrushColor("AccentTextBrush", theme.AccentText);
            SetBrushColor("SelectedNavigationBrush", theme.SelectedBackground);
            SetBackground(theme.BackgroundImage);

            SettingsService.Current.LauncherTheme = theme.Id;

            if (saveSettings)
            {
                SettingsService.Save();
            }

            ThemeChanged?.Invoke();
        }

        private static void SetBackground(
            string backgroundImage)
        {
            Uri imageUri = new(
                $"pack://application:,,,/StoryLauncher;component/Assets/Images/{backgroundImage}",
                UriKind.Absolute);

            BitmapImage image = new();
            image.BeginInit();
            image.UriSource = imageUri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            ImageBrush backgroundBrush = new(image)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            backgroundBrush.Freeze();

            Application.Current.Resources["LauncherBackgroundImage"] = image;
            Application.Current.Resources["LauncherBackgroundBrush"] = backgroundBrush;
        }

        private static void SetBrushColor(
            string resourceKey,
            string colorValue)
        {
            Color color =
                (Color)ColorConverter.ConvertFromString(
                    colorValue);

            Application.Current.Resources[resourceKey] =
                new SolidColorBrush(color);
        }
    }
}
