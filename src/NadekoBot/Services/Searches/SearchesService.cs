using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace NadekoBot.Services.Searches
{
    public class SearchesService
    {
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

        public SearchesService(DiscordSocketClient client, IGoogleApiService google, DbService db)
        {
            _client = client;
            _google = google;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();

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

        public async Task<string> DapiSearch(string tag, DapiSearchType type)
        {
            tag = tag?.Replace(" ", "_");
            var website = "";
            switch (type)
            {
                case DapiSearchType.Safebooru:
                    website = $"https://safebooru.org/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Gelbooru:
                    website = $"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Rule34:
                    website = $"https://rule34.xxx/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Konachan:
                    website = $"https://konachan.com/post.xml?s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Yandere:
                    website = $"https://yande.re/post.xml?limit=100&tags={tag}";
                    break;
            }
            try
            {
                var toReturn = await Task.Run(async () =>
                {
                    using (var http = new HttpClient())
                    {
                        http.AddFakeHeaders();
                        var data = await http.GetStreamAsync(website).ConfigureAwait(false);
                        var doc = new XmlDocument();
                        doc.Load(data);

                        var node = doc.LastChild.ChildNodes[new NadekoRandom().Next(0, doc.LastChild.ChildNodes.Count)];

                        var url = node.Attributes["file_url"].Value;
                        if (!url.StartsWith("http"))
                            url = "https:" + url;
                        return url;
                    }
                }).ConfigureAwait(false);
                return toReturn;
            }
            catch
            {
                return null;
            }
        }
    }

    public enum DapiSearchType
    {
        Safebooru,
        Gelbooru,
        Konachan,
        Rule34,
        Yandere
    }
    
    public struct UserChannelPair
    {
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
    }


    public class StreamStatus
    {
        public bool IsLive { get; set; }
        public string ApiLink { get; set; }
        public string Views { get; set; }
    }
}
