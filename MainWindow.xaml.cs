using System.Windows;
using StoryLauncher.Views;

namespace StoryLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}