using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace StoryLauncher.Views
{
    public partial class HomePage : Page
    {
        private sealed record NewsSlide(
            string Date,
            string Title,
            string Summary,
            string Details,
            string ImagePath,
            string SourceUrl);

        private readonly List<NewsSlide> _slides = new()
        {
            new("ИЮЛЬ 2026", "Chaos Cubed уже вышло",
                "Серные пещеры, необычные кубы и новые возможности для строительства.",
                "Летнее игровое обновление Chaos Cubed вышло для Minecraft. " +
                "Оно добавляет подземный биом серных пещер. " +
                "В пещерах встречаются серные бассейны, киноварь и острые серные образования. " +
                "Новый серный куб умеет поглощать блоки. " +
                "После поглощения меняются его скорость, отскок, трение и другие свойства. " +
                "Игроки получили новые наборы блоков из серы и киновари. " +
                "Сильная сера позволяет создавать пузырящиеся бассейны и гейзеры. " +
                "В обновлении появился новый музыкальный диск и дополнительные фоновые композиции. " +
                "Содержимое Chaos Cubed доступно в Java Edition и Bedrock Edition. " +
                "Обновление бесплатно для владельцев игры.",
                "/StoryLauncher;component/Assets/Images/NewsChaosCubed.png",
                "https://www.minecraft.net/en-us/updates/chaos-cubed-drop"),

            new("16 ИЮНЯ 2026", "Minecraft Java Edition 26.2",
                "Крупный выпуск Java Edition с Chaos Cubed, списком друзей и Vulkan.",
                "Minecraft Java Edition 26.2 вышла 16 июня 2026 года. " +
                "В неё вошёл весь набор возможностей Chaos Cubed. " +
                "В игру добавили серного куба и новый биом серных пещер. " +
                "Появились строительные блоки из серы и киновари. " +
                "Также добавлены серные шипы и блок сильной серы. " +
                "Java Edition получила встроенный список друзей. " +
                "В настройках появились связанные с ним параметры сетевой игры. " +
                "Разработчики добавили экспериментальный вариант рендеринга через Vulkan. " +
                "Версия включает новый музыкальный диск и пять фоновых композиций. " +
                "Помимо новых функций выпуск содержит технические изменения и исправления ошибок.",
                "/StoryLauncher;component/Assets/Images/NewsJava262.png",
                "https://www.minecraft.net/en-us/article/minecraft-java-edition-26-2"),

            new("7 ИЮЛЯ 2026", "Minecraft 26.3 Snapshot 3",
                "Подушки, одноразовая соломенная кровать и новые технические возможности.",
                "Третий снимок Minecraft 26.3 опубликован 7 июля 2026 года. " +
                "Главной новинкой стала подушка, на которую игрок может сесть. " +
                "Подушки доступны в шестнадцати цветах. " +
                "Их можно размещать на ровной поверхности. " +
                "Вторая новинка — соломенная кровать для путешествий. " +
                "Она позволяет пропустить ночь без изменения точки возрождения. " +
                "После одного использования такая кровать исчезает. " +
                "Из трёх тюков сена создаются четыре соломенные кровати. " +
                "Снимок также расширяет возможности пользовательских рецептов зелий и визуальных эффектов. " +
                "Эти функции пока находятся в тестировании и могут измениться до выпуска.",
                "/StoryLauncher;component/Assets/Images/NewsSnapshot263.png",
                "https://www.minecraft.net/en-us/article/minecraft-26-3-snapshot-3"),

            new("23 ИЮНЯ 2026", "Новый пятнистый лес в тестировании",
                "В Bedrock Preview появился новый лес, тополя и заброшенные лагеря.",
                "Minecraft Preview 26.40.27 опубликована 23 июня 2026 года. " +
                "В тестовую версию добавили биом пятнистого леса. " +
                "Он формируется рядом с холодными биомами. " +
                "В нём растут новые тополя и встречаются поваленные деревья. " +
                "На стволах можно найти полочные грибы. " +
                "Ландшафт дополняют красные кустарники. " +
                "В разных биомах теперь могут появляться заброшенные лагеря. " +
                "Внешний вид лагеря зависит от места, где он сформировался. " +
                "В сундуках и бочках лагеря можно обнаружить добычу. " +
                "Пока эти возможности экспериментальные и доступны для проверки в Bedrock Preview.",
                "/StoryLauncher;component/Assets/Images/NewsDappledForest.png",
                "https://www.minecraft.net/en-us/article/minecraft-preview-26-40-27"),

            new("30 МАЯ 2026", "Minecraft LIVE на TwitchCon 2026",
                "Итоги дополнительной трансляции с Chaos Cubed и будущим лесным биомом.",
                "Дополнительный выпуск Minecraft LIVE прошёл на TwitchCon Europe. " +
                "Официальное резюме трансляции опубликовано 30 мая 2026 года. " +
                "Разработчики подробно показали обновление Chaos Cubed. " +
                "Они рассказали о серных кубах и различных вариантах их физики. " +
                "Зрителям также показали серные пещеры. " +
                "В эфире впервые представили пятнистый лес из следующего игрового обновления. " +
                "В этом биоме растут тополя и появляются таинственные заброшенные лагеря. " +
                "Команда поделилась идеями оформления построек из новых материалов. " +
                "В трансляции показали ранние материалы Minecraft Dungeons II. " +
                "Выпуск объединил новости основной игры и других проектов вселенной Minecraft.",
                "/StoryLauncher;component/Assets/Images/NewsMinecraftLive.png",
                "https://www.minecraft.net/en-us/article/minecraft-live-twitchcon-rotterdam-recap")
        };

        private readonly DispatcherTimer _timer;
        private int _currentIndex;
        private bool _isChanging;

        public HomePage()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += (_, _) => ChangeSlide(1);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BuildDots();
            ShowCurrentSlide(false);
            PlayPageAnimation();
            _timer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        private void PreviousNewsButton_Click(object sender, RoutedEventArgs e) => ChangeSlide(-1);
        private void NextNewsButton_Click(object sender, RoutedEventArgs e) => ChangeSlide(1);

        private void ChangeSlide(int direction)
        {
            if (_isChanging)
            {
                return;
            }

            _currentIndex = (_currentIndex + direction + _slides.Count) % _slides.Count;
            ShowCurrentSlide(true);
            _timer.Stop();
            _timer.Start();
        }

        private void ShowCurrentSlide(bool animate)
        {
            NewsSlide slide = _slides[_currentIndex];

            void ApplySlide()
            {
                NewsImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(slide.ImagePath, UriKind.Relative));
                NewsDateText.Text = slide.Date;
                NewsTitleText.Text = slide.Title;
                NewsSummaryText.Text = slide.Summary;
                NewsDetailsTitleText.Text = slide.Title;
                NewsDetailsText.Text = slide.Details;
                OfficialNewsButton.Tag = slide.SourceUrl;
                UpdateDots();
            }

            if (!animate)
            {
                ApplySlide();
                return;
            }

            _isChanging = true;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            fadeOut.Completed += (_, _) =>
            {
                ApplySlide();
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                fadeIn.Completed += (_, _) => _isChanging = false;
                SlideContainer.BeginAnimation(OpacityProperty, fadeIn);
            };
            SlideContainer.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void OfficialNewsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? url = OfficialNewsButton.Tag as string;

            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void BuildDots()
        {
            NewsDotsPanel.Children.Clear();
            for (int i = 0; i < _slides.Count; i++)
            {
                int index = i;
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
                    Template = CreateDotTemplate()
                };
                dot.Click += (_, _) =>
                {
                    if (_currentIndex == index) return;
                    _currentIndex = index;
                    ShowCurrentSlide(true);
                    _timer.Stop();
                    _timer.Start();
                };
                NewsDotsPanel.Children.Add(dot);
            }
        }

        private static ControlTemplate CreateDotTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty,
                new System.Windows.TemplateBindingExtension(BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            template.VisualTree = border;
            return template;
        }

        private void UpdateDots()
        {
            for (int i = 0; i < NewsDotsPanel.Children.Count; i++)
            {
                if (NewsDotsPanel.Children[i] is Button dot)
                {
                    dot.Opacity = i == _currentIndex ? 1 : 0.35;
                    dot.Width = i == _currentIndex ? 24 : 9;
                    dot.Background = i == _currentIndex
                        ? (Application.Current.TryFindResource(
                            "PrimaryBrush") as Brush
                            ?? Brushes.MediumPurple)
                        : Brushes.White;
                }
            }
        }

        private void PlayPageAnimation()
        {
            BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350)));

            if (RootGrid.RenderTransform is TranslateTransform transform)
            {
                transform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
            }
        }
    }
}
