using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using NadekoBot.Modules.Utility.Extensions;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Modules.Utility.Common;
using NadekoBot.Modules.Utility.Common.Exceptions;
using Discord.Net;

namespace NadekoBot.Modules.Utility.Services
{
    public class StreamRoleService : INService
    {
        private readonly DbService _db;
        private readonly ConcurrentDictionary<ulong, StreamRoleSettings> guildSettings;
        //(guildId, userId), roleId
        private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), ulong> toRemove = new ConcurrentDictionary<(ulong GuildId, ulong UserId), ulong>();
        private readonly Logger _log;

        public StreamRoleService(DiscordSocketClient client, DbService db, IEnumerable<GuildConfig> gcs)
        {
            this._db = db;
            this._log = LogManager.GetCurrentClassLogger();

            guildSettings = gcs.ToDictionary(x => x.GuildId, x => x.StreamRole)
                .Where(x => x.Value != null && x.Value.Enabled)
                .ToConcurrent();

            client.GuildMemberUpdated += Client_GuildMemberUpdated;
        }

        private Task Client_GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var _ = Task.Run(async () =>
            {
                //if user wasn't streaming or didn't have a game status at all
                if ((!before.Game.HasValue || before.Game.Value.StreamType == StreamType.NotStreaming)
                    && guildSettings.TryGetValue(after.Guild.Id, out var setting))
                {
                    await TryApplyRole(after, setting).ConfigureAwait(false);
                }

                // try removing a role that was given to the user
                // if user had a game status
                // and he was streaming
                // and he no longer has a game status, or has a game status which is not a stream
                // and if he's scheduled for role removal, get the roleid to remove
                else if (before.Game.HasValue &&
                    before.Game.Value.StreamType != StreamType.NotStreaming &&
                    (!after.Game.HasValue || after.Game.Value.StreamType == StreamType.NotStreaming) &&
                    toRemove.TryRemove((after.Guild.Id, after.Id), out var roleId))
                {
                    try
                    {
                        //get the role to remove from the role id
                        var role = after.Guild.GetRole(roleId);
                        if (role == null)
                            return;
                        //check if user has the role which needs to be removed to avoid errors
                        if (after.Roles.Contains(role))
                            await after.RemoveRoleAsync(role).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Failed removing the stream role from the user who stopped streaming.");
                        _log.Error(ex);
                    }
                }
            });

