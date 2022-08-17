using AngleSharp;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;
using NadekoBot.Core.Modules.Searches.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Searches.Services
{
    public class SearchesService : INService, IUnloadableService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly DiscordSocketClient _client;
        private readonly IGoogleApiService _google;
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly IImageCache _imgs;
        private readonly IDataCache _cache;
        private readonly FontProvider _fonts;
        private readonly IBotCredentials _creds;
        private readonly NadekoRandom _rng;

        public ConcurrentDictionary<ulong, bool> TranslatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
        // (userId, channelId)
        public ConcurrentDictionary<(ulong UserId, ulong ChannelId), string> UserLanguages { get; } = new ConcurrentDictionary<(ulong, ulong), string>();

        public List<WoWJoke> WowJokes { get; } = new List<WoWJoke>();
        public List<MagicItem> MagicItems { get; } = new List<MagicItem>();

        private readonly ConcurrentDictionary<ulong, SearchImageCacher> _imageCacher = new ConcurrentDictionary<ulong, SearchImageCacher>();

        public ConcurrentDictionary<ulong, Timer> AutoHentaiTimers { get; } = new ConcurrentDictionary<ulong, Timer>();
        public ConcurrentDictionary<ulong, Timer> AutoBoobTimers { get; } = new ConcurrentDictionary<ulong, Timer>();
        public ConcurrentDictionary<ulong, Timer> AutoButtTimers { get; } = new ConcurrentDictionary<ulong, Timer>();

        private readonly ConcurrentDictionary<ulong, HashSet<string>> _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>();

        public SearchesService(DiscordSocketClient client, IGoogleApiService google,
            DbService db, NadekoBot bot, IDataCache cache, IHttpClientFactory factory,
            FontProvider fonts, IBotCredentials creds)
        {
            _httpFactory = factory;
            _client = client;
            _google = google;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _imgs = cache.LocalImages;
            _cache = cache;
            _fonts = fonts;
            _creds = creds;
            _rng = new NadekoRandom();

            _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>(
                bot.AllGuildConfigs.ToDictionary(
                    x => x.GuildId,
                    x => new HashSet<string>(x.NsfwBlacklistedTags.Select(y => y.Tag))));

            //translate commands
            _client.MessageReceived += (msg) =>
            {
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!(msg is SocketUserMessage umsg))
                            return;

                        if (!TranslatedChannels.TryGetValue(umsg.Channel.Id, out var autoDelete))
                            return;

                        var key = (umsg.Author.Id, umsg.Channel.Id);

                        if (!UserLanguages.TryGetValue(key, out string langs))
                            return;

                        var text = await Translate(langs, umsg.Resolve(TagHandling.Ignore))
                                            .ConfigureAwait(false);
                        if (autoDelete)
                            try { await umsg.DeleteAsync().ConfigureAwait(false); } catch { }
                        await umsg.Channel.SendConfirmAsync($"{umsg.Author.Mention} `:` "
                            + text.Replace("<@ ", "<@", StringComparison.InvariantCulture)
                                  .Replace("<@! ", "<@!", StringComparison.InvariantCulture)).ConfigureAwait(false);
                    }
                    catch { }
                });
                return Task.CompletedTask;
            };

            //joke commands
            if (File.Exists("data/wowjokes.json"))
            {
                WowJokes = JsonConvert.DeserializeObject<List<WoWJoke>>(File.ReadAllText("data/wowjokes.json"));
            }
            else
                _log.Warn("data/wowjokes.json is missing. WOW Jokes are not loaded.");

            if (File.Exists("data/magicitems.json"))
            {
                MagicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
            }
            else
                _log.Warn("data/magicitems.json is missing. Magic items are not loaded.");
        }

        public async Task<Stream> GetRipPictureAsync(string text, Uri imgUrl)
        {
            byte[] data = await _cache.GetOrAddCachedDataAsync($"nadeko_rip_{text}_{imgUrl}",
                GetRipPictureFactory,
                (text, imgUrl),
                TimeSpan.FromDays(1)).ConfigureAwait(false);

            return data.ToStream();
        }

        public async Task<byte[]> GetRipPictureFactory((string text, Uri imgUrl) arg)
        {
            var (text, imgUrl) = arg;
            var (succ, data) = await _cache.TryGetImageDataAsync(imgUrl).ConfigureAwait(false);
            if (!succ)
            {
                using (var http = _httpFactory.CreateClient())
                using (var temp = await http.GetAsync(imgUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (!temp.IsImage())
                    {
                        data = null;
                    }
                    else
                    {
                        var imgData = await temp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        using (var tempDraw = Image.Load(imgData))
                        {
                            tempDraw.Mutate(x => x.Resize(69, 70));
                            tempDraw.ApplyRoundedCorners(35);
                            using (var tds = tempDraw.ToStream())
                            {
                                data = tds.ToArray();
                            }
                        }
                    }
                }
                await _cache.SetImageDataAsync(imgUrl, data).ConfigureAwait(false);
            }
            using (var bg = Image.Load(_imgs.Rip.ToArray()))
            {
                //avatar 82, 139
                if (data != null)
                {
                    using (var avatar = Image.Load(data))
                    {
                        avatar.Mutate(x => x.Resize(85, 85));
                        bg.Mutate(x => x
                            .DrawImage(GraphicsOptions.Default,
                                avatar,
                                new Point(82, 139)));
                    }
                }
                //text 63, 241
                bg.Mutate(x => x.DrawText(
                    new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        WrapTextWidth = 190,
                    },
                    text,
                    _fonts.NotoSans.CreateFont(20, FontStyle.Bold),
                    Rgba32.Black,
                    new PointF(25, 225)));

                //flowa
                using (var flowers = Image.Load(_imgs.RipOverlay.ToArray()))
                {
                    bg.Mutate(x => x.DrawImage(GraphicsOptions.Default,
                        flowers,
                        new Point(0, 0)));
                }

                return bg.ToStream().ToArray();
            }
        }

        public Task<WeatherData> GetWeatherDataAsync(string query)
        {
            query = query.Trim().ToLowerInvariant();

            return _cache.GetOrAddCachedDataAsync($"nadeko_weather_{query}",
                GetWeatherDataFactory,
                query,
                expiry: TimeSpan.FromHours(3));
        }

        private async Task<WeatherData> GetWeatherDataFactory(string query)
        {
            using (var http = _httpFactory.CreateClient())
            {
                try
                {
                    var data = await http.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?" +
                        $"q={query}&" +
                        $"appid=42cd627dd60debf25a5739e50a217d74&" +
                        $"units=metric").ConfigureAwait(false);

                    if (data == null)
                        return null;

                    return JsonConvert.DeserializeObject<WeatherData>(data);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex.Message);
                    return null;
                }
            }
        }

        public Task<TimeData> GetTimeDataAsync(string arg)
        {
            return _cache.GetOrAddCachedDataAsync($"nadeko_time_{arg}",
                GetTimeDataFactory,
                arg,
                TimeSpan.FromMinutes(5));
        }

        private async Task<TimeData> GetTimeDataFactory(string arg)
        {
            try
            {
                using (var http = _httpFactory.CreateClient())
                {
                    var res = await http.GetStringAsync($"https://maps.googleapis.com/maps/api/geocode/json?address={arg}&key={_creds.GoogleApiKey}").ConfigureAwait(false);
                    var obj = JsonConvert.DeserializeObject<GeolocationResult>(res);
                    if (obj?.Results == null || obj.Results.Length == 0)
                    {
                        _log.Warn("Geocode lookup failed for {0}", arg);
                        return null;
                    }
                    var currentSeconds = DateTime.UtcNow.UnixTimestamp();
                    var timeRes = await http.GetStringAsync($"https://maps.googleapis.com/maps/api/timezone/json?location={obj.Results[0].Geometry.Location.Lat},{obj.Results[0].Geometry.Location.Lng}&timestamp={currentSeconds}&key={_creds.GoogleApiKey}").ConfigureAwait(false);

                    var timeObj = JsonConvert.DeserializeObject<TimeZoneResult>(timeRes);

                    var time = DateTime.UtcNow.AddSeconds(timeObj.DstOffset + timeObj.RawOffset);

                    var toReturn = new TimeData
                    {
                        Address = obj.Results[0].FormattedAddress,
                        Time = time,
                        TimeZoneName = timeObj.TimeZoneName,
                    };

                    return toReturn;
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                return null;
            }
        }

        public enum ImageTag
        {
            Food,
            Dogs,
            Cats,
            Birds
        }

        public string GetRandomImageUrl(ImageTag tag)
        {
            var subpath = tag.ToString().ToLowerInvariant();

            int max;
            switch (tag)
            {
                case ImageTag.Food:
                    max = 773;
                    break;
                case ImageTag.Dogs:
                    max = 750;
                    break;
                case ImageTag.Cats:
                    max = 773;
                    break;
                case ImageTag.Birds:
                    max = 578;
                    break;
                default:
                    max = 100;
                    break;
            }

            return $"https://nadeko-pictures.nyc3.digitaloceanspaces.com/{subpath}/" +
                _rng.Next(1, max).ToString("000") + ".png";
        }

        public async Task<string> Translate(string langs, string text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text is empty or null", nameof(text));
            var langarr = langs.ToLowerInvariant().Split('>');
            if (langarr.Length != 2)
                throw new ArgumentException("Langs does not have 2 parts separated by a >", nameof(langs));
            var from = langarr[0];
            var to = langarr[1];
            text = text?.Trim();
            return (await _google.Translate(text, from, to).ConfigureAwait(false)).SanitizeMentions();
        }

        public Task<ImageCacherObject> DapiSearch(string tag, DapiSearchType type, ulong? guild, bool isExplicit = false)
        {
            tag = tag ?? "";
            if (string.IsNullOrWhiteSpace(tag)
                && (tag.Contains("loli") || tag.Contains("shota")))
            {
                return null;
            }

            var tags = tag
                .Split('+')
                .Select(x => x.ToLowerInvariant().Replace(' ', '_'))
                .ToArray();

            if (guild.HasValue)
            {
                var blacklistedTags = GetBlacklistedTags(guild.Value);

                var cacher = _imageCacher.GetOrAdd(guild.Value, (key) => new SearchImageCacher(_httpFactory));

                return cacher.GetImage(tags, isExplicit, type, blacklistedTags);
            }
            else
            {
                var cacher = _imageCacher.GetOrAdd(guild ?? 0, (key) => new SearchImageCacher(_httpFactory));

                return cacher.GetImage(tags, isExplicit, type);
            }
        }

        public HashSet<string> GetBlacklistedTags(ulong guildId)
        {
            if (_blacklistedTags.TryGetValue(guildId, out var tags))
                return tags;
            return new HashSet<string>();
        }

        public bool ToggleBlacklistedTag(ulong guildId, string tag)
        {
            var tagObj = new NsfwBlacklitedTag
            {
                Tag = tag
            };

            bool added;
            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set.Include(y => y.NsfwBlacklistedTags));
                if (gc.NsfwBlacklistedTags.Add(tagObj))
                    added = true;
                else
                {
                    gc.NsfwBlacklistedTags.Remove(tagObj);
                    var toRemove = gc.NsfwBlacklistedTags.FirstOrDefault(x => x.Equals(tagObj));
                    if (toRemove != null)
                        uow._context.Remove(toRemove);
                    added = false;
                }
                var newTags = new HashSet<string>(gc.NsfwBlacklistedTags.Select(x => x.Tag));
                _blacklistedTags.AddOrUpdate(guildId, newTags, delegate { return newTags; });

                uow.SaveChanges();
            }
            return added;
        }

        public void ClearCache()
        {
            foreach (var c in _imageCacher)
            {
                c.Value?.Clear();
            }
        }

        public async Task<string> GetYomamaJoke()
        {
            using (var http = _httpFactory.CreateClient())
            {
                var response = await http.GetStringAsync(new Uri("http://api.yomomma.info/")).ConfigureAwait(false);
                return JObject.Parse(response)["joke"].ToString() + " 😆";
            }
        }

        public static async Task<(string Text, string BaseUri)> GetRandomJoke()
        {
            var config = AngleSharp.Configuration.Default.WithDefaultLoader();
            using (var document = await BrowsingContext.New(config).OpenAsync("http://www.goodbadjokes.com/random").ConfigureAwait(false))
            {
                var html = document.QuerySelector(".post > .joke-body-wrap > .joke-content");

                var part1 = html.QuerySelector("dt")?.TextContent;
                var part2 = html.QuerySelector("dd")?.TextContent;

                return (part1 + "\n\n" + part2, document.BaseUri);
            }
        }

        public async Task<string> GetChuckNorrisJoke()
        {
            using (var http = _httpFactory.CreateClient())
            {
                var response = await http.GetStringAsync(new Uri("http://api.icndb.com/jokes/random/")).ConfigureAwait(false);
                return JObject.Parse(response)["value"]["joke"].ToString() + " 😆";
            }
        }

        public Task Unload()
        {
            AutoBoobTimers.ForEach(x => x.Value.Change(Timeout.Infinite, Timeout.Infinite));
            AutoBoobTimers.Clear();
            AutoButtTimers.ForEach(x => x.Value.Change(Timeout.Infinite, Timeout.Infinite));
            AutoButtTimers.Clear();
            AutoHentaiTimers.ForEach(x => x.Value.Change(Timeout.Infinite, Timeout.Infinite));
            AutoHentaiTimers.Clear();

            _imageCacher.Clear();
            return Task.CompletedTask;
        }

        public async Task<MtgData> GetMtgCardAsync(string search)
        {
            search = search.Trim().ToLowerInvariant();
            var data = await _cache.GetOrAddCachedDataAsync($"nadeko_mtg_{search}",
                GetMtgCardFactory,
                search,
                TimeSpan.FromDays(1)).ConfigureAwait(false);

            if (data == null || data.Length == 0)
                return null;

            return data[_rng.Next(0, data.Length)];
        }

        private async Task<MtgData[]> GetMtgCardFactory(string search)
        {
            async Task<MtgData> GetMtgDataAsync(MtgResponse.Data card)
            {
                string storeUrl;
                try
                {
                    storeUrl = await _google.ShortenUrl($"https://shop.tcgplayer.com/productcatalog/product/show?" +
                        $"newSearch=false&" +
                        $"ProductType=All&" +
                        $"IsProductNameExact=false&" +
                        $"ProductName={Uri.EscapeUriString(card.Name)}").ConfigureAwait(false);
                }
                catch { storeUrl = "<url can't be found>"; }

                return new MtgData
                {
                    Description = card.Text,
                    Name = card.Name,
                    ImageUrl = card.ImageUrl,
                    StoreUrl = storeUrl,
                    Types = string.Join(",\n", card.Types),
                    ManaCost = card.ManaCost,
                };
            }

            using (var http = _httpFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Clear();
                var response = await http.GetStringAsync($"https://api.magicthegathering.io/v1/cards?name={Uri.EscapeUriString(search)}")
                    .ConfigureAwait(false);

                var responseObject = JsonConvert.DeserializeObject<MtgResponse>(response);
                if (responseObject == null)
                    return new MtgData[0];

                var cards = responseObject.Cards.Take(5).ToArray();
                if (cards.Length == 0)
                    return new MtgData[0];

                var tasks = new List<Task<MtgData>>(cards.Length);
                for (int i = 0; i < cards.Length; i++)
                {
                    var card = cards[i];

                    tasks.Add(GetMtgDataAsync(card));
                }

                return await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        public Task<HearthstoneCardData> GetHearthstoneCardDataAsync(string name)
        {
            name = name.ToLowerInvariant();
            return _cache.GetOrAddCachedDataAsync($"nadeko_hearthstone_{name}",
                HearthstoneCardDataFactory,
                name,
                TimeSpan.FromDays(1));
        }

        private async Task<HearthstoneCardData> HearthstoneCardDataFactory(string name)
        {
            using (var http = _httpFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", _creds.MashapeKey);
                try
                {
                    var response = await http.GetStringAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/" +
                        $"cards/search/{Uri.EscapeUriString(name)}").ConfigureAwait(false);
                    var objs = JsonConvert.DeserializeObject<HearthstoneCardData[]>(response);
                    if (objs == null || objs.Length == 0)
                        return null;
                    var data = objs.FirstOrDefault(x => x.Collectible)
                        ?? objs.FirstOrDefault(x => !string.IsNullOrEmpty(x.PlayerClass))
                        ?? objs.FirstOrDefault();
                    if (data == null)
                        return null;
                    if (!string.IsNullOrWhiteSpace(data.Img))
                    {
                        data.Img = await _google.ShortenUrl(data.Img).ConfigureAwait(false);
                    }
                    if (!string.IsNullOrWhiteSpace(data.Text))
                    {
                        var converter = new Html2Markdown.Converter();
                        data.Text = converter.Convert(data.Text);
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                    return null;
                }
            }
        }

        public Task<OmdbMovie> GetMovieDataAsync(string name)
        {
            name = name.Trim().ToLowerInvariant();
            return _cache.GetOrAddCachedDataAsync($"nadeko_movie_{name}",
                GetMovieDataFactory,
                name,
                TimeSpan.FromDays(1));
        }

        private async Task<OmdbMovie> GetMovieDataFactory(string name)
        {
            using (var http = _httpFactory.CreateClient())
            {
                var res = await http.GetStringAsync(string.Format("https://omdbapi.nadeko.bot/?t={0}&y=&plot=full&r=json",
                    name.Trim().Replace(' ', '+'))).ConfigureAwait(false);
                var movie = JsonConvert.DeserializeObject<OmdbMovie>(res);
                if (movie?.Title == null)
                    return null;
                movie.Poster = await _google.ShortenUrl(movie.Poster).ConfigureAwait(false);
                return movie;
            }
        }
    }
}
