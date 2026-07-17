using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StoryLauncher.Services
{
    public static class ProfileImageService
    {
        private static readonly string ProfileFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "StoryLauncher",
                "Profile");

        public static string CustomAvatarPath =>
            Path.Combine(ProfileFolder, "custom-avatar.png");

        public static string CustomSkinPath =>
            Path.Combine(ProfileFolder, "custom-skin.png");

        public static string MinecraftAvatarPath =>
            Path.Combine(ProfileFolder, "minecraft-avatar.png");

        public static BitmapImage LoadBitmap(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        public static string SaveCustomAvatar(string sourcePath)
        {
            BitmapImage source = LoadBitmap(sourcePath);

            if (source.PixelWidth < 16 || source.PixelHeight < 16)
            {
                throw new InvalidOperationException(
                    "Аватарка слишком маленькая. Выбери изображение не меньше 16×16 пикселей.");
            }

            SaveAsPng(source, CustomAvatarPath);
            return CustomAvatarPath;
        }

        public static string SaveCustomSkin(string sourcePath)
        {
            BitmapImage source = LoadBitmap(sourcePath);

            bool modernSkin =
                source.PixelWidth == 64 &&
                source.PixelHeight == 64;

            bool legacySkin =
                source.PixelWidth == 64 &&
                source.PixelHeight == 32;

            if (!modernSkin && !legacySkin)
            {
                throw new InvalidOperationException(
                    "Нужна Minecraft-развёртка размером 64×64 или старого формата 64×32 пикселя. Формат файла может быть PNG, JPG, BMP, GIF или TIFF.");
            }

            SaveAsPng(source, CustomSkinPath);
            return CustomSkinPath;
        }

        public static void SaveMinecraftAvatar(BitmapSource source)
        {
            SaveAsPng(source, MinecraftAvatarPath);
        }

        public static BitmapSource RenderHeadFromSkin(BitmapSource skin)
        {
            ValidateSkin(skin);

            const int scale = 16;
            var target = new RenderTargetBitmap(
                8 * scale,
                8 * scale,
                96,
                96,
                PixelFormats.Pbgra32);

            var visual = new DrawingVisual();

            using (DrawingContext drawing = visual.RenderOpen())
            {
                DrawSkinPart(
                    drawing,
                    skin,
                    new Int32Rect(8, 8, 8, 8),
                    new Rect(0, 0, 8 * scale, 8 * scale));

                DrawSkinPart(
                    drawing,
                    skin,
                    new Int32Rect(40, 8, 8, 8),
                    new Rect(0, 0, 8 * scale, 8 * scale));
            }

            target.Render(visual);
            target.Freeze();
            return target;
        }

        public static BitmapSource RenderBodyFromSkin(BitmapSource skin)
        {
            ValidateSkin(skin);

            bool legacySkin = skin.PixelHeight == 32;

            const int scale = 10;
            var target = new RenderTargetBitmap(
                16 * scale,
                32 * scale,
                96,
                96,
                PixelFormats.Pbgra32);

            var visual = new DrawingVisual();

            using (DrawingContext drawing = visual.RenderOpen())
            {
                // Голова и шлем.
                DrawLayeredPart(drawing, skin,
                    new Int32Rect(8, 8, 8, 8),
                    new Int32Rect(40, 8, 8, 8),
                    new Rect(4 * scale, 0, 8 * scale, 8 * scale));

                if (legacySkin)
                {
                    // У старого формата нет отдельных левых конечностей
                    // и дополнительных слоёв — повторяем правые.
                    DrawSkinPart(drawing, skin,
                        new Int32Rect(20, 20, 8, 12),
                        new Rect(4 * scale, 8 * scale, 8 * scale, 12 * scale));

                    DrawSkinPart(drawing, skin,
                        new Int32Rect(44, 20, 4, 12),
                        new Rect(0, 8 * scale, 4 * scale, 12 * scale));

                    DrawSkinPart(drawing, skin,
                        new Int32Rect(44, 20, 4, 12),
                        new Rect(12 * scale, 8 * scale, 4 * scale, 12 * scale));

                    DrawSkinPart(drawing, skin,
                        new Int32Rect(4, 20, 4, 12),
                        new Rect(4 * scale, 20 * scale, 4 * scale, 12 * scale));

                    DrawSkinPart(drawing, skin,
                        new Int32Rect(4, 20, 4, 12),
                        new Rect(8 * scale, 20 * scale, 4 * scale, 12 * scale));
                }
                else
                {
                    // Тело и куртка.
                    DrawLayeredPart(drawing, skin,
                        new Int32Rect(20, 20, 8, 12),
                        new Int32Rect(20, 36, 8, 12),
                        new Rect(4 * scale, 8 * scale, 8 * scale, 12 * scale));

                    // Правая рука.
                    DrawLayeredPart(drawing, skin,
                        new Int32Rect(44, 20, 4, 12),
                        new Int32Rect(44, 36, 4, 12),
                        new Rect(0, 8 * scale, 4 * scale, 12 * scale));

                    // Левая рука.
                    DrawLayeredPart(drawing, skin,
                        new Int32Rect(36, 52, 4, 12),
                        new Int32Rect(52, 52, 4, 12),
                        new Rect(12 * scale, 8 * scale, 4 * scale, 12 * scale));

                    // Правая нога.
                    DrawLayeredPart(drawing, skin,
                        new Int32Rect(4, 20, 4, 12),
                        new Int32Rect(4, 36, 4, 12),
                        new Rect(4 * scale, 20 * scale, 4 * scale, 12 * scale));

                    // Левая нога.
                    DrawLayeredPart(drawing, skin,
                        new Int32Rect(20, 52, 4, 12),
                        new Int32Rect(4, 52, 4, 12),
                        new Rect(8 * scale, 20 * scale, 4 * scale, 12 * scale));
                }
            }

            target.Render(visual);
            target.Freeze();
            return target;
        }

        private static void DrawLayeredPart(
            DrawingContext drawing,
            BitmapSource skin,
            Int32Rect basePart,
            Int32Rect overlayPart,
            Rect destination)
        {
            DrawSkinPart(drawing, skin, basePart, destination);
            DrawSkinPart(drawing, skin, overlayPart, destination);
        }

        private static void DrawSkinPart(
            DrawingContext drawing,
            BitmapSource skin,
            Int32Rect source,
            Rect destination)
        {
            var cropped = new CroppedBitmap(skin, source);
            cropped.Freeze();

            RenderOptions.SetBitmapScalingMode(
                cropped,
                BitmapScalingMode.NearestNeighbor);

            drawing.DrawImage(cropped, destination);
        }

        private static void ValidateSkin(BitmapSource skin)
        {
            bool valid =
                skin.PixelWidth == 64 &&
                (skin.PixelHeight == 64 || skin.PixelHeight == 32);

            if (!valid)
            {
                throw new InvalidOperationException(
                    "Скин должен иметь размер 64×64 или 64×32 пикселя.");
            }
        }

        private static void SaveAsPng(
            BitmapSource source,
            string destinationPath)
        {
            Directory.CreateDirectory(ProfileFolder);

            string temporaryPath = destinationPath + ".tmp";
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = File.Create(temporaryPath))
            {
                encoder.Save(stream);
            }

            File.Move(
                temporaryPath,
                destinationPath,
                overwrite: true);
        }
    }
}