            return Task.CompletedTask;
        }

        private async Task TryApplyRole(IGuildUser user, StreamRoleSettings setting)
        {
            // if the user has a game status now
            // and that status is a streaming status
            // and the feature is enabled
            // and he's not blacklisted
            // and keyword is either not set, or the game contains the keyword required, or he's whitelisted
            if (user.Game.HasValue &&
                    user.Game.Value.StreamType != StreamType.NotStreaming
                    && setting.Enabled
                    && !setting.Blacklist.Any(x => x.UserId == user.Id)
                    && (string.IsNullOrWhiteSpace(setting.Keyword) 
                        || user.Game.Value.Name.Contains(setting.Keyword) 
                        || setting.Whitelist.Any(x => x.UserId == user.Id)))
            {
                IRole fromRole;
                IRole addRole;

                //get needed roles
                fromRole = user.Guild.GetRole(setting.FromRoleId);
                if (fromRole == null)
                    throw new StreamRoleNotFoundException();
                addRole = user.Guild.GetRole(setting.AddRoleId);
                if (addRole == null)
                    throw new StreamRoleNotFoundException();

                try
                {
                    //check if user is in the fromrole
                    if (user.RoleIds.Contains(setting.FromRoleId))
                    {
                        //check if he doesn't have addrole already, to avoid errors
                        if (!user.RoleIds.Contains(setting.AddRoleId))
                            await user.AddRoleAsync(addRole).ConfigureAwait(false);
                        //schedule him for the role removal when he stops streaming
                        toRemove.TryAdd((addRole.Guild.Id, user.Id), addRole.Id);
                    }
                }
                catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    StopStreamRole(user.Guild.Id);
                    _log.Warn("Error adding stream role(s). Disabling stream role feature.");
                    _log.Error(ex);
                    throw new StreamRolePermissionException();
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed adding stream role.");
                    _log.Error(ex);
                }
            }
        }

        /// <summary>
        /// Adds or removes a user from a blacklist or a whitelist in the specified guild.
        /// </summary>
        /// <param name="guildId">Id of the guild</param>
        /// <param name="action">Add or rem action</param>
        /// <param name="userId">User's Id</param>
        /// <param name="userName">User's name#discrim</param>
        /// <returns>Whether the operation was successful</returns>
        public async Task<bool> ApplyListAction(StreamRoleListType listType, ulong guildId, AddRemove action, ulong userId, string userName)
        {
            userName.ThrowIfNull(nameof(userName));

            bool success;
            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(guildId);

                if (listType == StreamRoleListType.Whitelist)
                {
                    var userObj = new StreamRoleWhitelistedUser()
                    {
                        UserId = userId,
                        Username = userName,
                    };

                    if (action == AddRemove.Rem)
                        success = streamRoleSettings.Whitelist.Remove(userObj);
                    else
                        success = streamRoleSettings.Whitelist.Add(userObj);
                }
                else
                {
                    var userObj = new StreamRoleBlacklistedUser()
                    {
                        UserId = userId,
                        Username = userName,
                    };

                    if (action == AddRemove.Rem)
                        success = streamRoleSettings.Blacklist.Remove(userObj);
                    else
                        success = streamRoleSettings.Blacklist.Add(userObj);
                }

                await uow.CompleteAsync().ConfigureAwait(false);
            }
            return success;
        }

        /// <summary>
        /// Sets keyword on a guild and updates the cache.
        /// </summary>
        /// <param name="guildId">Guild Id</param>
        /// <param name="keyword">Keyword to set</param>
        /// <returns>The keyword set</returns>
        public string SetKeyword(ulong guildId, string keyword)
        {
            keyword = keyword?.Trim()?.ToLowerInvariant();

            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(guildId);

                streamRoleSettings.Keyword = keyword;
                UpdateCache(guildId, streamRoleSettings);
                uow.Complete();

                return streamRoleSettings.Keyword;
            }

        }

        /// <summary>
        /// Gets the currently set keyword on a guild.
        /// </summary>
        /// <param name="guildId">Guild Id</param>
        /// <returns>The keyword set</returns>
        public string GetKeyword(ulong guildId)
        {
            if (guildSettings.TryGetValue(guildId, out var outSetting))
                return outSetting.Keyword;

            StreamRoleSettings setting;
            using (var uow = _db.UnitOfWork)
            {
                setting = uow.GuildConfigs.GetStreamRoleSettings(guildId);
            }

            UpdateCache(guildId, setting);

            return setting.Keyword;
        }

        /// <summary>
        /// Sets the role to monitor, and a role to which to add to 
        /// the user who starts streaming in the monitored role.
        /// </summary>
        /// <param name="fromRole">Role to monitor</param>
        /// <param name="addRole">Role to add to the user</param>
        public async Task SetStreamRole(IRole fromRole, IRole addRole)
        {
            fromRole.ThrowIfNull(nameof(fromRole));
            addRole.ThrowIfNull(nameof(addRole));

            StreamRoleSettings setting;
            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(fromRole.Guild.Id);

                streamRoleSettings.Enabled = true;
                streamRoleSettings.AddRoleId = addRole.Id;
                streamRoleSettings.FromRoleId = fromRole.Id;

                setting = streamRoleSettings;
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            UpdateCache(fromRole.Guild.Id, setting);

            foreach (var usr in await fromRole.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false))
            {
                await TryApplyRole(usr, setting).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops the stream role feature on the specified guild.
        /// </summary>
        /// <param name="guildId">Guild's Id</param>
        public void StopStreamRole(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(guildId);
                streamRoleSettings.Enabled = false;
                uow.Complete();
            }

            guildSettings.TryRemove(guildId, out _);
        }

        private void UpdateCache(ulong guildId, StreamRoleSettings setting)
        {
            guildSettings.AddOrUpdate(guildId, (key) => setting, (key, old) => setting);
        }
    }
}
