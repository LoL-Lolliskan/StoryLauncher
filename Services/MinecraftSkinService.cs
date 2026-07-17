using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace StoryLauncher.Services
{
    public sealed class MinecraftPlayerSkin
    {
        public required string Username { get; init; }

        public required string Uuid { get; init; }

        public required string SourceName { get; init; }

        public required BitmapImage Avatar { get; init; }

        public required BitmapImage BodyRender { get; init; }
    }

    public static class MinecraftSkinService
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static MinecraftSkinService()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "StoryLauncher/0.1.3");
        }

        public static async Task<MinecraftPlayerSkin?> GetPlayerSkinAsync(
            string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return null;
            }

            nickname = nickname.Trim();

            MojangProfileResponse? officialProfile =
                await TryGetOfficialProfileAsync(nickname);

            if (officialProfile != null &&
                !string.IsNullOrWhiteSpace(officialProfile.Id))
            {
                MinecraftPlayerSkin? officialSkin =
                    await TryDownloadRenderedSkinAsync(
                        officialProfile.Name ?? nickname,
                        officialProfile.Id,
                        officialProfile.Id,
                        "Официальный Minecraft Java-скин");

                if (officialSkin != null)
                {
                    return officialSkin;
                }
            }

            // Если официальный профиль не найден, пробуем веб-рендер
            // непосредственно по введённому нику.
            return await TryDownloadRenderedSkinAsync(
                nickname,
                nickname,
                string.Empty,
                "Веб-скин MCHeads по нику");
        }

        private static async Task<MojangProfileResponse?>
            TryGetOfficialProfileAsync(string nickname)
        {
            try
            {
                string profileUrl =
                    "https://api.mojang.com/users/profiles/minecraft/" +
                    Uri.EscapeDataString(nickname);

                using HttpResponseMessage response =
                    await HttpClient.GetAsync(profileUrl);

                if (response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content
                    .ReadFromJsonAsync<MojangProfileResponse>();
            }
            catch
            {
                return null;
            }
        }

        private static async Task<MinecraftPlayerSkin?>
            TryDownloadRenderedSkinAsync(
                string username,
                string identifier,
                string uuid,
                string sourceName)
        {
            try
            {
                string safeIdentifier =
                    Uri.EscapeDataString(identifier);

                string avatarUrl =
                    $"https://mc-heads.net/avatar/{safeIdentifier}/128.png";

                // Изометрический 3D-рендер полного тела, повёрнутый вправо.
                string bodyUrl =
                    $"https://mc-heads.net/body/{safeIdentifier}/right";

                Task<BitmapImage> avatarTask =
                    DownloadImageAsync(avatarUrl);

                Task<BitmapImage> bodyTask =
                    DownloadImageAsync(bodyUrl);

                await Task.WhenAll(avatarTask, bodyTask);

                return new MinecraftPlayerSkin
                {
                    Username = username,
                    Uuid = uuid,
                    SourceName = sourceName,
                    Avatar = await avatarTask,
                    BodyRender = await bodyTask
                };
            }
            catch
            {
                return null;
            }
        }

        private static async Task<BitmapImage> DownloadImageAsync(
            string url)
        {
            byte[] imageBytes =
                await HttpClient.GetByteArrayAsync(url);

            using var memoryStream =
                new MemoryStream(imageBytes);

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private sealed class MojangProfileResponse
        {
            public string? Id { get; set; }

            public string? Name { get; set; }
        }
    }
}
