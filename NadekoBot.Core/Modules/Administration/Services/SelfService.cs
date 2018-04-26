using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NLog;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using NadekoBot.Common.ShardCom;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services.Database.Models;
using System.Threading;
using System.Collections.Concurrent;

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
        private ImmutableDictionary<ulong, IDMChannel> ownerChannels = new Dictionary<ulong, IDMChannel>().ToImmutableDictionary();
        private ConcurrentDictionary<ulong?, ConcurrentDictionary<int, Timer>> _autoCommands = new ConcurrentDictionary<ulong?, ConcurrentDictionary<int, Timer>>();
        private readonly IBotConfigProvider _bc;
        private readonly IDataCache _cache;
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
            _cache = cache;
            _imgs = cache.LocalImages;

            var sub = _redis.GetSubscriber();
            sub.Subscribe(_creds.RedisKey() + "_reload_images",
                delegate { _imgs.Reload(); }, CommandFlags.FireAndForget);
            sub.Subscribe(_creds.RedisKey() + "_reload_bot_config",
                delegate { _bc.Reload(); }, CommandFlags.FireAndForget);

            Task.Run(async () =>
            {
                await bot.Ready.Task.ConfigureAwait(false);

                foreach (var cmd in bc.BotConfig.StartupCommands.Where(x => x.Interval <= 0))
                {
                    await ExecuteCommand(cmd);
                }

                _autoCommands = bc.BotConfig
                    .StartupCommands
                    .Where(x => x.Interval >= 5)
                    .GroupBy(x => x.GuildId)
                    .ToDictionary(
                        x => x.Key,
                        y => y.ToDictionary(x => x.Id,
                            x => TimerFromStartupCommand((StartupCommand)x))
                    .ToConcurrent())
                    .ToConcurrent();

            });

            Task.Run(async () =>
            {
                await bot.Ready.Task.ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                if (client.ShardId == 0)
                    await LoadOwnerChannels().ConfigureAwait(false);
            });
        }

        private Timer TimerFromStartupCommand(StartupCommand x)
        {
            return new Timer(async (obj) => await ExecuteCommand((StartupCommand)obj),
                x,
                x.Interval * 1000,
                x.Interval * 1000);
        }

        private async Task ExecuteCommand(StartupCommand cmd)
        {
            var prefix = _cmdHandler.GetPrefix(cmd.GuildId);
            //if someone already has .die as their startup command, ignore it
            if (cmd.CommandText.StartsWith(prefix + "die"))
                return;
            await _cmdHandler.ExecuteExternal(cmd.GuildId, cmd.ChannelId, cmd.CommandText);
            await Task.Delay(400).ConfigureAwait(false);
        }

        public void AddNewAutoCommand(StartupCommand cmd)
        {
            using (var uow = _db.UnitOfWork)
            {
                uow.BotConfig
                   .GetOrCreate(set => set.Include(x => x.StartupCommands))
                   .StartupCommands
                   .Add(cmd);

                var autos = _autoCommands.GetOrAdd(cmd.GuildId, new ConcurrentDictionary<int, Timer>());
                autos.AddOrUpdate(cmd.Id, key => TimerFromStartupCommand(cmd), (key, old) =>
                {
                    old.Change(Timeout.Infinite, Timeout.Infinite);
                    return TimerFromStartupCommand(cmd);
                });
                uow.Complete();
            }
        }

        public IEnumerable<StartupCommand> GetStartupCommands()
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.BotConfig
                   .GetOrCreate(set => set.Include(x => x.StartupCommands))
                   .StartupCommands
                   .OrderBy(x => x.Id)
                   .ToArray();
            }
        }

        private async Task LoadOwnerChannels()
        {
            var channels = await Task.WhenAll(_creds.OwnerIds.Select(id =>
            {
                var user = _client.GetUser(id);
                if (user == null)
                    return Task.FromResult<IDMChannel>(null);

                return user.GetOrCreateDMChannelAsync();
            }));

            ownerChannels = channels.Where(x => x != null)
                .ToDictionary(x => x.Recipient.Id, x => x)
                .ToImmutableDictionary();

            if (!ownerChannels.Any())
                _log.Warn("No owner channels created! Make sure you've specified correct OwnerId in the credentials.json file.");
            else
                _log.Info($"Created {ownerChannels.Count} out of {_creds.OwnerIds.Length} owner message channels.");
        }

        // forwards dms
        public async Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            if (msg.Channel is IDMChannel && ForwardDMs && ownerChannels.Any())
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
                    var allOwnerChannels = ownerChannels.Values;

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
                    var firstOwnerChannel = ownerChannels.Values.First();
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

        public bool RemoveStartupCommand(int index, out StartupCommand cmd)
        {
            using (var uow = _db.UnitOfWork)
            {
                var cmds = uow.BotConfig
                   .GetOrCreate(set => set.Include(x => x.StartupCommands))
                   .StartupCommands;
                cmd = cmds
                   .FirstOrDefault(x => x.Index == index);

                if (cmd != null)
                {
                    cmds.Remove(cmd);
                    if (_autoCommands.TryGetValue(cmd.GuildId, out var autos))
                        if (autos.TryRemove(cmd.Id, out var timer))
                            timer.Change(Timeout.Infinite, Timeout.Infinite);

                    uow.Complete();
                    return true;
                }
            }
            return false;
        }

        public void ClearStartupCommands()
        {
            using (var uow = _db.UnitOfWork)
            {
                uow.BotConfig
                   .GetOrCreate(set => set.Include(x => x.StartupCommands))
                   .StartupCommands
                   .Clear();
                uow.Complete();
            }
        }

        public void ReloadBotConfig()
        {
            var sub = _cache.Redis.GetSubscriber();
            sub.Publish(_creds.RedisKey() + "_reload_bot_config",
                "",
                CommandFlags.FireAndForget);
        }

        public void ReloadImages()
        {
            var sub = _cache.Redis.GetSubscriber();
            sub.Publish(_creds.RedisKey() + "_reload_images",
                "",
                CommandFlags.FireAndForget);
        }

        public void Die()
        {
            var sub = _cache.Redis.GetSubscriber();
            sub.Publish(_creds.RedisKey() + "_die", "", CommandFlags.FireAndForget);
        }

        public void ForwardMessages()
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.BotConfig.GetOrCreate(set => set);
                config.ForwardMessages = !config.ForwardMessages;
                uow.Complete();
            }
            _bc.Reload();
        }

        public void Restart()
        {
            Process.Start(_creds.RestartCommand.Cmd, _creds.RestartCommand.Args);
            var sub = _cache.Redis.GetSubscriber();
            sub.Publish(_creds.RedisKey() + "_die", "", CommandFlags.FireAndForget);
        }

        public void RestartShard(int shardId)
        {
            var pub = _cache.Redis.GetSubscriber();
            pub.Publish(_creds.RedisKey() + "_shardcoord_stop",
                JsonConvert.SerializeObject(shardId),
                CommandFlags.FireAndForget);
        }

        public void ForwardToAll()
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.BotConfig.GetOrCreate(set => set);
                config.ForwardToAllOwners = !config.ForwardToAllOwners;
                uow.Complete();
            }
            _bc.Reload();
        }

        public IEnumerable<ShardComMessage> GetAllShardStatuses(int page)
        {
            var db = _cache.Redis.GetDatabase();
            return db.ListRange(_creds.RedisKey() + "_shardstats")
                .Select(x => JsonConvert.DeserializeObject<ShardComMessage>(x));
        }
    }
}
