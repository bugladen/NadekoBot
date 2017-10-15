using NadekoBot.Core.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using NadekoBot.Modules.Searches.Common;

namespace NadekoBot.Modules.Searches.Services
{
    public class AnimeSearchService : INService
    {
        private readonly Logger _log;
        private readonly IDataCache _cache;
        private readonly HttpClient _http;

        public AnimeSearchService(IDataCache cache)
        {
            _log = LogManager.GetCurrentClassLogger();
            _cache = cache;
            _http = new HttpClient();
        }

        public async Task<AnimeResult> GetAnimeData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            try
            {

                var link = "https://aniapi.nadekobot.me/anime/" + Uri.EscapeDataString(query.Replace("/", " "));
                link = link.ToLowerInvariant();
                var (ok, data) = await _cache.TryGetAnimeDataAsync(link).ConfigureAwait(false);
                if (!ok)
                {
                    data = await _http.GetStringAsync(link).ConfigureAwait(false);
                    await _cache.SetAnimeDataAsync(link, data).ConfigureAwait(false);
                }


                return JsonConvert.DeserializeObject<AnimeResult>(data);
            }
            catch
            {
                return null;
            }
        }

        public async Task<MangaResult> GetMangaData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            try
            {

                 var link = "https://aniapi.nadekobot.me/manga/" + Uri.EscapeDataString(query.Replace("/", " "));
                link = link.ToLowerInvariant();
                var (ok, data) = await _cache.TryGetAnimeDataAsync(link).ConfigureAwait(false);
                if (!ok)
                {
                    data = await _http.GetStringAsync(link).ConfigureAwait(false);
                    await _cache.SetAnimeDataAsync(link, data).ConfigureAwait(false);
                }


                return JsonConvert.DeserializeObject<MangaResult>(data);
            }
            catch
            {
                return null;
            }
        }
    }
}
