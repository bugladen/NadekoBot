using Microsoft.EntityFrameworkCore;
using NadekoBot.DataStructures.ModuleBehaviors;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace NadekoBot.Services.Permissions
{
    public class PermissionsService : ILateBlocker
    {
        private readonly DbService _db;
        private readonly Logger _log;

        //guildid, root permission
        public ConcurrentDictionary<ulong, PermissionCache> Cache { get; } =
            new ConcurrentDictionary<ulong, PermissionCache>();

        public PermissionsService(DbService db, BotConfig bc)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;

            var sw = Stopwatch.StartNew();
            TryMigratePermissions(bc);
            using (var uow = _db.UnitOfWork)
            {
                foreach (var x in uow.GuildConfigs.Permissionsv2ForAll())
                {
                    Cache.TryAdd(x.GuildId, new PermissionCache()
                    {
                        Verbose = x.VerbosePermissions,
                        PermRole = x.PermissionRole,
                        Permissions = new PermissionsCollection<Permissionv2>(x.Permissions)
                    });
                }
            }

            sw.Stop();
            _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
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

        private void TryMigratePermissions(BotConfig bc)
        {
            var log = LogManager.GetCurrentClassLogger();
            using (var uow = _db.UnitOfWork)
            {
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

                    uow.BotConfig.GetOrCreate().PermissionVersion = 2;
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

        public async Task<bool> TryBlockLate(DiscordShardedClient client, IUserMessage msg, IGuild guild, IMessageChannel channel, IUser user, string moduleName, string commandName)
        {
            await Task.Yield();
            if (guild == null)
            {

                return false;
            }

            var resetCommand = commandName == "resetperms";

            //todo perms
            PermissionCache pc = GetCache(guild.Id);
            if (!resetCommand && !pc.Permissions.CheckPermissions(msg, commandName, moduleName, out int index))
            {
                var returnMsg = $"Permission number #{index + 1} **{pc.Permissions[index].GetCommand((SocketGuild)guild)}** is preventing this action.";
                return true;
                //return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, returnMsg));
            }


            if (moduleName == "Permissions")
            {
                var roles = (user as SocketGuildUser)?.Roles ?? ((IGuildUser)user).RoleIds.Select(x => guild.GetRole(x)).Where(x => x != null);
                if (!roles.Any(r => r.Name.Trim().ToLowerInvariant() == pc.PermRole.Trim().ToLowerInvariant()) && user.Id != ((IGuildUser)user).Guild.OwnerId)
                {
                    return true;
                    //return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, $"You need the **{pc.PermRole}** role in order to use permission commands."));
                }
            }

            return false;
        }
    }
}
