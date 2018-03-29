using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NLog;
using YoutubeExplode;

namespace NadekoBot.Modules.Music.Common.SongResolver.Strategies
{
    public class YoutubeResolveStrategy : IResolveStrategy
    {
        private readonly Logger _log;

        public YoutubeResolveStrategy()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        public async Task<SongInfo> ResolveSong(string query)
        {
            try
            {
                SongInfo s = await ResolveWithYtExplode(query);
                if (s != null)
                    return s;
            }
            catch { }
            return await ResolveWithYtDl(query);
        }

        private async Task<SongInfo> ResolveWithYtExplode(string query)
        {
            YoutubeExplode.Models.Video video;
            var client = new YoutubeClient();
            if (!YoutubeClient.TryParseVideoId(query, out var id))
            {
                _log.Info("Searching for video");
                var videos = await client.SearchVideosAsync(query, 1);

                video = videos.FirstOrDefault();
            }
            else
            {
                _log.Info("Getting video with id");
                video = await client.GetVideoAsync(id);
            }

            if (video == null)
                return null;

            _log.Info("Video found");
            var streamInfo = await client.GetVideoMediaStreamInfosAsync(video.Id);
            var stream = streamInfo.Audio
                .OrderByDescending(x => x.Bitrate)
                .FirstOrDefault();

            _log.Info("Got stream info");

            if (stream == null)
                return null;

            return new SongInfo
            {
                Provider = "YouTube",
                ProviderType = MusicType.YouTube,
                Query = "https://youtube.com/watch?v=" + video.Id,
                Thumbnail = video.Thumbnails.MediumResUrl,
                TotalTime = video.Duration,
                Uri = async () =>
                {
                    await Task.Yield();
                    return stream.Url;
                },
                VideoId = video.Id,
                Title = video.Title,
            };
        }

        private async Task<SongInfo> ResolveWithYtDl(string query)
        {
            string[] data;
            try
            {
                using (var ytdl = new YtdlOperation())
                {
                    data = (await ytdl.GetDataAsync(query)).Split('\n');
                }

                if (data.Length < 6)
                {
                    _log.Info("No song found. Data less than 6");
                    return null;
                }

                if (!TimeSpan.TryParseExact(data[4], new[] { "ss", "m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", "hhh\\:mm\\:ss" }, CultureInfo.InvariantCulture, out var time))
                    time = TimeSpan.FromHours(24);

                return new SongInfo()
                {
                    Title = data[0],
                    VideoId = data[1],
                    Uri = async () =>
                    {
                        using (var ytdl = new YtdlOperation())
                        {
                            data = (await ytdl.GetDataAsync(query)).Split('\n');
                        }
                        if (data.Length < 6)
                        {
                            _log.Info("No song found. Data less than 6");
                            return null;
                        }
                        return data[2];
                    },
                    Thumbnail = data[3],
                    TotalTime = time,
                    Provider = "YouTube",
                    ProviderType = MusicType.YouTube,
                    Query = "https://youtube.com/watch?v=" + data[1],
                };
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                return null;
            }

        }
    }
}
