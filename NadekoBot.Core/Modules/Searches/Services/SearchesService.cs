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
using ImageSharp;
using Image = ImageSharp.Image;
using SixLabors.Primitives;
using SixLabors.Fonts;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Core.Modules.Searches.Common;

namespace NadekoBot.Modules.Searches.Services
{
    public class SearchesService : INService, IUnloadableService
    {
        public HttpClient Http { get; }

        private readonly DiscordSocketClient _client;
        private readonly IGoogleApiService _google;
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly IImageCache _imgs;
        private readonly IDataCache _cache;
        private readonly FontProvider _fonts;

        public ConcurrentDictionary<ulong, bool> TranslatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
        public ConcurrentDictionary<UserChannelPair, string> UserLanguages { get; } = new ConcurrentDictionary<UserChannelPair, string>();

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
                    data = await Http.GetStringAsync("https://api.coinmarketcap.com/v1/ticker/")
                        .ConfigureAwait(false);

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
            DbService db, NadekoBot bot, IDataCache cache,
            FontProvider fonts)
        {
            Http = new HttpClient();
            Http.AddFakeHeaders();
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
                        var umsg = msg as SocketUserMessage;
                        if (umsg == null)
                            return;

                        if (!TranslatedChannels.TryGetValue(umsg.Channel.Id, out var autoDelete))
                            return;

                        var key = new UserChannelPair()
                        {
                            UserId = umsg.Author.Id,
                            ChannelId = umsg.Channel.Id,
                        };

                        if (!UserLanguages.TryGetValue(key, out string langs))
                            return;

                        var text = await Translate(langs, umsg.Resolve(TagHandling.Ignore))
                                            .ConfigureAwait(false);
                        if (autoDelete)
                            try { await umsg.DeleteAsync().ConfigureAwait(false); } catch { }
                        await umsg.Channel.SendConfirmAsync($"{umsg.Author.Mention} `:` " + text.Replace("<@ ", "<@").Replace("<@! ", "<@!")).ConfigureAwait(false);
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

        public async Task<Image<Rgba32>> GetRipPictureAsync(string text, string imgUrl)
        {
            var (succ, data) = await _cache.TryGetImageDataAsync(imgUrl);
            if (!succ)
            {
                using (var temp = await Http.GetAsync(imgUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (temp.Content.Headers.ContentType.MediaType != "image/png"
                        && temp.Content.Headers.ContentType.MediaType != "image/jpeg"
                        && temp.Content.Headers.ContentType.MediaType != "image/gif")
                        data = null;
                    else
                    {
                        using (var tempDraw = ImageSharp.Image.Load(await temp.Content.ReadAsStreamAsync()).Resize(69, 70))
                        {
                            tempDraw.ApplyRoundedCorners(35);
                            data = tempDraw.ToStream().ToArray();
                        }
                    }
                }

                await _cache.SetImageDataAsync(imgUrl, data);
            }
            var bg = ImageSharp.Image.Load(_imgs.Rip.ToArray());

            //avatar 82, 139
            if (data != null)
            {
                using (var avatar = Image.Load(data).Resize(85, 85))
                {
                    bg.DrawImage(avatar,
                        default,
                        new Point(82, 139),
                        GraphicsOptions.Default);
                }
            }
            //text 63, 241
            bg.DrawText(text, 
                _fonts.RipNameFont, 
                Rgba32.Black, 
                new PointF(25, 225),
                new ImageSharp.Drawing.TextGraphicsOptions()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 190,
                });

            //flowa
            using (var flowers = Image.Load(_imgs.FlowerCircle.ToArray()))
            {
                bg.DrawImage(flowers,
                    default,
                    new Point(0, 0),
                    GraphicsOptions.Default);
            }

            return bg;
        }

        public async Task<string> Translate(string langs, string text = null)
        {
            var langarr = langs.ToLowerInvariant().Split('>');
            if (langarr.Length != 2)
                throw new ArgumentException();
            var from = langarr[0];
            var to = langarr[1];
            text = text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException();
            return (await _google.Translate(text, from, to).ConfigureAwait(false)).SanitizeMentions();
        }

        public Task<ImageCacherObject> DapiSearch(string tag, DapiSearchType type, ulong? guild, bool isExplicit = false)
        {
            if (guild.HasValue)
            {
                var blacklistedTags = GetBlacklistedTags(guild.Value);

                var cacher = _imageCacher.GetOrAdd(guild.Value, (key) => new SearchImageCacher());

                return cacher.GetImage(tag, isExplicit, type, blacklistedTags);
            }
            else
            {
                var cacher = _imageCacher.GetOrAdd(guild ?? 0, (key) => new SearchImageCacher());

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
                var gc = uow.GuildConfigs.For(guildId, set => set.Include(y => y.NsfwBlacklistedTags));
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
            var response = await Http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
            return JObject.Parse(response)["joke"].ToString() + " 😆";
        }

        public async Task<(string Text, string BaseUri)> GetRandomJoke()
        {
            var config = AngleSharp.Configuration.Default.WithDefaultLoader();
            var document = await BrowsingContext.New(config).OpenAsync("http://www.goodbadjokes.com/random");

            var html = document.QuerySelector(".post > .joke-content");

            var part1 = html.QuerySelector("dt").TextContent;
            var part2 = html.QuerySelector("dd").TextContent;

            return (part1 + "\n\n" + part2, document.BaseUri);
        }

        public async Task<string> GetChuckNorrisJoke()
        {
            var response = await Http.GetStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
            return JObject.Parse(response)["value"]["joke"].ToString() + " 😆";
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
    
    public struct UserChannelPair
    {
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
    }
}
