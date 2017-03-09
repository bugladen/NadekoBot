using NadekoBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Services;
using Discord;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Discord.WebSocket;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NadekoBot.DataStructures;
using NLog;

namespace NadekoBot.Modules.Permissions
{
    [NadekoModule("Permissions", ";")]
    public partial class Permissions : NadekoTopLevelModule
    {
        public class OldPermissionCache
        {
            public string PermRole { get; set; }
            public bool Verbose { get; set; } = true;
            public Permission RootPermission { get; set; }
        }

        public class PermissionCache
        {
            public string PermRole { get; set; }
            public bool Verbose { get; set; } = true;
            public PermissionsCollection<Permissionv2> Permissions { get; set; }
        }

        //guildid, root permission
        public static ConcurrentDictionary<ulong, PermissionCache> Cache { get; } =
            new ConcurrentDictionary<ulong, PermissionCache>();

        static Permissions()
        {
            var log = LogManager.GetCurrentClassLogger();
            var sw = Stopwatch.StartNew();

            TryMigratePermissions();

            using (var uow = DbHandler.UnitOfWork())
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
            log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
        }

        private static void TryMigratePermissions()
        {
            var log = LogManager.GetCurrentClassLogger();
            using (var uow = DbHandler.UnitOfWork())
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
                        var gc = uow.GuildConfigs.For(oc.Key, set => set.Include(x => x.Permissions));

                        var oldPerms = oc.Value.RootPermission.AsEnumerable().Reverse().ToList();
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

        private static async Task AddPermissions(ulong guildId, params Permissionv2[] perms)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(guildId, set => set.Include(x => x.Permissions));
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

        public static void UpdateCache(GuildConfig config)
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Verbose(PermissionAction action)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                config.VerbosePermissions = action.Value;
                await uow.CompleteAsync().ConfigureAwait(false);
                UpdateCache(config);
            }
            if (action.Value)
            {
                await ReplyConfirmLocalized("verbose_true").ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("verbose_false").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task PermRole([Remainder] IRole role = null)
        {
            if (role != null && role == role.Guild.EveryoneRole)
                return;

            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                if (role == null)
                {
                    await ReplyConfirmLocalized("permrole", Format.Bold(config.PermissionRole)).ConfigureAwait(false);
                    return;
                }
                config.PermissionRole = role.Name.Trim();
                await uow.CompleteAsync().ConfigureAwait(false);
                UpdateCache(config);
            }

            await ReplyConfirmLocalized("permrole_changed", Format.Bold(role.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListPerms(int page = 1)
        {
            if (page < 1 || page > 4)
                return;

            PermissionCache permCache;
            IList<Permissionv2> perms;

            if (Cache.TryGetValue(Context.Guild.Id, out permCache))
            {
                perms = permCache.Permissions.Source.ToList();
            }
            else
            {
                perms = Permissionv2.GetDefaultPermlist;
            }

            var startPos = 20 * (page - 1);
            var toSend = Format.Bold(GetText("page", page)) + "\n\n" + string.Join("\n",
                             perms.Reverse()
                                 .Skip((page - 1) * 20)
                                 .Take(20)
                                 .Select(p =>
                                 {
                                     var str =
                                         $"`{p.Index + startPos + 1}.` {Format.Bold(p.GetCommand((SocketGuild) Context.Guild))}";
                                     if (p.Index == 0)
                                         str += $" [{GetText("uneditable")}]";
                                     return str;
                                 }));

            await Context.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RemovePerm(int index)
        {
            index -= 1;
            if (index < 0)
                return;
            try
            {
                Permissionv2 p;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.Permissions));
                    var permsCol = new PermissionsCollection<Permissionv2>(config.Permissions);
                    p = permsCol[index];
                    permsCol.RemoveAt(index);
                    uow._context.Remove(p);
                    await uow.CompleteAsync().ConfigureAwait(false);
                    UpdateCache(config);
                }
                await ReplyConfirmLocalized("removed",
                    index + 1,
                    Format.Code(p.GetCommand((SocketGuild) Context.Guild))).ConfigureAwait(false);
            }
            catch (IndexOutOfRangeException)
            {
                await ReplyErrorLocalized("perm_out_of_range").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MovePerm(int from, int to)
        {
            from -= 1;
            to -= 1;
            if (!(from == to || from < 0 || to < 0))
            {
                try
                {
                    Permissionv2 fromPerm;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.Permissions));
                        var permsCol = new PermissionsCollection<Permissionv2>(config.Permissions);

                        var fromFound = from < permsCol.Count;
                        var toFound = to < permsCol.Count;

                        if (!fromFound)
                        {
                            await ReplyErrorLocalized("not_found", ++from).ConfigureAwait(false);
                            return;
                        }

                        if (!toFound)
                        {
                            await ReplyErrorLocalized("not_found", ++to).ConfigureAwait(false);
                            return;
                        }
                        fromPerm = permsCol[from];

                        permsCol.RemoveAt(from);
                        permsCol.Insert(to, fromPerm);
                        await uow.CompleteAsync().ConfigureAwait(false);
                        UpdateCache(config);
                    }
                    await ReplyConfirmLocalized("moved_permission",
                            Format.Code(fromPerm.GetCommand((SocketGuild) Context.Guild)),
                            ++from,
                            ++to)
                        .ConfigureAwait(false);
                    return;
                }
                catch (Exception e) when (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                {
                }
            }
            await ReplyErrorLocalized("perm_out_of_range").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrCmd(CommandInfo command, PermissionAction action)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Server,
                PrimaryTargetId = 0,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("sx_enable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command")).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("sx_disable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command")).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrMdl(ModuleInfo module, PermissionAction action)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Server,
                PrimaryTargetId = 0,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("sx_enable",
                    Format.Code(module.Name),
                    GetText("of_module")).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("sx_disable",
                    Format.Code(module.Name),
                    GetText("of_module")).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UsrCmd(CommandInfo command, PermissionAction action, [Remainder] IGuildUser user)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("ux_enable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command"),
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("ux_disable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command"),
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UsrMdl(ModuleInfo module, PermissionAction action, [Remainder] IGuildUser user)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("ux_enable",
                    Format.Code(module.Name),
                    GetText("of_module"),
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("ux_disable",
                    Format.Code(module.Name),
                    GetText("of_module"),
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleCmd(CommandInfo command, PermissionAction action, [Remainder] IRole role)
        {
            if (role == role.Guild.EveryoneRole)
                return;

            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("rx_enable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command"),
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("rx_disable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command"),
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleMdl(ModuleInfo module, PermissionAction action, [Remainder] IRole role)
        {
            if (role == role.Guild.EveryoneRole)
                return;

            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value,
            });


            if (action.Value)
            {
                await ReplyConfirmLocalized("rx_enable",
                    Format.Code(module.Name),
                    GetText("of_module"),
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("rx_disable",
                    Format.Code(module.Name),
                    GetText("of_module"),
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlCmd(CommandInfo command, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("cx_enable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command"),
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("cx_disable",
                    Format.Code(command.Aliases.First()),
                    GetText("of_command"),
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlMdl(ModuleInfo module, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("cx_enable",
                    Format.Code(module.Name),
                    GetText("of_module"),
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("cx_disable",
                    Format.Code(module.Name),
                    GetText("of_module"),
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllChnlMdls(PermissionAction action, [Remainder] ITextChannel chnl)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("acm_enable",
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("acm_disable",
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllRoleMdls(PermissionAction action, [Remainder] IRole role)
        {
            if (role == role.Guild.EveryoneRole)
                return;

            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("arm_enable",
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("arm_disable",
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllUsrMdls(PermissionAction action, [Remainder] IUser user)
        {
            await AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("aum_enable",
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("aum_disable",
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllSrvrMdls(PermissionAction action)
        {
            var newPerm = new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Server,
                PrimaryTargetId = 0,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value,
            };

            var allowUser = new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = Context.User.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = true,
            };

            await AddPermissions(Context.Guild.Id,
                newPerm,
                allowUser);

            if (action.Value)
            {
                await ReplyConfirmLocalized("asm_enable").ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("asm_disable").ConfigureAwait(false);
            }
        }
    }
}