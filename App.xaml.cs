using System.Windows;
using StoryLauncher.Services;

namespace StoryLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Загружаем сохранённые настройки до открытия окон
            SettingsService.Load();

            // Создаём папки лаунчера и игры
            GamePathService.CreateDirectories();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Дополнительно сохраняем настройки при закрытии
            SettingsService.Save();

            base.OnExit(e);
        }
    }
}