using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NLog;
using StackExchange.Redis;

namespace NadekoBot.Modules.Administration.Services
{
    public class SelfService : ILateExecutor, INService
    {
        public bool ForwardDMs => _bc.BotConfig.ForwardMessages;
        public bool ForwardDMsToAllOwners => _bc.BotConfig.ForwardToAllOwners;

        private readonly ConnectionMultiplexer _redis;
        private readonly NadekoBot _bot;
        private readonly CommandHandler _cmdHandler;
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly ILocalization _localization;
        private readonly NadekoStrings _strings;
        private readonly DiscordSocketClient _client;
        private readonly IBotCredentials _creds;
        private ImmutableArray<AsyncLazy<IDMChannel>> ownerChannels = new ImmutableArray<AsyncLazy<IDMChannel>>();
        private readonly IBotConfigProvider _bc;
        private readonly IImageCache _imgs;

        public SelfService(DiscordSocketClient client, NadekoBot bot, CommandHandler cmdHandler, DbService db,
            IBotConfigProvider bc, ILocalization localization, NadekoStrings strings, IBotCredentials creds,
            IDataCache cache)
        {
            _redis = cache.Redis;
            _bot = bot;
            _cmdHandler = cmdHandler;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _localization = localization;
            _strings = strings;
            _client = client;
            _creds = creds;
            _bc = bc;
            _imgs = cache.LocalImages;

            var sub = _redis.GetSubscriber();
            sub.Subscribe(_creds.RedisKey() + "_reload_images",
                delegate { _imgs.Reload(); }, CommandFlags.FireAndForget);
            sub.Subscribe(_creds.RedisKey() + "_reload_bot_config",
                delegate { _bc.Reload(); }, CommandFlags.FireAndForget);

            Task.Run(async () =>
            {
                await bot.Ready.Task.ConfigureAwait(false);

                foreach (var cmd in bc.BotConfig.StartupCommands)
                {
                    var prefix = _cmdHandler.GetPrefix(cmd.GuildId);
                    //if someone already has .die as their startup command, ignore it
                    if (cmd.CommandText.StartsWith(prefix + "die"))
                        continue;
                    await cmdHandler.ExecuteExternal(cmd.GuildId, cmd.ChannelId, cmd.CommandText);
                    await Task.Delay(400).ConfigureAwait(false);
                }
            });

            Task.Run(async () =>
            {
                await bot.Ready.Task.ConfigureAwait(false);

                await Task.Delay(5000);

                if(client.ShardId == 0)
                    LoadOwnerChannels();                
            });
        }

        private void LoadOwnerChannels()
        {
            var hs = new HashSet<ulong>(_creds.OwnerIds);
            var channels = new Dictionary<ulong, AsyncLazy<IDMChannel>>();

            if (hs.Count > 0)
            {
                foreach (var g in _client.Guilds)
                {
                    if (hs.Count == 0)
                        break;

                    foreach (var u in g.Users)
                    {
                        if (hs.Remove(u.Id))
                        {
                            channels.Add(u.Id, new AsyncLazy<IDMChannel>(async () => await u.GetOrCreateDMChannelAsync()));
                            if (hs.Count == 0)
                                break;
                        }
                    }
                }
            }

            ownerChannels = channels.OrderBy(x => _creds.OwnerIds.IndexOf(x.Key))
                    .Select(x => x.Value)
                    .ToImmutableArray();

            if (!ownerChannels.Any())
                _log.Warn("No owner channels created! Make sure you've specified correct OwnerId in the credentials.json file.");
            else
                _log.Info($"Created {ownerChannels.Length} out of {_creds.OwnerIds.Length} owner message channels.");
        }

        // forwards dms
        public async Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            if (msg.Channel is IDMChannel && ForwardDMs && ownerChannels.Length > 0)
            {
                var title = _strings.GetText("dm_from",
                                _localization.DefaultCultureInfo,
                                "Administration".ToLowerInvariant()) +
                            $" [{msg.Author}]({msg.Author.Id})";

                var attachamentsTxt = _strings.GetText("attachments",
                    _localization.DefaultCultureInfo,
                    "Administration".ToLowerInvariant());

                var toSend = msg.Content;

                if (msg.Attachments.Count > 0)
                {
                    toSend += $"\n\n{Format.Code(attachamentsTxt)}:\n" +
                              string.Join("\n", msg.Attachments.Select(a => a.ProxyUrl));
                }

                if (ForwardDMsToAllOwners)
                {
                    var allOwnerChannels = await Task.WhenAll(ownerChannels
                        .Select(x => x.Value))
                        .ConfigureAwait(false);

                    foreach (var ownerCh in allOwnerChannels.Where(ch => ch.Recipient.Id != msg.Author.Id))
                    {
                        try
                        {
                            await ownerCh.SendConfirmAsync(title, toSend).ConfigureAwait(false);
                        }
                        catch
                        {
                            _log.Warn("Can't contact owner with id {0}", ownerCh.Recipient.Id);
                        }
                    }
                }
                else
                {
                    var firstOwnerChannel = await ownerChannels[0];
                    if (firstOwnerChannel.Recipient.Id != msg.Author.Id)
                    {
                        try
                        {
                            await firstOwnerChannel.SendConfirmAsync(title, toSend).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }
    }
}
