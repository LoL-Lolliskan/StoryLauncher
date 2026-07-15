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
                "StoryLauncher/0.1");
        }

        public static async Task<MinecraftPlayerSkin?> GetPlayerSkinAsync(
            string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return null;
            }

            nickname = nickname.Trim();

            string profileUrl =
                $"https://api.mojang.com/users/profiles/minecraft/" +
                Uri.EscapeDataString(nickname);

            using HttpResponseMessage profileResponse =
                await HttpClient.GetAsync(profileUrl);

            if (profileResponse.StatusCode == HttpStatusCode.NoContent ||
                profileResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            profileResponse.EnsureSuccessStatusCode();

            MojangProfileResponse? profile =
                await profileResponse.Content
                    .ReadFromJsonAsync<MojangProfileResponse>();

            if (profile == null ||
                string.IsNullOrWhiteSpace(profile.Id))
            {
                return null;
            }

            string uuid = profile.Id;

            string avatarUrl =
                $"https://crafatar.com/avatars/{uuid}" +
                "?size=128&overlay&default=MHF_Steve";

            string bodyUrl =
                $"https://crafatar.com/renders/body/{uuid}" +
                "?scale=8&overlay&default=MHF_Steve";

            BitmapImage avatar =
                await DownloadImageAsync(avatarUrl);

            BitmapImage body =
                await DownloadImageAsync(bodyUrl);

            return new MinecraftPlayerSkin
            {
                Username = profile.Name ?? nickname,
                Uuid = uuid,
                Avatar = avatar,
                BodyRender = body
            };
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