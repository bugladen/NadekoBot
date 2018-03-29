using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Modules.Permissions.Services
{
    public class PermissionService : ILateBlocker, INService
    {
        private readonly DbService _db;
        private readonly CommandHandler _cmd;
        private readonly NadekoStrings _strings;

        //guildid, root permission
        public ConcurrentDictionary<ulong, PermissionCache> Cache { get; } =
            new ConcurrentDictionary<ulong, PermissionCache>();

        public PermissionService(DiscordSocketClient client, DbService db, CommandHandler cmd, NadekoStrings strings)
        {
            _db = db;
            _cmd = cmd;
            _strings = strings;

            using (var uow = _db.UnitOfWork)
            {
                foreach (var x in uow.GuildConfigs.Permissionsv2ForAll(client.Guilds.ToArray().Select(x => (long)x.Id).ToList()))
                {
                    Cache.TryAdd(x.GuildId, new PermissionCache()
                    {
                        Verbose = x.VerbosePermissions,
                        PermRole = x.PermissionRole,
                        Permissions = new PermissionsCollection<Permissionv2>(x.Permissions)
                    });
                }
            }
        }

        public PermissionCache GetCache(ulong guildId)
        {
            if (!Cache.TryGetValue(guildId, out var pc))
            {
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(guildId,
                        set => set.Include(x => x.Permissions));
                    UpdateCache(config);
                }
                Cache.TryGetValue(guildId, out pc);
                if (pc == null)
                    throw new Exception("Cache is null.");
            }
            return pc;
        }

        public async Task AddPermissions(ulong guildId, params Permissionv2[] perms)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.GcWithPermissionsv2For(guildId);
                //var orderedPerms = new PermissionsCollection<Permissionv2>(config.Permissions);
                var max = config.Permissions.Max(x => x.Index); //have to set its index to be the highest
                foreach (var perm in perms)
                {
                    perm.Index = ++max;
                    config.Permissions.Add(perm);
                }
                await uow.CompleteAsync().ConfigureAwait(false);
                UpdateCache(config);
            }
        }

        public void UpdateCache(GuildConfig config)
        {
            Cache.AddOrUpdate(config.GuildId, new PermissionCache()
            {
                Permissions = new PermissionsCollection<Permissionv2>(config.Permissions),
                PermRole = config.PermissionRole,
                Verbose = config.VerbosePermissions
            }, (id, old) =>
            {
                old.Permissions = new PermissionsCollection<Permissionv2>(config.Permissions);
                old.PermRole = config.PermissionRole;
                old.Verbose = config.VerbosePermissions;
                return old;
            });
        }

        public async Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage msg, IGuild guild, IMessageChannel channel, IUser user, string moduleName, string commandName)
        {
            await Task.Yield();
            if (guild == null)
            {
                return false;
            }
            else
            {
                var resetCommand = commandName == "resetperms";

                PermissionCache pc = GetCache(guild.Id);
                if (!resetCommand && !pc.Permissions.CheckPermissions(msg, commandName, moduleName, out int index))
                {
                    if (pc.Verbose)
                        try { await channel.SendErrorAsync(_strings.GetText("trigger", guild.Id, "Permissions".ToLowerInvariant(), index + 1, Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), (SocketGuild)guild)))).ConfigureAwait(false); } catch { }
                    return true;
                }


                if (moduleName == "Permissions")
                {
                    var guildUser = user as IGuildUser;
                    if (guildUser == null)
                        return true;

                    var permRole = pc.PermRole;
                    ulong rid = 0;
                    if (!(guildUser.GuildPermissions.Administrator
                        && (string.IsNullOrWhiteSpace(permRole)
                            || !ulong.TryParse(permRole, out rid)
                            || !guildUser.RoleIds.Contains(rid))))
                    {
                        string returnMsg;
                        IRole role;
                        if (string.IsNullOrWhiteSpace(permRole) || (role = guild.GetRole(rid)) == null)
                        {
                            returnMsg = $"You need Admin permissions in order to use permission commands.";
                        }
                        else
                        {
                            returnMsg = $"You need the {Format.Bold(role.Name)} role in order to use permission commands.";
                        }
                        if (pc.Verbose)
                            try { await channel.SendErrorAsync(returnMsg).ConfigureAwait(false); } catch { }
                        return true;
                    }
                }
            }

            return false;
        }
    }
}