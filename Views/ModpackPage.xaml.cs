using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using StoryLauncher.Services;

namespace StoryLauncher.Views
{
    public partial class ModpackPage : Page
    {
        private readonly MinecraftInstallService
            _minecraftInstallService;

        private readonly FabricInstallService
            _fabricInstallService;

        private readonly MinecraftLaunchService
            _minecraftLaunchService;

        private readonly ModpackUpdateService
            _modpackUpdateService;

        private readonly ModpackInstallerService
            _modpackInstallerService;

        private bool _isInstalling;
        private bool _isGameRunning;
        private CancellationTokenSource? _operationCancellation;

        private readonly List<string> _slideImages = new()
        {
            "/StoryLauncher;component/Assets/Images/ModpackSlide1.png",
            "/StoryLauncher;component/Assets/Images/ModpackSlide2.png",
            "/StoryLauncher;component/Assets/Images/ModpackSlide3.png",
            "/StoryLauncher;component/Assets/Images/ModpackSlide4.png"
        };

        private readonly DispatcherTimer _slideTimer;
        private int _currentSlideIndex;
        private bool _isSlideChanging;

        public ModpackPage()
        {
            InitializeComponent();

            _minecraftInstallService =
                new MinecraftInstallService();

            _fabricInstallService =
                new FabricInstallService();

            _minecraftLaunchService =
                new MinecraftLaunchService();

            _modpackUpdateService =
                new ModpackUpdateService();

            _modpackInstallerService =
                new ModpackInstallerService();

            _slideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            _slideTimer.Tick +=
                (_, _) => ChangeSlide(1);

            _minecraftInstallService.StatusChanged +=
                InstallService_StatusChanged;

            _minecraftInstallService.ProgressChanged +=
                InstallService_ProgressChanged;

            _fabricInstallService.StatusChanged +=
                InstallService_StatusChanged;

            _fabricInstallService.ProgressChanged +=
                InstallService_ProgressChanged;

            _minecraftLaunchService.StatusChanged +=
                InstallService_StatusChanged;

            _minecraftLaunchService.ProgressChanged +=
                InstallService_ProgressChanged;
        }

        private void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            PlayPageAnimation();

            GamePathService.CreateDirectories();

            GamePathTextBox.Text =
                GamePathService.GameDirectory;

            UpdateDiskSpaceDisplay();

            UpdateInstallationState();

            BuildSlideDots();
            ShowCurrentSlide(false);
            _slideTimer.Start();
        }

