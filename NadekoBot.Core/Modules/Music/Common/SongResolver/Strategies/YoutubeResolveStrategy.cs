using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NLog;
using System;
using System.Globalization;
using System.Threading.Tasks;

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
            _log.Info("Getting link");
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
                TimeSpan time;
                if (!TimeSpan.TryParseExact(data[4], new[] { "ss", "m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", "hhh\\:mm\\:ss" }, CultureInfo.InvariantCulture, out time))
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
