using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
using NLog;

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

            var sw = Stopwatch.StartNew();
            if (client.ShardId == 0)
                TryMigratePermissions();

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

        private void TryMigratePermissions()
        {
            using (var uow = _db.UnitOfWork)
            {
                var bc = uow.BotConfig.GetOrCreate();
                var log = LogManager.GetCurrentClassLogger();
                if (bc.PermissionVersion <= 1)
                {
                    log.Info("Permission version is 1, upgrading to 2.");
                    var oldCache = new ConcurrentDictionary<ulong, OldPermissionCache>(uow.GuildConfigs
                        .OldPermissionsForAll()
                        .Where(x => x.RootPermission != null) // there is a check inside already, but just in case
                        .ToDictionary(k => k.GuildId,
                            v => new OldPermissionCache()
                            {
                                RootPermission = v.RootPermission,
                                Verbose = v.VerbosePermissions,
                                PermRole = v.PermissionRole
                            }));

                    if (oldCache.Any())
                    {
                        log.Info("Old permissions found. Performing one-time migration to v2.");
                        var i = 0;
                        foreach (var oc in oldCache)
                        {
                            if (i % 3 == 0)
                                log.Info("Migrating Permissions #" + i + " - GuildId: " + oc.Key);
                            i++;
                            var gc = uow.GuildConfigs.GcWithPermissionsv2For(oc.Key);

                            var oldPerms = oc.Value.RootPermission.AsEnumerable().Reverse().ToList();
                            uow._context.Set<Permission>().RemoveRange(oldPerms);
                            gc.RootPermission = null;
                            if (oldPerms.Count > 2)
                            {

                                var newPerms = oldPerms.Take(oldPerms.Count - 1)
                                    .Select(x => x.Tov2())
                                    .ToList();

                                var allowPerm = Permissionv2.AllowAllPerm;
                                var firstPerm = newPerms[0];
                                if (allowPerm.State != firstPerm.State ||
                                    allowPerm.PrimaryTarget != firstPerm.PrimaryTarget ||
                                    allowPerm.SecondaryTarget != firstPerm.SecondaryTarget ||
                                    allowPerm.PrimaryTargetId != firstPerm.PrimaryTargetId ||
                                    allowPerm.SecondaryTargetName != firstPerm.SecondaryTargetName)
                                    newPerms.Insert(0, Permissionv2.AllowAllPerm);
                                Cache.TryAdd(oc.Key, new PermissionCache
                                {
                                    Permissions = new PermissionsCollection<Permissionv2>(newPerms),
                                    Verbose = gc.VerbosePermissions,
                                    PermRole = gc.PermissionRole,
                                });
                                gc.Permissions = newPerms;
                            }
                        }
                        log.Info("Permission migration to v2 is done.");
                    }

                    bc.PermissionVersion = 2;
                    uow.Complete();
                }
                if (bc.PermissionVersion <= 2)
                {
                    var oldPrefixes = new[] { ".", ";", "!!", "!m", "!", "+", "-", "$", ">" };
                    uow._context.Database.ExecuteSqlCommand(
    @"UPDATE Permissionv2
SET secondaryTargetName=trim(substr(secondaryTargetName, 3))
WHERE secondaryTargetName LIKE '!!%' OR secondaryTargetName LIKE '!m%';

UPDATE Permissionv2
SET secondaryTargetName=substr(secondaryTargetName, 2)
WHERE secondaryTargetName LIKE '.%' OR
secondaryTargetName LIKE '~%' OR
secondaryTargetName LIKE ';%' OR
secondaryTargetName LIKE '>%' OR
secondaryTargetName LIKE '-%' OR
secondaryTargetName LIKE '!%';");
                    bc.PermissionVersion = 3;
                    uow.Complete();
                }
            }
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
                    var roles = (user as SocketGuildUser)?.Roles ?? ((IGuildUser)user).RoleIds.Select(x => guild.GetRole(x)).Where(x => x != null);
                    if (!roles.Any(r => r.Name.Trim().ToLowerInvariant() == pc.PermRole.Trim().ToLowerInvariant()) && user.Id != ((IGuildUser)user).Guild.OwnerId)
                    {
                        var returnMsg = $"You need the **{pc.PermRole}** role in order to use permission commands.";
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