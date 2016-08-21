using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Text.RegularExpressions;
using Google.Apis.Urlshortener.v1;
using Google.Apis.Urlshortener.v1.Data;

namespace NadekoBot.Services.Impl
{
    public class GoogleApiService : IGoogleApiService
    {
        private YouTubeService yt;
        private UrlshortenerService sh;

        public GoogleApiService()
        {
            var bcs = new BaseClientService.Initializer
            {
                ApplicationName = "Nadeko Bot",
                ApiKey = NadekoBot.Credentials.GoogleApiKey
            };

            yt = new YouTubeService(bcs);
            sh = new UrlshortenerService(bcs);
        }
        public async Task<IEnumerable<string>> FindPlaylistIdsByKeywordsAsync(string keywords, int count = 1)
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

            return (await query.ExecuteAsync()).Items.Select(i => i.Id.PlaylistId);
        }

        public async Task<IEnumerable<string>> FindRelatedVideosAsync(string id, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var match = new Regex("(?:youtu\\.be\\/|v=)(?<id>[\\da-zA-Z\\-_]*)").Match(id);
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

        public async Task<IEnumerable<string>> FindVideosByKeywordsAsync(string keywords, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                throw new ArgumentNullException(nameof(keywords));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

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

            var response = await sh.Url.Insert(new Url { LongUrl = url }).ExecuteAsync();
            return response.Id;
        }
    }
}
