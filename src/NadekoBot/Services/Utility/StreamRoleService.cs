using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using NadekoBot.Extensions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace NadekoBot.Services.Utility
{
    public class StreamRoleService
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
               .ToConcurrent();

            client.GuildMemberUpdated += Client_GuildMemberUpdated;
        }

        private Task Client_GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var _ = Task.Run(async () =>
            {
                //if user wasn't streaming or didn't have a game status at all
                // and has a game status now
                // and that status is a streaming status
                // and we are supposed to give him a role
                if ((!before.Game.HasValue || before.Game.Value.StreamType == StreamType.NotStreaming) &&
                    after.Game.HasValue &&
                    after.Game.Value.StreamType != StreamType.NotStreaming
                    && guildSettings.TryGetValue(after.Guild.Id, out var setting))
                {
                    IRole fromRole;
                    IRole addRole;
                    try
                    {
                        //get needed roles
                        fromRole = after.Guild.GetRole(setting.FromRoleId);
                        if (fromRole == null)
                            throw new InvalidOperationException();
                        addRole = after.Guild.GetRole(setting.AddRoleId);
                        if (addRole == null)
                            throw new InvalidOperationException();
                    }
                    catch (Exception ex)
                    {
                        StopStreamRole(before.Guild.Id);
                        _log.Warn("Error getting Stream Role(s). Disabling stream role feature.");
                        _log.Error(ex);
                        return;
                    }

                    try
                    {
                        //check if user is in the fromrole
                        if (after.Roles.Contains(fromRole))
                        { 
                            //check if he doesn't have addrole already, to avoid errors
                            if(!after.Roles.Contains(addRole))
                                await after.AddRoleAsync(addRole).ConfigureAwait(false);
                            //schedule him for the role removal when he stops streaming
                            toRemove.TryAdd((addRole.Guild.Id, after.Id), addRole.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Failed adding stream role.");
                        _log.Error(ex);
                    }
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

        public void SetStreamRole(IRole fromRole, IRole addRole)
        {
            StreamRoleSettings setting;
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(fromRole.Guild.Id, x => x.Include(y => y.StreamRole));

                if (gc.StreamRole == null)
                    gc.StreamRole = new StreamRoleSettings()
                    {
                        AddRoleId = addRole.Id,
                        FromRoleId = fromRole.Id
                    };
                else
                {
                    gc.StreamRole.AddRoleId = addRole.Id;
                    gc.StreamRole.FromRoleId = fromRole.Id;
                }
                setting = gc.StreamRole;
                uow.Complete();
            }

            guildSettings.AddOrUpdate(fromRole.Guild.Id, (key) => setting, (key, old) => setting);
        }

        public void StopStreamRole(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, x => x.Include(y => y.StreamRole));
                gc.StreamRole = null;
                uow.Complete();
            }

            guildSettings.TryRemove(guildId, out _);
        }
    }
}
