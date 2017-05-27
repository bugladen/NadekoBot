using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Games
{
    public class GamesService
    {
        private readonly BotConfig _bc;

        public readonly ConcurrentDictionary<ulong, GirlRating> GirlRatings = new ConcurrentDictionary<ulong, GirlRating>();
        public readonly ImmutableArray<string> EightBallResponses;

        private readonly Timer _t;
        private readonly DiscordShardedClient _client;
        private readonly NadekoStrings _strings;
        private readonly IImagesService _images;
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, Lazy<ChatterBotSession>> CleverbotGuilds { get; }

        public readonly string TypingArticlesPath = "data/typing_articles2.json";
        public List<TypingArticle> TypingArticles { get; } = new List<TypingArticle>();

        public GamesService(DiscordShardedClient client, BotConfig bc, IEnumerable<GuildConfig> gcs, 
            NadekoStrings strings, IImagesService images)
        {
            _bc = bc;
            _client = client;
            _strings = strings;
            _images = images;
            _log = LogManager.GetCurrentClassLogger();

            //8ball
            EightBallResponses = _bc.EightBallResponses.Select(ebr => ebr.Text).ToImmutableArray();

            //girl ratings
            _t = new Timer((_) =>
            {
                GirlRatings.Clear();

            }, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

            //cleverbot
            CleverbotGuilds = new ConcurrentDictionary<ulong, Lazy<ChatterBotSession>>(
                    gcs.Where(gc => gc.CleverbotEnabled)
                        .ToDictionary(gc => gc.GuildId, gc => new Lazy<ChatterBotSession>(() => new ChatterBotSession(gc.GuildId), true)));

            //plantpick
            client.MessageReceived += PotentialFlowerGeneration;
            GenerationChannels = new ConcurrentHashSet<ulong>(gcs
                .SelectMany(c => c.GenerateCurrencyChannelIds.Select(obj => obj.ChannelId)));

            try
            {
                TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(TypingArticlesPath));
            }
            catch (Exception ex)
            {
                _log.Warn("Error while loading typing articles {0}", ex.ToString());
                TypingArticles = new List<TypingArticle>();
            }
        }


        public string PrepareMessage(IUserMessage msg, out ChatterBotSession cleverbot)
        {
            var channel = msg.Channel as ITextChannel;
            cleverbot = null;

            if (channel == null)
                return null;

            Lazy<ChatterBotSession> lazyCleverbot;
            if (!CleverbotGuilds.TryGetValue(channel.Guild.Id, out lazyCleverbot))
                return null;

            cleverbot = lazyCleverbot.Value;

            var nadekoId = _client.CurrentUser.Id;
            var normalMention = $"<@{nadekoId}> ";
            var nickMention = $"<@!{nadekoId}> ";
            string message;
            if (msg.Content.StartsWith(normalMention))
            {
                message = msg.Content.Substring(normalMention.Length).Trim();
            }
            else if (msg.Content.StartsWith(nickMention))
            {
                message = msg.Content.Substring(nickMention.Length).Trim();
            }
            else
            {
                return null;
            }

            return message;
        }

        public async Task<bool> TryAsk(ChatterBotSession cleverbot, ITextChannel channel, string message)
        {
            await channel.TriggerTypingAsync().ConfigureAwait(false);

            var response = await cleverbot.Think(message).ConfigureAwait(false);
            try
            {
                await channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false);
            }
            catch
            {
                await channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false); // try twice :\
            }
            return true;
        }

        public ConcurrentHashSet<ulong> GenerationChannels { get; }
        //channelid/message
        public ConcurrentDictionary<ulong, List<IUserMessage>> PlantedFlowers { get; } = new ConcurrentDictionary<ulong, List<IUserMessage>>();
        //channelId/last generation
        public ConcurrentDictionary<ulong, DateTime> LastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();
        
        public KeyValuePair<string, ImmutableArray<byte>> GetRandomCurrencyImage()
        {
            var rng = new NadekoRandom();
            return _images.Currency[rng.Next(0, _images.Currency.Length)];
        }

        private string GetText(ITextChannel ch, string key, params object[] rep)
            => _strings.GetText(key, ch.GuildId, "Games".ToLowerInvariant(), rep);

        private Task PotentialFlowerGeneration(SocketMessage imsg)
        {
            var msg = imsg as SocketUserMessage;
            if (msg == null || msg.Author.IsBot)
                return Task.CompletedTask;

            var channel = imsg.Channel as ITextChannel;
            if (channel == null)
                return Task.CompletedTask;

            if (!GenerationChannels.Contains(channel.Id))
                return Task.CompletedTask;

            var _ = Task.Run(async () =>
            {
                try
                {
                    var lastGeneration = LastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
                    var rng = new NadekoRandom();

                    //todo i'm stupid :rofl: wtg kwoth. real async programming :100: :ok_hand: :100: :100: :thumbsup:
                    if (DateTime.Now - TimeSpan.FromSeconds(_bc.CurrencyGenerationCooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

                    var num = rng.Next(1, 101) + _bc.CurrencyGenerationChance * 100;

                    if (num > 100)
                    {
                        LastGenerations.AddOrUpdate(channel.Id, DateTime.Now, (id, old) => DateTime.Now);

                        var dropAmount = _bc.CurrencyDropAmount;

                        if (dropAmount > 0)
                        {
                            var msgs = new IUserMessage[dropAmount];
                            var prefix = NadekoBot.Prefix;
                            var toSend = dropAmount == 1
                                ? GetText(channel, "curgen_sn", _bc.CurrencySign)
                                    + " " + GetText(channel, "pick_sn", prefix)
                                : GetText(channel, "curgen_pl", dropAmount, _bc.CurrencySign)
                                    + " " + GetText(channel, "pick_pl", prefix);
                            var file = GetRandomCurrencyImage();
                            using (var fileStream = file.Value.ToStream())
                            {
                                var sent = await channel.SendFileAsync(
                                    fileStream,
                                    file.Key,
                                    toSend).ConfigureAwait(false);

                                msgs[0] = sent;
                            }

                            PlantedFlowers.AddOrUpdate(channel.Id, msgs.ToList(), (id, old) => { old.AddRange(msgs); return old; });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Warn(ex);
                }
            });
            return Task.CompletedTask;
        }
    }
}
