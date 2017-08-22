using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp.Services
{
    public class XpService : INService
    {
        private readonly DbService _db;
        private readonly CommandHandler _cmd;
        private readonly IBotConfigProvider _bc;
        private readonly Logger _log;
        public const int XP_REQUIRED_LVL_1 = 36;

        private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _excludedRoles 
            = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();
        private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _excludedChannels 
            = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();
        private readonly ConcurrentHashSet<ulong> _excludedServers = new ConcurrentHashSet<ulong>();

        private readonly ConcurrentHashSet<ulong> _rewardedUsers = new ConcurrentHashSet<ulong>();

        private readonly ConcurrentQueue<UserCacheItem> _addMessageXp = new ConcurrentQueue<UserCacheItem>();

        private readonly Timer updateXpTimer;
        private readonly Timer clearRewardedUsersTimer;

        public XpService(CommandHandler cmd, IBotConfigProvider bc, 
            IEnumerable<GuildConfig> allGuildConfigs, DbService db)
        {
            _db = db;
            _cmd = cmd;
            _bc = bc;
            _log = LogManager.GetCurrentClassLogger();

            _cmd.OnMessageNoTrigger += _cmd_OnMessageNoTrigger;

            updateXpTimer = new Timer(_ =>
            {
                using (var uow = _db.UnitOfWork)
                {
                    while (_addMessageXp.TryDequeue(out var usr))
                    {
                        var usrObj = uow.Xp.GetOrCreateUser(usr.GuildId, usr.UserId);
                        usrObj.Xp += _bc.BotConfig.XpPerMessage;
                    }
                    uow.Complete();
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            clearRewardedUsersTimer = new Timer(_ =>
            {
                _rewardedUsers.Clear();
            }, null, TimeSpan.FromSeconds(bc.BotConfig.XpMinutesTimeout), TimeSpan.FromSeconds(bc.BotConfig.XpMinutesTimeout));
        }

        private Task _cmd_OnMessageNoTrigger(IUserMessage arg)
        {
            if (!(arg.Author is SocketGuildUser user) || user.IsBot)
                return Task.CompletedTask;

            var _ = Task.Run(() =>
            {
                if (!SetUserRewarded(user.Id))
                    return;

                if (_excludedChannels.TryGetValue(user.Guild.Id, out var chans) &&
                    chans.Contains(arg.Channel.Id))
                    return;

                if (_excludedServers.Contains(user.Guild.Id))
                    return;

                if (_excludedRoles.TryGetValue(user.Guild.Id, out var roles) &&
                    user.Roles.Any(x => roles.Contains(x.Id)))
                    return;

                _log.Info("Adding {0} xp to {1} on {2} server", _bc.BotConfig.XpPerMessage, user.ToString(), user.Guild.Name);
                _addMessageXp.Enqueue(new UserCacheItem { GuildId = user.Guild.Id, UserId = user.Id });
            });
            return Task.CompletedTask;
        }

        public bool IsServerExcluded(ulong id)
        {
            return _excludedServers.Contains(id);
        }

        public IEnumerable<ulong> GetExcludedRoles(ulong id)
        {
            if (_excludedRoles.TryGetValue(id, out var val))
                return val.ToArray();

            return Enumerable.Empty<ulong>();
        }

        public IEnumerable<ulong> GetExcludedChannels(ulong id)
        {
            if (_excludedChannels.TryGetValue(id, out var val))
                return val.ToArray();

            return Enumerable.Empty<ulong>();
        }

        private bool SetUserRewarded(ulong userId)
        {
            return _rewardedUsers.Add(userId);
        }

        public UserXpStats GetUserStats(ulong guildId, ulong userId)
        {
            UserXpStats user;
            using (var uow = _db.UnitOfWork)
            {
                user = uow.Xp.GetOrCreateUser(guildId, userId);
            }

            return user;
        }

        public string GenerateXpBar(int currentXp, int requiredXp)
        {
            //todo
            return $"{currentXp}/{requiredXp}";
        }


        //todo exclude in database
        public bool ToggleExcludeServer(ulong id)
        {
            using (var uow = _db.UnitOfWork)
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(id);
                if (_excludedServers.Add(id))
                {
                    xpSetting.ServerExcluded = true;
                    uow.Complete();
                    return true;
                }

                _excludedServers.TryRemove(id);
                xpSetting.ServerExcluded = false;
                uow.Complete();
                return false;
            }
        }

        public bool ToggleExcludeRole(ulong guildId, ulong rId)
        {
            var roles = _excludedRoles.GetOrAdd(guildId, _ => new ConcurrentHashSet<ulong>());
            using (var uow = _db.UnitOfWork)
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(guildId);
                var excludeObj = new ExcludedItem
                {
                    ItemId = rId,
                    ItemType = ExcludedItemType.Role,
                };

                if (roles.Add(rId))
                {

                    if (xpSetting.ExclusionList.Add(excludeObj))
                    {
                        uow.Complete();
                    }

                    return true;
                }
                else
                {
                    roles.TryRemove(rId);

                    if (xpSetting.ExclusionList.Remove(excludeObj))
                    {
                        uow.Complete();
                    }

                    return false;
                }
            }
        }

        public bool ToggleExcludeChannel(ulong guildId, ulong chId)
        {
            var channels = _excludedChannels.GetOrAdd(guildId, _ => new ConcurrentHashSet<ulong>());
            using (var uow = _db.UnitOfWork)
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(guildId);
                var excludeObj = new ExcludedItem
                {
                    ItemId = chId,
                    ItemType = ExcludedItemType.Channel,
                };

                if (channels.Add(chId))
                {

                    if (xpSetting.ExclusionList.Add(excludeObj))
                    {
                        uow.Complete();
                    }

                    return true;
                }
                else
                {
                    channels.TryRemove(chId);

                    if (xpSetting.ExclusionList.Remove(excludeObj))
                    {
                        uow.Complete();
                    }

                    return false;
                }
            }
        }
    }
}