        private void Page_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _slideTimer.Stop();
        }

        private void PreviousSlideButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ChangeSlide(-1);
        }

        private void NextSlideButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ChangeSlide(1);
        }

        private void ChangeSlide(int direction)
        {
            if (_isSlideChanging)
            {
                return;
            }

            _currentSlideIndex =
                (_currentSlideIndex + direction +
                 _slideImages.Count) %
                _slideImages.Count;

            ShowCurrentSlide(true);

            _slideTimer.Stop();
            _slideTimer.Start();
        }

        private void ShowCurrentSlide(bool animate)
        {
            void ApplyImage()
            {
                ModpackSlideImage.Source =
                    new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(
                            _slideImages[_currentSlideIndex],
                            UriKind.Relative));

                UpdateSlideDots();
            }

            if (!animate)
            {
                ApplyImage();
                return;
            }

            _isSlideChanging = true;

            var fadeOut =
                new DoubleAnimation(
                    1,
                    0,
                    TimeSpan.FromMilliseconds(220));

            fadeOut.Completed += (_, _) =>
            {
                ApplyImage();

                var fadeIn =
                    new DoubleAnimation(
                        0,
                        1,
                        TimeSpan.FromMilliseconds(420))
                    {
                        EasingFunction =
                            new CubicEase
                            {
                                EasingMode =
                                    EasingMode.EaseOut
                            }
                    };

                fadeIn.Completed +=
                    (_, _) =>
                        _isSlideChanging = false;

                ModpackSlideContainer.BeginAnimation(
                    OpacityProperty,
                    fadeIn);
            };

            ModpackSlideContainer.BeginAnimation(
                OpacityProperty,
                fadeOut);
        }

        private void BuildSlideDots()
        {
            ModpackDotsPanel.Children.Clear();

            for (int index = 0;
                 index < _slideImages.Count;
                 index++)
            {
                int selectedIndex = index;

                var dot = new Button
                {
                    Width = 9,
                    Height = 9,
                    Margin = new Thickness(5, 0, 5, 0),
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Background = Brushes.White,
                    Opacity = 0.35,
                    Cursor = Cursors.Hand,
                    Template = CreateSlideDotTemplate()
                };

                dot.Click += (_, _) =>
                {
                    if (_currentSlideIndex ==
                        selectedIndex)
                    {
                        return;
                    }

                    _currentSlideIndex =
                        selectedIndex;

                    ShowCurrentSlide(true);

                    _slideTimer.Stop();
                    _slideTimer.Start();
                };

                ModpackDotsPanel.Children.Add(dot);
            }
        }

        private static ControlTemplate
            CreateSlideDotTemplate()
        {
            var template =
                new ControlTemplate(
                    typeof(Button));

            var border =
                new FrameworkElementFactory(
                    typeof(Border));

            border.SetValue(
                Border.BackgroundProperty,
                new TemplateBindingExtension(
                    BackgroundProperty));

            border.SetValue(
                Border.CornerRadiusProperty,
                new CornerRadius(5));

            template.VisualTree = border;

            return template;
        }

        private void UpdateSlideDots()
        {
            for (int index = 0;
                 index < ModpackDotsPanel.Children.Count;
                 index++)
            {
                if (ModpackDotsPanel.Children[index]
                    is not Button dot)
                {
                    continue;
                }

                bool selected =
                    index == _currentSlideIndex;

                dot.Opacity =
                    selected ? 1 : 0.35;

                dot.Width =
                    selected ? 24 : 9;

                dot.Background =
                    selected
                        ? (Application.Current.TryFindResource(
                            "PrimaryBrush") as Brush
                            ?? Brushes.MediumPurple)
                        : Brushes.White;
            }
        }

        private void PlayPageAnimation()
        {
            var fade =
                new DoubleAnimation(
                    0,
                    1,
                    TimeSpan.FromMilliseconds(350));

            var move =
                new DoubleAnimation(
                    18,
                    0,
                    TimeSpan.FromMilliseconds(350))
                {
                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };

            BeginAnimation(
                Page.OpacityProperty,
                fade);

            if (RootGrid.RenderTransform
                is TranslateTransform transform)
            {
                transform.BeginAnimation(
                    TranslateTransform.YProperty,
                    move);
            }
        }

        private async void InstallButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isInstalling ||
                _isGameRunning)
            {
                return;
            }

            bool minecraftInstalled =
                _minecraftInstallService
                    .IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService
                    .IsFabricInstalled();

            bool operationCancelled = false;

            try
            {
                /*
                 * Шаг 1 — установка Minecraft.
                 */
                if (!minecraftInstalled)
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "Начать установку Minecraft 1.21.1?",
                            "Установка Minecraft",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    SetInstallingState(true);

                    CancellationToken cancellationToken =
                        BeginCancellableOperation();

                    StorageSpaceService.EnsureEnoughSpace(
                        GamePathService.GameDirectory,
                        4L * 1024L * 1024L * 1024L);

                    UpdateDiskSpaceDisplay();

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    StatusTextBlock.Text =
                        "Подготовка установки Minecraft 1.21.1...";

                    await _minecraftInstallService
                        .InstallVanillaAsync(
                            cancellationToken);

                    InstallProgressBar.Value = 100;
                    ProgressPercentTextBlock.Text = "100%";

                    StatusTextBlock.Text =
                        "Minecraft 1.21.1 успешно установлен.";

                    return;
                }

                /*
                 * Шаг 2 — установка Fabric.
                 */
                if (!fabricInstalled)
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "Minecraft уже установлен.\n\n" +
                            "Установить Fabric Loader 0.19.3?",
                            "Установка Fabric",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    SetInstallingState(true);

                    CancellationToken cancellationToken =
                        BeginCancellableOperation();

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    StatusTextBlock.Text =
                        "Подготовка установки Fabric Loader...";

                    await _fabricInstallService
                        .InstallFabricAsync(
                            cancellationToken);

                    InstallProgressBar.Value = 100;
                    ProgressPercentTextBlock.Text = "100%";

                    StatusTextBlock.Text =
                        "Fabric Loader 0.19.3 успешно установлен.";

                    return;
                }

                /*
                 * Шаг 3 — проверка и обновление модпака.
                 */
                SetInstallingState(true);

                CancellationToken modpackCancellationToken =
                    BeginCancellableOperation();

                bool modpackReady =
                    await EnsureModpackReadyAsync(
                        askForConfirmation: true,
                        cancellationToken:
                            modpackCancellationToken);

                if (!modpackReady)
                {
                    return;
                }

                /*
                 * Шаг 4 — запуск Minecraft.
                 */
                EndCancellableOperation();
                SetInstallingState(false);
                await LaunchGameAsync();
            }
            catch (OperationCanceledException)
            {
                operationCancelled = true;

                InstallProgressBar.IsIndeterminate = false;
                StatusTextBlock.Text =
                    "Операция отменена пользователем.";
                ProgressDetailsTextBlock.Text =
                    "Загруженная часть сохранена. При следующем запуске скачивание продолжится.";
            }
            catch (Exception exception)
            {
                InstallProgressBar.IsIndeterminate =
                    false;

                StatusTextBlock.Text =
                    "Произошла ошибка.";

                MessageBox.Show(
                    "Не удалось завершить операцию.\n\n" +
                    exception.Message,
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (!_isGameRunning)
                {
                    EndCancellableOperation();
                    SetInstallingState(false);

                    if (!operationCancelled)
                    {
                        UpdateInstallationState();
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет файлы модпака и устанавливает
        /// только отсутствующие или изменённые пакеты.
        /// </summary>
        private async Task<bool>
            EnsureModpackReadyAsync(
                bool askForConfirmation,
                CancellationToken cancellationToken)
        {
            StatusTextBlock.Text =
                "Проверка версии модпака...";

            InstallProgressBar.IsIndeterminate =
                true;

            ProgressPercentTextBlock.Text =
                "...";

            /*
             * Получаем информацию о последней версии.
             */
            ModpackVersionInfo versionInfo =
                await _modpackUpdateService
                    .DownloadVersionInfoAsync(
                        cancellationToken);

            StatusTextBlock.Text =
                "Загрузка списка файлов модпака...";

            /*
             * Получаем release-manifest.json.
             *
             * Мы скачиваем его даже при совпадении версии,
             * чтобы найти случайно удалённые или повреждённые
             * файлы сборки.
             */
            ModpackReleaseManifest manifest =
                await _modpackUpdateService
                    .DownloadManifestAsync(
                        versionInfo.ManifestUrl,
                        cancellationToken);

            StatusTextBlock.Text =
                "Проверка файлов модпака...";

            InstallProgressBar.IsIndeterminate =
                true;

            /*
             * Сравниваем файлы игрока с manifest.
             */
            var checkProgress =
                new Progress<ModpackCheckProgress>(
                    update =>
                    {
                        SetProgressValue(update.Percent);

                        ProgressPercentTextBlock.Text =
                            $"{update.Percent}%";

                        StatusTextBlock.Text =
                            "Проверка файлов модпака...";

                        ProgressDetailsTextBlock.Text =
                            $"{update.CurrentFile}/{update.TotalFiles}: " +
                            update.FileName +
                            FormatRemainingTime(
                                update.EstimatedRemaining);
                    });

            ModpackUpdatePlan plan =
                await _modpackUpdateService
                    .CreateUpdatePlanAsync(
                        manifest,
                        GamePathService.GameDirectory,
                        checkProgress,
                        cancellationToken);

            InstallProgressBar.IsIndeterminate =
                false;

            /*
             * Ничего скачивать не нужно.
             */
            if (!plan.HasChanges)
            {
                InstallProgressBar.Value = 100;
                ProgressPercentTextBlock.Text = "100%";

                StatusTextBlock.Text =
                    $"Модпак обновлён. Версия {manifest.Version}.";

                return true;
            }

            /*
             * Версия изменилась, но сами файлы
             * уже совпадают. Просто сохраняем версию.
             */
            if (plan.DownloadFileCount == 0 &&
                plan.FilesToDelete.Count == 0)
            {
                ModpackInstallStateService
                    .SetInstalledVersion(
                        manifest.Version);

                ModpackStateService
                    .SetInstalledVersion(
                        manifest.Version);

                InstallProgressBar.Value = 100;
                ProgressPercentTextBlock.Text = "100%";

                StatusTextBlock.Text =
                    $"Версия модпака обновлена до " +
                    $"{manifest.Version}.";

                return true;
            }

            string downloadSize =
                FormatFileSize(
                    plan.DownloadSize);

            long requiredBytes =
                StorageSpaceService
                    .CalculateModpackRequiredBytes(
                        plan.DownloadSize);

            StorageSpaceInfo spaceInfo =
                StorageSpaceService.EnsureEnoughSpace(
                    GamePathService.GameDirectory,
                    requiredBytes);

            DiskSpaceTextBlock.Text =
                $"Диск {spaceInfo.DriveName} • свободно " +
                $"{StorageSpaceService.FormatBytes(spaceInfo.AvailableBytes)} • " +
                $"нужно примерно " +
                $"{StorageSpaceService.FormatBytes(requiredBytes)}";

            if (askForConfirmation)
            {
                MessageBoxResult result =
                    MessageBox.Show(
                        "Доступно обновление модпака.\n\n" +
                        $"Установлено: {plan.InstalledVersion}\n" +
                        $"Новая версия: {plan.LatestVersion}\n" +
                        $"Файлов для загрузки: " +
                        $"{plan.DownloadFileCount}\n" +
                        $"Размер загрузки: {downloadSize}\n" +
                        $"Свободно на диске: " +
                        $"{StorageSpaceService.FormatBytes(spaceInfo.AvailableBytes)}\n\n" +
                        "Скачать и установить обновление?",
                        "Обновление Story Modpack",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                {
                    StatusTextBlock.Text =
                        "Обновление модпака отменено.";

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    return false;
                }
            }

            InstallButton.Content =
                "ОБНОВЛЕНИЕ...";

            StatusTextBlock.Text =
                "Подготовка обновления модпака...";

            InstallProgressBar.Value = 0;
            ProgressPercentTextBlock.Text = "0%";

            var progress =
                new Progress<ModpackInstallProgress>(
                    update =>
                    {
                        int safePercent =
                            Math.Clamp(
                                update.Percent,
                                0,
                                100);

                        InstallProgressBar
                            .IsIndeterminate = false;

                        SetProgressValue(safePercent);

                        ProgressPercentTextBlock.Text =
                            $"{safePercent}%";

                        StatusTextBlock.Text =
                            $"{update.Status}\n" +
                            $"{update.CurrentFile}/" +
                            $"{update.TotalFiles}: " +
                            $"{update.FileName}";

                        ProgressDetailsTextBlock.Text =
                            BuildDownloadDetails(update);
                    });

            /*
             * Скачиваем и устанавливаем только файлы,
             * указанные в плане обновления.
             */
            await _modpackInstallerService
                .InstallUpdateAsync(
                    plan,
                    GamePathService.GameDirectory,
                    progress,
                    cancellationToken);

            InstallProgressBar.Value = 100;
            ProgressPercentTextBlock.Text = "100%";

            StatusTextBlock.Text =
                $"Модпак успешно обновлён до версии " +
                $"{plan.LatestVersion}.";

            return true;
        }

        private async Task LaunchGameAsync()
        {
            if (_isGameRunning)
            {
                return;
            }

            Window? mainWindow =
                Window.GetWindow(this);

            bool musicPaused =
                false;

            try
            {
                _isGameRunning = true;

                SetControlsEnabled(false);

                InstallButton.Content =
                    "ЗАПУСК...";

                InstallProgressBar.IsIndeterminate =
                    false;

                InstallProgressBar.Value =
                    0;

                ProgressPercentTextBlock.Text =
                    "0%";

                StatusTextBlock.Text =
                    "Подготовка Fabric-сборки к запуску...";

                string username =
                    SettingsService.Current
                        .Nickname?
                        .Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        username))
                {
                    MessageBox.Show(
                        "Сначала открой профиль и введи свой ник.",
                        "Ник не указан",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                int ramMb =
                    SettingsService.Current
                        .AllocatedRamMb;

                Process process =
                    await _minecraftLaunchService
                        .LaunchAsync(
                            username,
                            ramMb);

                InstallProgressBar.Value =
                    100;

                ProgressPercentTextBlock.Text =
                    "100%";

                StatusTextBlock.Text =
                    "Minecraft запущен.";

                bool shouldResumeMusic =
                    SettingsService.Current
                        .MusicEnabled;

                MusicService.Pause();
                musicPaused = true;

                if (mainWindow != null)
                {
                    mainWindow.Hide();
                }

                await process.WaitForExitAsync();

                if (shouldResumeMusic)
                {
                    MusicService.Resume();
                    musicPaused = false;
                }

                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }

                StatusTextBlock.Text =
                    "Minecraft закрыт. Можно запускать снова.";
            }
            catch (Exception exception)
            {
                if (musicPaused &&
                    SettingsService.Current.MusicEnabled)
                {
                    MusicService.Resume();
                }

                if (mainWindow != null &&
                    !mainWindow.IsVisible)
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }

                InstallProgressBar.IsIndeterminate =
                    false;

                StatusTextBlock.Text =
                    "Не удалось запустить Minecraft.";

                MessageBox.Show(
                    "Ошибка запуска Minecraft.\n\n" +
                    exception.Message,
                    "StoryLauncher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isGameRunning = false;

                SetControlsEnabled(true);

                UpdateInstallationState();
            }
        }

        private async void CheckFilesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isInstalling ||
                _isGameRunning)
            {
                return;
            }

            bool operationCancelled = false;

            try
            {
                SetInstallingState(true);

                CancellationToken cancellationToken =
                    BeginCancellableOperation();

                CheckFilesButton.IsEnabled =
                    false;

                StatusTextBlock.Text =
                    "Проверка Minecraft и Fabric...";

                InstallProgressBar.IsIndeterminate =
                    true;

                ProgressPercentTextBlock.Text =
                    "...";

                bool minecraftInstalled =
                    _minecraftInstallService
                        .IsVanillaInstalled();

                bool fabricInstalled =
                    _fabricInstallService
                        .IsFabricInstalled();

                if (!minecraftInstalled)
                {
                    InstallProgressBar
                        .IsIndeterminate = false;

                    InstallProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";

                    StatusTextBlock.Text =
                        "Minecraft 1.21.1 не установлен.";

                    MessageBox.Show(
                        "Minecraft 1.21.1 не найден.",
                        "Проверка файлов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (!fabricInstalled)
                {
                    InstallProgressBar
                        .IsIndeterminate = false;

                    InstallProgressBar.Value = 50;
                    ProgressPercentTextBlock.Text = "50%";

                    StatusTextBlock.Text =
                        "Fabric Loader не установлен.";

                    MessageBox.Show(
                        "Minecraft 1.21.1 установлен.\n" +
                        "Fabric Loader 0.19.3 не найден.",
                        "Проверка файлов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                bool modpackReady =
                    await EnsureModpackReadyAsync(
                        askForConfirmation: true,
                        cancellationToken:
                            cancellationToken);

                if (!modpackReady)
                {
                    return;
                }

                int modsCount =
                    CountFiles(
                        GamePathService.ModsDirectory,
                        "*.jar");

                int worldsCount =
                    CountDirectories(
                        GamePathService.SavesDirectory);

                MessageBox.Show(
                    "Проверка завершена.\n\n" +
                    "Minecraft 1.21.1: установлен\n" +
                    "Fabric Loader 0.19.3: установлен\n" +
                    $"Версия модпака: " +
                    $"{ModpackInstallStateService.Current.InstalledVersion}\n" +
                    $"Количество модов: {modsCount}\n" +
                    $"Количество миров: {worldsCount}",
                    "Проверка файлов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                operationCancelled = true;

                InstallProgressBar.IsIndeterminate = false;
                StatusTextBlock.Text =
                    "Проверка отменена пользователем.";
                ProgressDetailsTextBlock.Text =
                    "Можно запустить проверку ещё раз в любое время.";
            }
            catch (Exception exception)
            {
                InstallProgressBar.IsIndeterminate =
                    false;

                StatusTextBlock.Text =
                    "Не удалось проверить файлы.";

                MessageBox.Show(
                    "Ошибка проверки файлов.\n\n" +
                    exception.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                EndCancellableOperation();
                SetInstallingState(false);

                if (!_isGameRunning)
                {
                    CheckFilesButton.IsEnabled =
                        true;
                }

                if (!operationCancelled)
                {
                    UpdateInstallationState();
                }
            }
        }

        private void OpenGameFolderButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                GamePathService.CreateDirectories();

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            GamePathService
                                .GameDirectory,

                        UseShellExecute =
                            true
                    });
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Не удалось открыть папку игры.\n\n" +
                    exception.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void InstallService_StatusChanged(
            string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text =
                    status;
            });
        }

        private void InstallService_ProgressChanged(
            int progress)
        {
            Dispatcher.Invoke(() =>
            {
                int safeProgress =
                    Math.Clamp(
                        progress,
                        0,
                        100);

                InstallProgressBar
                    .IsIndeterminate = false;

                SetProgressValue(safeProgress);

                ProgressPercentTextBlock.Text =
                    $"{safeProgress}%";
            });
        }

        private void SetInstallingState(
            bool isInstalling)
        {
            _isInstalling =
                isInstalling;

            bool controlsEnabled =
                !isInstalling &&
                !_isGameRunning;

            SetControlsEnabled(
                controlsEnabled);

            if (isInstalling)
            {
                InstallButton.Content =
                    "ПОДОЖДИТЕ...";

                ActivitySpinner.Visibility =
                    Visibility.Visible;
            }
            else if (_operationCancellation == null)
            {
                ActivitySpinner.Visibility =
                    Visibility.Collapsed;
            }
        }

        private void SetControlsEnabled(
            bool isEnabled)
        {
            InstallButton.IsEnabled =
                isEnabled;

            CheckFilesButton.IsEnabled =
                isEnabled;

            OpenGameFolderButton.IsEnabled =
                isEnabled;
        }

        private CancellationToken BeginCancellableOperation()
        {
            EndCancellableOperation();

            _operationCancellation =
                new CancellationTokenSource();

            CancelOperationButton.IsEnabled = true;
            CancelOperationButton.Content = "ОТМЕНИТЬ";
            CancelOperationButton.Visibility =
                Visibility.Visible;

            ActivitySpinner.Visibility =
                Visibility.Visible;

            ProgressDetailsTextBlock.Text =
                "Подготовка операции...";

            return _operationCancellation.Token;
        }

        private void EndCancellableOperation()
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;

            if (CancelOperationButton != null)
            {
                CancelOperationButton.Visibility =
                    Visibility.Collapsed;
                CancelOperationButton.IsEnabled = true;
                CancelOperationButton.Content = "ОТМЕНИТЬ";
            }

            if (ActivitySpinner != null)
            {
                ActivitySpinner.Visibility =
                    Visibility.Collapsed;
            }
        }

        private void CancelOperationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_operationCancellation == null ||
                _operationCancellation.IsCancellationRequested)
            {
                return;
            }

            CancelOperationButton.IsEnabled = false;
            CancelOperationButton.Content = "ОТМЕНЯЮ...";
            StatusTextBlock.Text =
                "Останавливаю операцию безопасно...";
            ProgressDetailsTextBlock.Text =
                "Уже скачанные данные сохраняются для продолжения.";

            _operationCancellation.Cancel();
        }

        private void SetProgressValue(int percent)
        {
            int safePercent = Math.Clamp(percent, 0, 100);
            double currentValue = InstallProgressBar.Value;

            InstallProgressBar.Value = safePercent;

            var animation = new DoubleAnimation
            {
                From = currentValue,
                To = safePercent,
                Duration = TimeSpan.FromMilliseconds(220),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            InstallProgressBar.BeginAnimation(
                ProgressBar.ValueProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static string BuildDownloadDetails(
            ModpackInstallProgress update)
        {
            var parts = new List<string>();

            if (update.IsResumed)
            {
                parts.Add("Продолжение загрузки");
            }

            if (update.TotalBytes > 0)
            {
                parts.Add(
                    $"{FormatFileSize(update.DownloadedBytes)} / " +
                    $"{FormatFileSize(update.TotalBytes)}");
            }

            if (update.BytesPerSecond > 0)
            {
                parts.Add(
                    $"{FormatFileSize(update.BytesPerSecond)}/с");
            }

            if (update.EstimatedRemaining.HasValue)
            {
                parts.Add(
                    "осталось примерно " +
                    FormatDuration(
                        update.EstimatedRemaining.Value));
            }

            return parts.Count > 0
                ? string.Join(" • ", parts)
                : "Подготовка файла...";
        }

        private static string FormatRemainingTime(
            TimeSpan? remaining)
        {
            if (!remaining.HasValue ||
                remaining.Value <= TimeSpan.Zero)
            {
                return string.Empty;
            }

            return " • осталось примерно " +
                FormatDuration(remaining.Value);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours} ч " +
                    $"{duration.Minutes} мин";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{(int)duration.TotalMinutes} мин " +
                    $"{duration.Seconds} сек";
            }

            return $"{Math.Max(1, duration.Seconds)} сек";
        }

        private void UpdateDiskSpaceDisplay()
        {
            try
            {
                StorageSpaceInfo info =
                    StorageSpaceService.GetSpaceInfo(
                        GamePathService.GameDirectory,
                        0);

                DiskSpaceTextBlock.Text =
                    $"Диск {info.DriveName} • свободно " +
                    StorageSpaceService.FormatBytes(
                        info.AvailableBytes);
            }
            catch
            {
                DiskSpaceTextBlock.Text =
                    "Свободное место будет проверено перед установкой";
            }
        }

        private void UpdateInstallationState()
        {
            bool minecraftInstalled =
                _minecraftInstallService
                    .IsVanillaInstalled();

            bool fabricInstalled =
                _fabricInstallService
                    .IsFabricInstalled();

            InstallProgressBar.IsIndeterminate =
                false;

            UpdateDiskSpaceDisplay();

            if (!minecraftInstalled)
            {
                StatusTextBlock.Text =
                    "Minecraft 1.21.1 ещё не установлен.";

                InstallProgressBar.Value = 0;
                ProgressPercentTextBlock.Text = "0%";

                InstallButton.Content =
                    "УСТАНОВИТЬ MINECRAFT";

                ProgressDetailsTextBlock.Text =
                    "Для установки автоматически проверится свободное место.";

                return;
            }

            if (!fabricInstalled)
            {
                StatusTextBlock.Text =
                    "Minecraft установлен. Теперь установи Fabric.";

                InstallProgressBar.Value = 50;
                ProgressPercentTextBlock.Text = "50%";

                InstallButton.Content =
                    "УСТАНОВИТЬ FABRIC";

                ProgressDetailsTextBlock.Text =
                    "Minecraft установлен, следующий шаг — Fabric Loader.";

                return;
            }

            string installedVersion =
                ModpackInstallStateService
                    .Current
                    .InstalledVersion;

            StatusTextBlock.Text =
                "Minecraft и Fabric установлены. " +
                $"Версия модпака: {installedVersion}.";

            InstallProgressBar.Value = 100;
            ProgressPercentTextBlock.Text = "100%";

            InstallButton.Content =
                "ИГРАТЬ";

            ProgressDetailsTextBlock.Text =
                "Файлы готовы. Проверку можно запустить отдельной кнопкой.";
        }

        private static string FormatFileSize(
            long bytes)
        {
            string[] units =
            {
                "Б",
                "КБ",
                "МБ",
                "ГБ",
                "ТБ"
            };

            double size =
                bytes;

            int unitIndex =
                0;

            while (size >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return
                $"{size:0.##} {units[unitIndex]}";
        }

        private static int CountFiles(
            string directory,
            string searchPattern)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            try
            {
                return Directory.GetFiles(
                    directory,
                    searchPattern,
                    SearchOption.TopDirectoryOnly)
                    .Length;
            }
            catch
            {
                return 0;
            }
        }

        private static int CountDirectories(
            string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            try
            {
                return Directory.GetDirectories(
                    directory,
                    "*",
                    SearchOption.TopDirectoryOnly)
                    .Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
