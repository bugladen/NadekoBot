using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Modules.Utility.Models;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public class UtilityService
    {
        public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; } = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>();

        //messagerepeater
        //guildid/RepeatRunners
        public ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>> Repeaters { get; set; }
        public bool RepeaterReady { get; private set; }

        //remind
        public RemindService Remind { get; }

        //unit conversion
        public ConverterService Converter { get; }

        public UtilityService(IEnumerable<GuildConfig> guildConfigs, DiscordShardedClient client, BotConfig config, DbHandler db)
        {
            //commandmap
            AliasMaps = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>(
                    guildConfigs.ToDictionary(
                        x => x.GuildId,
                        x => new ConcurrentDictionary<string, string>(x.CommandAliases
                            .Distinct(new CommandAliasEqualityComparer())
                            .ToDictionary(ca => ca.Trigger, ca => ca.Mapping))));

            //crossesrver
            _client = client;
            _client.MessageReceived += Client_MessageReceived;

            //messagerepeater
            var _ = Task.Run(async () =>
            {
#if !GLOBAL_NADEKO
                await Task.Delay(5000).ConfigureAwait(false);
#else
                    await Task.Delay(30000).ConfigureAwait(false);
#endif
                //todo this is pretty terrible :kms: no time
                Repeaters = new ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>>(guildConfigs
                    .ToDictionary(gc => gc.GuildId,
                        gc => new ConcurrentQueue<RepeatRunner>(gc.GuildRepeaters
                            .Select(gr => new RepeatRunner(client, gr))
                            .Where(x => x.Guild != null))));
                RepeaterReady = true;
            });

            //reminder
            Remind = new RemindService(client, config, db);

            //unit converter
            Converter = new ConverterService(db);
        }

        private async Task Client_MessageReceived(Discord.WebSocket.SocketMessage imsg)
        {
            try
            {
                if (imsg.Author.IsBot)
                    return;
                var msg = imsg as IUserMessage;
                if (msg == null)
                    return;
                var channel = imsg.Channel as ITextChannel;
                if (channel == null)
                    return;
                if (msg.Author.Id == _client.CurrentUser.Id) return;
                foreach (var subscriber in Subscribers)
                {
                    var set = subscriber.Value;
                    if (!set.Contains(channel))
                        continue;
                    foreach (var chan in set.Except(new[] { channel }))
                    {
                        try
                        {
                            await chan.SendMessageAsync(GetMessage(channel, (IGuildUser)msg.Author,
                                msg)).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private string GetMessage(ITextChannel channel, IGuildUser user, IUserMessage message) =>
            $"**{channel.Guild.Name} | {channel.Name}** `{user.Username}`: " + message.Content.SanitizeMentions();

        public readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers =
            new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();
        private DiscordShardedClient _client;
    }

    public class ConverterService
    {
        public class MeasurementUnit
        {
            public List<string> Triggers { get; set; }
            public string UnitType { get; set; }
            public decimal Modifier { get; set; }
        }

        public class Rates
        {
            public string Base { get; set; }
            public DateTime Date { get; set; }
            [JsonProperty("rates")]
            public Dictionary<string, decimal> ConversionRates { get; set; }
        }

        public List<ConvertUnit> Units { get; set; } = new List<ConvertUnit>();
        private readonly Logger _log;
        private Timer _timer;
        private readonly TimeSpan _updateInterval = new TimeSpan(12, 0, 0);
        private readonly DbHandler _db;

        public ConverterService(DbHandler db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            try
            {
                var data = JsonConvert.DeserializeObject<List<MeasurementUnit>>(File.ReadAllText("data/units.json")).Select(u => new ConvertUnit()
                {
                    Modifier = u.Modifier,
                    UnitType = u.UnitType,
                    InternalTrigger = string.Join("|", u.Triggers)
                }).ToArray();

                using (var uow = _db.UnitOfWork)
                {
                    if (uow.ConverterUnits.Empty())
                    {
                        uow.ConverterUnits.AddRange(data);
                        uow.Complete();
                    }
                }
                Units = data.ToList();
            }
            catch (Exception ex)
            {
                _log.Warn("Could not load units: " + ex.Message);
            }

            _timer = new Timer(async (obj) => await UpdateCurrency(), null, _updateInterval, _updateInterval);
        }
        
        public static async Task<Rates> UpdateCurrencyRates()
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync("http://api.fixer.io/latest").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Rates>(res);
            }
        }

        public async Task UpdateCurrency()
        {
            try
            {
                var currencyRates = await UpdateCurrencyRates();
                var unitTypeString = "currency";
                var range = currencyRates.ConversionRates.Select(u => new ConvertUnit()
                {
                    InternalTrigger = u.Key,
                    Modifier = u.Value,
                    UnitType = unitTypeString
                }).ToArray();
                var baseType = new ConvertUnit()
                {
                    Triggers = new[] { currencyRates.Base },
                    Modifier = decimal.One,
                    UnitType = unitTypeString
                };
                var toRemove = Units.Where(u => u.UnitType == unitTypeString);

                using (var uow = _db.UnitOfWork)
                {
                    uow.ConverterUnits.RemoveRange(toRemove.ToArray());
                    uow.ConverterUnits.Add(baseType);
                    uow.ConverterUnits.AddRange(range);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                Units.RemoveAll(u => u.UnitType == unitTypeString);
                Units.Add(baseType);
                Units.AddRange(range);
                _log.Info("Updated Currency");
            }
            catch
            {
                _log.Warn("Failed updating currency. Ignore this.");
            }
        }

    }

    public class RemindService
    {
        public readonly Regex Regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        public string RemindMessageFormat { get; }

        public readonly IDictionary<string, Func<Reminder, string>> _replacements = new Dictionary<string, Func<Reminder, string>>
            {
                { "%message%" , (r) => r.Message },
                { "%user%", (r) => $"<@!{r.UserId}>" },
                { "%target%", (r) =>  r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>"}
            };

        private readonly Logger _log;
        private readonly CancellationTokenSource cancelSource;
        private readonly CancellationToken cancelAllToken;
        private readonly BotConfig _config;
        private readonly DiscordShardedClient _client;
        private readonly DbHandler _db;

        public RemindService(DiscordShardedClient client, BotConfig config, DbHandler db)
        {
            _config = config;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _db = db;

            cancelSource = new CancellationTokenSource();
            cancelAllToken = cancelSource.Token;
            List<Reminder> reminders;
            using (var uow = _db.UnitOfWork)
            {
                reminders = uow.Reminders.GetAll().ToList();
            }
            RemindMessageFormat = _config.RemindMessageFormat;

            foreach (var r in reminders)
            {
                Task.Run(() => StartReminder(r));
            }
        }

        public async Task StartReminder(Reminder r)
        {
            var t = cancelAllToken;
            var now = DateTime.Now;

            var time = r.When - now;

            if (time.TotalMilliseconds > int.MaxValue)
                return;

            await Task.Delay(time, t).ConfigureAwait(false);
            try
            {
                IMessageChannel ch;
                if (r.IsPrivate)
                {
                    var user = _client.GetGuild(r.ServerId).GetUser(r.ChannelId);
                    if (user == null)
                        return;
                    ch = await user.CreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                }
                if (ch == null)
                    return;

                await ch.SendMessageAsync(
                    _replacements.Aggregate(RemindMessageFormat,
                        (cur, replace) => cur.Replace(replace.Key, replace.Value(r)))
                        .SanitizeMentions()
                        ).ConfigureAwait(false); //it works trust me
            }
            catch (Exception ex) { _log.Warn(ex); }
            finally
            {
                using (var uow = _db.UnitOfWork)
                {
                    uow.Reminders.Remove(r);
                    await uow.CompleteAsync();
                }
            }
        }
    }

    public class CommandAliasEqualityComparer : IEqualityComparer<CommandAlias>
    {
        public bool Equals(CommandAlias x, CommandAlias y) => x.Trigger == y.Trigger;

        public int GetHashCode(CommandAlias obj) => obj.Trigger.GetHashCode();
    }
}
