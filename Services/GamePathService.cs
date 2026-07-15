using System;
using System.IO;

namespace StoryLauncher.Services
{
    public static class GamePathService
    {
        public static string LauncherDirectory =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "StoryLauncher");

        public static string GameDirectory =>
            Path.Combine(LauncherDirectory, "Game");

        public static string ModsDirectory =>
            Path.Combine(GameDirectory, "mods");

        public static string ConfigDirectory =>
            Path.Combine(GameDirectory, "config");

        public static string ResourcePacksDirectory =>
            Path.Combine(GameDirectory, "resourcepacks");

        public static string ShaderPacksDirectory =>
            Path.Combine(GameDirectory, "shaderpacks");

        public static string SavesDirectory =>
            Path.Combine(GameDirectory, "saves");

        public static void CreateDirectories()
        {
            Directory.CreateDirectory(LauncherDirectory);
            Directory.CreateDirectory(GameDirectory);
            Directory.CreateDirectory(ModsDirectory);
            Directory.CreateDirectory(ConfigDirectory);
            Directory.CreateDirectory(ResourcePacksDirectory);
            Directory.CreateDirectory(ShaderPacksDirectory);
            Directory.CreateDirectory(SavesDirectory);
        }
    }
}