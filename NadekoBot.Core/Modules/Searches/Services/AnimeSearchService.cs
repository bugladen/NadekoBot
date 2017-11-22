using NadekoBot.Core.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using NadekoBot.Modules.Searches.Common;
using AngleSharp;
using AngleSharp.Dom.Html;
using System.Linq;

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

        public async Task<NovelResult> GetNovelData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            query = query.Replace(" ", "-");
            try
            {

                var link = "http://www.novelupdates.com/series/" + Uri.EscapeDataString(query.Replace("/", " "));
                link = link.ToLowerInvariant();
                var (ok, data) = await _cache.TryGetNovelDataAsync(link).ConfigureAwait(false);
                if (!ok)
                {
                    var config = Configuration.Default.WithDefaultLoader();
                    var document = await BrowsingContext.New(config).OpenAsync(link);

                    var imageElem = document.QuerySelector("div.seriesimg > img");
                    if (imageElem == null)
                        return null;
                    var imageUrl = ((IHtmlImageElement)imageElem).Source;

                    var descElem = document.QuerySelector("div#editdescription > p");
                    var desc = descElem.InnerHtml;

                    var genres = document.QuerySelector("div#seriesgenre").Children
                        .Select(x => x as IHtmlAnchorElement)
                        .Where(x => x != null)
                        .Select(x => $"[{x.InnerHtml}]({x.Href})")
                        .ToArray();

                    var authors = document
                        .QuerySelector("div#showauthors")
                        .Children
                        .Select(x => x as IHtmlAnchorElement)
                        .Where(x => x != null)
                        .Select(x => $"[{x.InnerHtml}]({x.Href})")
                        .ToArray();

                    var score = ((IHtmlSpanElement)document
                        .QuerySelector("h5.seriesother > span.uvotes"))
                        .InnerHtml;

                    var status = document
                        .QuerySelector("div#editstatus")
                        .InnerHtml;
                    var title = document
                        .QuerySelector("div.w-blog-content > div.seriestitlenu")
                        .InnerHtml;

                    var obj = new NovelResult()
                    {
                        Description = desc,
                        Authors = authors,
                        Genres = genres,
                        ImageUrl = imageUrl,
                        Link = link,
                        Score = score,
                        Status = status,
                        Title = title,
                    };

                    await _cache.SetNovelDataAsync(link,
                        JsonConvert.SerializeObject(obj)).ConfigureAwait(false);

                    return obj;
                }
                
                return JsonConvert.DeserializeObject<NovelResult>(data);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
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
