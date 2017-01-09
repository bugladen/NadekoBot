using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Text.RegularExpressions;
using Google.Apis.Urlshortener.v1;
using Google.Apis.Urlshortener.v1.Data;
using NLog;

namespace NadekoBot.Services.Impl
{
    public class GoogleApiService : IGoogleApiService
    {
        private YouTubeService yt;
        private UrlshortenerService sh;
        private Logger _log { get; }

        public GoogleApiService()
        {
            var bcs = new BaseClientService.Initializer
            {
                ApplicationName = "Nadeko Bot",
                ApiKey = NadekoBot.Credentials.GoogleApiKey
            };

            _log = LogManager.GetCurrentClassLogger();

            yt = new YouTubeService(bcs);
            sh = new UrlshortenerService(bcs);
        }
        public async Task<IEnumerable<string>> GetPlaylistIdsByKeywordsAsync(string keywords, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                throw new ArgumentNullException(nameof(keywords));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var match = new Regex("(?:youtu\\.be\\/|list=)(?<id>[\\da-zA-Z\\-_]*)").Match(keywords);
            if (match.Length > 1)
            {
                return new[] { match.Groups["id"].Value.ToString() };
            }
            var query = yt.Search.List("snippet");
            query.MaxResults = count;
            query.Type = "playlist";
            query.Q = keywords;

            return (await query.ExecuteAsync()).Items.Select(i => i.Id.PlaylistId);
        }

        private readonly Regex YtVideoIdRegex = new Regex("(?:youtu\\.be\\/|v\\/|u\\/\\w\\/|embed\\/|watch\\?v=|\\&v=)(?<id>[^#\\&\\?]*)", RegexOptions.Compiled);

        public async Task<IEnumerable<string>> GetRelatedVideosAsync(string id, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var match = YtVideoIdRegex.Match(id);
            if (match.Length > 1)
            {
                id = match.Groups["id"].Value;
            }
            var query = yt.Search.List("snippet");
            query.MaxResults = count;
            query.RelatedToVideoId = id;
            query.Type = "video";
            return (await query.ExecuteAsync()).Items.Select(i => "http://www.youtube.com/watch?v=" + i.Id.VideoId);
        }

        public async Task<IEnumerable<string>> GetVideosByKeywordsAsync(string keywords, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                throw new ArgumentNullException(nameof(keywords));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            string id = "";
            var match = YtVideoIdRegex.Match(keywords);
            if (match.Length > 1)
            {
                id = match.Groups["id"].Value;
            }
            if (!string.IsNullOrWhiteSpace(id))
            {
                return new[] { "http://www.youtube.com/watch?v=" + id };
            }
            var query = yt.Search.List("snippet");
            query.MaxResults = count;
            query.Q = keywords;
            query.Type = "video";
            return (await query.ExecuteAsync()).Items.Select(i => "http://www.youtube.com/watch?v=" + i.Id.VideoId);
        }

        public async Task<string> ShortenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.GoogleApiKey))
                return url;

            try
            {
                var response = await sh.Url.Insert(new Url { LongUrl = url }).ExecuteAsync();
                return response.Id;
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                return url;
            }
        }

        public async Task<IEnumerable<string>> GetPlaylistTracksAsync(string playlistId, int count = 50)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentNullException(nameof(playlistId));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            string nextPageToken = null;

            List<string> toReturn = new List<string>(count);

            do
            {
                var toGet = count > 50 ? 50 : count;
                count -= toGet;

                var query = yt.PlaylistItems.List("contentDetails");
                query.MaxResults = toGet;
                query.PlaylistId = playlistId;
                query.PageToken = nextPageToken;

                var data = await query.ExecuteAsync();

                toReturn.AddRange(data.Items.Select(i => i.ContentDetails.VideoId));
                nextPageToken = data.NextPageToken;
            }
            while (count > 0 && !string.IsNullOrWhiteSpace(nextPageToken));

            return toReturn;
        }
        //todo AsyncEnumerable
        public async Task<IReadOnlyDictionary<string,TimeSpan>> GetVideoDurationsAsync(IEnumerable<string> videoIds)
        {
            var videoIdsList = videoIds as List<string> ?? videoIds.ToList();

            Dictionary<string, TimeSpan> toReturn = new Dictionary<string, TimeSpan>();

            if (!videoIdsList.Any())
                return toReturn;
            var toGet = 0;
            var remaining = videoIdsList.Count;

            do
            {
                toGet = remaining > 50 ? 50 : remaining;
                remaining -= toGet;

                var q = yt.Videos.List("contentDetails");
                q.Id = string.Join(",", videoIdsList.Take(toGet));
                videoIdsList = videoIdsList.Skip(toGet).ToList();
                var items = (await q.ExecuteAsync().ConfigureAwait(false)).Items;
                foreach (var i in items)
                {
                    toReturn.Add(i.Id, System.Xml.XmlConvert.ToTimeSpan(i.ContentDetails.Duration));
                }
            }
            while (remaining > 0);

            return toReturn;
        }
    }
}
