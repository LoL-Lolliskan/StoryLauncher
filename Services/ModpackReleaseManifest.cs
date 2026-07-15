using System;
using System.Collections.Generic;

namespace StoryLauncher.Services
{
    public sealed class ModpackReleaseManifest
    {
        public string Version { get; set; } =
            string.Empty;

        public string MinecraftVersion { get; set; } =
            string.Empty;

        public string FabricLoaderVersion { get; set; } =
            string.Empty;

        public DateTime GeneratedAtUtc { get; set; }

        public int TotalAssets { get; set; }

        public long TotalDownloadSize { get; set; }

        public List<ModpackReleaseFile> Files { get; set; } =
            new();
    }

    public sealed class ModpackReleaseFile
    {
        public string AssetName { get; set; } =
            string.Empty;

        public string DownloadUrl { get; set; } =
            string.Empty;

        public string InstallPath { get; set; } =
            string.Empty;

        public string Action { get; set; } =
            "copy";

        public string InstallMode { get; set; } =
            "overwrite";

        public long Size { get; set; }

        public string Sha256 { get; set; } =
            string.Empty;
    }
}