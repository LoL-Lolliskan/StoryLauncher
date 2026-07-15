using System.Collections.Generic;
using System.Linq;

namespace StoryLauncher.Services
{
    /// <summary>
    /// Один файл, который требуется скачать и установить.
    /// </summary>
    public sealed class ModpackUpdatePlanItem
    {
        public ModpackReleaseFile File { get; set; } =
            new();

        public string Reason { get; set; } =
            string.Empty;
    }

    /// <summary>
    /// Итог проверки установленного модпака.
    /// </summary>
    public sealed class ModpackUpdatePlan
    {
        public string InstalledVersion { get; set; } =
            "0.0.0";

        public string LatestVersion { get; set; } =
            string.Empty;

        public List<ModpackUpdatePlanItem> FilesToDownload
        {
            get;
            set;
        } = new();

        public List<string> FilesToDelete
        {
            get;
            set;
        } = new();

        public int DownloadFileCount =>
            FilesToDownload.Count;

        public long DownloadSize =>
            FilesToDownload.Sum(
                item => item.File.Size);

        public bool HasChanges =>
            FilesToDownload.Count > 0 ||
            FilesToDelete.Count > 0 ||
            InstalledVersion != LatestVersion;
    }
}