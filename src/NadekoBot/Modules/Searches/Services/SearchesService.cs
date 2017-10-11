using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Modules.NSFW.Exceptions;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using AngleSharp;

namespace NadekoBot.Modules.Searches.Services
{
    public class SearchesService : INService
    {
        public HttpClient Http { get; }

        private readonly DiscordSocketClient _client;
        private readonly IGoogleApiService _google;
        private readonly DbService _db;
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, bool> TranslatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
        public ConcurrentDictionary<UserChannelPair, string> UserLanguages { get; } = new ConcurrentDictionary<UserChannelPair, string>();
        
        public readonly string PokemonAbilitiesFile = "data/pokemon/pokemon_abilities7.json";
        public readonly string PokemonListFile = "data/pokemon/pokemon_list7.json";
        public Dictionary<string, SearchPokemon> Pokemons { get; } = new Dictionary<string, SearchPokemon>();
        public Dictionary<string, SearchPokemonAbility> PokemonAbilities { get; } = new Dictionary<string, SearchPokemonAbility>();

        public List<WoWJoke> WowJokes { get; } = new List<WoWJoke>();
        public List<MagicItem> MagicItems { get; } = new List<MagicItem>();

        private readonly ConcurrentDictionary<ulong, SearchImageCacher> _imageCacher = new ConcurrentDictionary<ulong, SearchImageCacher>();

        private readonly ConcurrentDictionary<ulong, HashSet<string>> _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>();

        public SearchesService(DiscordSocketClient client, IGoogleApiService google, DbService db, IEnumerable<GuildConfig> gcs)
        {
            Http = new HttpClient();
            Http.AddFakeHeaders();
            _client = client;
            _google = google;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();

            _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>(
                gcs.ToDictionary(
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

            //pokemon commands
            if (File.Exists(PokemonListFile))
            {
                Pokemons = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemon>>(File.ReadAllText(PokemonListFile));
            }
            else
                _log.Warn(PokemonListFile + " is missing. Pokemon abilities not loaded.");
            if (File.Exists(PokemonAbilitiesFile))
                PokemonAbilities = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemonAbility>>(File.ReadAllText(PokemonAbilitiesFile));
            else
                _log.Warn(PokemonAbilitiesFile + " is missing. Pokemon abilities not loaded.");

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

                if (blacklistedTags
                    .Any(x => tag.ToLowerInvariant().Contains(x)))
                {
                    throw new TagBlacklistedException();
                }

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
            var config = Configuration.Default.WithDefaultLoader();
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
    }
    
    public struct UserChannelPair
    {
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
    }
}
