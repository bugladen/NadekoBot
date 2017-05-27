using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Services.Permissions
{
    public class PermissionsService
    {
        private readonly DbHandler _db;
        private readonly Logger _log;

        //guildid, root permission
        public ConcurrentDictionary<ulong, PermissionCache> Cache { get; } =
            new ConcurrentDictionary<ulong, PermissionCache>();

        public PermissionsService(DbHandler db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;

            var sw = Stopwatch.StartNew();
            TryMigratePermissions();
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

        private void TryMigratePermissions()
        {
            var log = LogManager.GetCurrentClassLogger();
            using (var uow = _db.UnitOfWork)
            {
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
    }
}
