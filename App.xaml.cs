using System.Windows;
using StoryLauncher.Services;
using Velopack;

namespace StoryLauncher
{
    public partial class App : Application
    {
        public App()
        {
            // Velopack должен запускаться как можно раньше.
            VelopackApp.Build().Run();
        }

        protected override void OnStartup(
            StartupEventArgs e)
        {
            SettingsService.Load();

            ThemeService.Apply(
                SettingsService.Current.LauncherTheme,
                saveSettings: false);

            ModpackStateService.Load();

            ModpackInstallStateService.Load();

            GamePathService.CreateDirectories();

            base.OnStartup(e);
        }

        protected override void OnExit(
            ExitEventArgs e)
        {
            SettingsService.Save();

            base.OnExit(e);
        }
    }
}
