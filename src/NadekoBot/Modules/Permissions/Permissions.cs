using NadekoBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Services;
using Discord;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using NadekoBot.Extensions;
using Discord.WebSocket;
using System.Diagnostics;
using NLog;

namespace NadekoBot.Modules.Permissions
{
    [NadekoModule("Permissions", ";")]
    public partial class Permissions : NadekoTopLevelModule
    {
        public class PermissionCache
        {
            public string PermRole { get; set; }
            public bool Verbose { get; set; } = true;
            public Permission RootPermission { get; set; }
        }

        //guildid, root permission
        public static ConcurrentDictionary<ulong, PermissionCache> Cache { get; }

        static Permissions()
        {
            var log = LogManager.GetCurrentClassLogger();
            var sw = Stopwatch.StartNew();

            using (var uow = DbHandler.UnitOfWork())
            {
                Cache = new ConcurrentDictionary<ulong, PermissionCache>(uow.GuildConfigs
                                                                       .PermissionsForAll()
                                                                       .ToDictionary(k => k.GuildId,
                                                                            v => new PermissionCache()
                                                                            {
                                                                                RootPermission = v.RootPermission,
                                                                                Verbose = v.VerbosePermissions,
                                                                                PermRole = v.PermissionRole
                                                                            }));
            }

            sw.Stop();
            log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Verbose(PermissionAction action)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                config.VerbosePermissions = action.Value;
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = Permission.GetDefaultRoot(),
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.Verbose = config.VerbosePermissions; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
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
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = Permission.GetDefaultRoot(),
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.PermRole = role.Name.Trim(); return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            await ReplyConfirmLocalized("permrole_changed", Format.Bold(role.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListPerms(int page = 1)
        {
            if (page < 1 || page > 4)
                return;
            string toSend;
            using (var uow = DbHandler.UnitOfWork())
            {
                var perms = uow.GuildConfigs.PermissionsFor(Context.Guild.Id).RootPermission;
                var i = 1 + 20 * (page - 1);
                toSend = Format.Bold(GetText("page", page)) + "\n\n" + string.Join("\n",
                             perms.AsEnumerable()
                                 .Skip((page - 1) * 20)
                                 .Take(20)
                                 .Select(
                                     p =>
                                         $"`{(i++)}.` {(p.Next == null ? Format.Bold(p.GetCommand((SocketGuild) Context.Guild) + $" [{GetText("uneditable")}]") : (p.GetCommand((SocketGuild) Context.Guild)))}"));
            }

            await Context.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RemovePerm(int index)
        {
            index -= 1;
            try
            {
                Permission p;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.PermissionsFor(Context.Guild.Id);
                    var perms = config.RootPermission;
                    if (index == perms.Count() - 1)
                    {
                        return;
                    }
                    if (index == 0)
                    {
                        p = perms;
                        config.RootPermission = perms.Next;
                    }
                    else
                    {
                        p = perms.RemoveAt(index);
                    }
                    Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                    {
                        PermRole = config.PermissionRole,
                        RootPermission = config.RootPermission,
                        Verbose = config.VerbosePermissions
                    }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                using (var uow2 = DbHandler.UnitOfWork())
                {
                    uow2._context.Remove<Permission>(p);
                    uow2._context.SaveChanges();
                }
                await ReplyConfirmLocalized("removed", 
                    index+1,
                    Format.Code(p.GetCommand((SocketGuild)Context.Guild))).ConfigureAwait(false);
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
                    Permission fromPerm = null;
                    Permission toPerm = null;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var config = uow.GuildConfigs.PermissionsFor(Context.Guild.Id);
                        var perms = config.RootPermission;
                        var index = 0;
                        var fromFound = false;
                        var toFound = false;
                        while ((!toFound || !fromFound) && perms != null)
                        {
                            if (index == from)
                            {
                                fromPerm = perms;
                                fromFound = true;
                            }
                            if (index == to)
                            {
                                toPerm = perms;
                                toFound = true;
                            }
                            if (!toFound)
                            {
                                toPerm = perms; //In case of to > size
                            }
                            perms = perms.Next;
                            index++;
                        }
                        if (perms == null)
                        {
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
                        }

                        //Change chain for from indx
                        var next = fromPerm.Next;
                        var pre = fromPerm.Previous;
                        if (pre != null)
                            pre.Next = next;
                        if (fromPerm.Next == null || toPerm.Next == null)
                        {
                            throw new IndexOutOfRangeException();
                        }
                        next.Previous = pre;
                        if (from == 0)
                        {
                        }
                        await uow.CompleteAsync().ConfigureAwait(false);
                        //Inserting
                        if (to > from)
                        {
                            fromPerm.Previous = toPerm;
                            fromPerm.Next = toPerm.Next;

                            toPerm.Next.Previous = fromPerm;
                            toPerm.Next = fromPerm;
                        }
                        else
                        {
                            pre = toPerm.Previous;

                            fromPerm.Next = toPerm;
                            fromPerm.Previous = pre;

                            toPerm.Previous = fromPerm;
                            if (pre != null)
                                pre.Next = fromPerm;
                        }

                        config.RootPermission = fromPerm.GetRoot();
                        Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                        {
                            PermRole = config.PermissionRole,
                            RootPermission = config.RootPermission,
                            Verbose = config.VerbosePermissions
                        }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                        await uow.CompleteAsync().ConfigureAwait(false);
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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Server,
                    PrimaryTargetId = 0,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });

                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Server,
                    PrimaryTargetId = 0,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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

            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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

            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }


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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Aliases.First().ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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

            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Server,
                    PrimaryTargetId = 0,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                };
                uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, newPerm);

                var allowUser = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = Context.User.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = true,
                };

                var config = uow.GuildConfigs.SetNewRootPermission(Context.Guild.Id, allowUser);
                Cache.AddOrUpdate(Context.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

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
