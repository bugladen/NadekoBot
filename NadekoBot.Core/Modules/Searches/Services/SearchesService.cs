using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using AngleSharp;
using System.Threading;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.Primitives;
using SixLabors.Fonts;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Core.Modules.Searches.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.ImageSharp.Processing.Drawing;

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

        private readonly SemaphoreSlim _cryptoLock = new SemaphoreSlim(1, 1);
        public async Task<CryptoData[]> CryptoData()
        {
            string data;
            var r = _cache.Redis.GetDatabase();
            await _cryptoLock.WaitAsync().ConfigureAwait(false);
            try
            {
                data = await r.StringGetAsync("crypto_data").ConfigureAwait(false);

                if (data == null)
                {
                    using (var http = _httpFactory.CreateClient())
                    {
                        data = await http.GetStringAsync(new Uri("https://api.coinmarketcap.com/v1/ticker/"))
                            .ConfigureAwait(false);
                    }
                    await r.StringSetAsync("crypto_data", data, TimeSpan.FromHours(1)).ConfigureAwait(false);
                }
            }
            finally
            {
                _cryptoLock.Release();
            }

            return JsonConvert.DeserializeObject<CryptoData[]>(data);
        }

        public SearchesService(DiscordSocketClient client, IGoogleApiService google,
            DbService db, NadekoBot bot, IDataCache cache, IHttpClientFactory factory,
            FontProvider fonts)
        {
            _httpFactory = factory;
            _client = client;
            _google = google;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _imgs = cache.LocalImages;
            _cache = cache;
            _fonts = fonts;

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

        public async Task<Image<Rgba32>> GetRipPictureAsync(string text, Uri imgUrl)
        {
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
            var bg = Image.Load(_imgs.Rip.ToArray());

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

            return bg;
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
            if (guild.HasValue)
            {
                var blacklistedTags = GetBlacklistedTags(guild.Value);

                var cacher = _imageCacher.GetOrAdd(guild.Value, (key) => new SearchImageCacher(_httpFactory));

                return cacher.GetImage(tag, isExplicit, type, blacklistedTags);
            }
            else
            {
                var cacher = _imageCacher.GetOrAdd(guild ?? 0, (key) => new SearchImageCacher(_httpFactory));

                return cacher.GetImage(tag, isExplicit, type);
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
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set.Include(y => y.NsfwBlacklistedTags));
                if (gc.NsfwBlacklistedTags.Add(tagObj))
                    added = true;
                else
                {
                    gc.NsfwBlacklistedTags.Remove(tagObj);
                    added = false;
                }
                var newTags = new HashSet<string>(gc.NsfwBlacklistedTags.Select(x => x.Tag));
                _blacklistedTags.AddOrUpdate(guildId, newTags, delegate { return newTags; });

                uow.Complete();
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

                var part1 = html.QuerySelector("dt").TextContent;
                var part2 = html.QuerySelector("dd").TextContent;

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
    }
}
