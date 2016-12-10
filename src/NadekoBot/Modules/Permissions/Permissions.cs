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

namespace NadekoBot.Modules.Permissions
{
    [NadekoModule("Permissions", ";")]
    public partial class Permissions : DiscordModule
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
        }

        public Permissions() : base()
        {
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Verbose(IUserMessage msg, PermissionAction action)
        {
            var channel = (ITextChannel)msg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(channel.Guild.Id, set => set);
                config.VerbosePermissions = action.Value;
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = Permission.GetDefaultRoot(),
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.Verbose = config.VerbosePermissions; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            await channel.SendConfirmAsync("ℹ️ I will " + (action.Value ? "now" : "no longer") + " show permission warnings.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task PermRole(IUserMessage msg, [Remainder] IRole role = null)
        {
            var channel = (ITextChannel)msg.Channel;
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(channel.Guild.Id, set => set);
                if (role == null)
                {
                    await channel.SendConfirmAsync($"ℹ️ Current permission role is **{config.PermissionRole}**.").ConfigureAwait(false);
                    return;
                }
                else {
                    config.PermissionRole = role.Name.Trim();
                    Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                    {
                        PermRole = config.PermissionRole,
                        RootPermission = Permission.GetDefaultRoot(),
                        Verbose = config.VerbosePermissions
                    }, (id, old) => { old.PermRole = role.Name.Trim(); return old; });
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            await channel.SendConfirmAsync($"Users now require **{role.Name}** role in order to edit permissions.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListPerms(IUserMessage msg, int page = 1)
        {
            var channel = (ITextChannel)msg.Channel;

            if (page < 1 || page > 4)
                return;
            string toSend = "";
            using (var uow = DbHandler.UnitOfWork())
            {
                var perms = uow.GuildConfigs.PermissionsFor(channel.Guild.Id).RootPermission;
                var i = 1 + 20 * (page - 1);
                toSend = Format.Code($"📄 Permissions page {page}") + "\n\n" + String.Join("\n", perms.AsEnumerable().Skip((page - 1) * 20).Take(20).Select(p => $"`{(i++)}.` {(p.Next == null ? Format.Bold(p.GetCommand(channel.Guild) + " [uneditable]") : (p.GetCommand(channel.Guild)))}"));
            }

            await channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RemovePerm(IUserMessage imsg, int index)
        {
            var channel = (ITextChannel)imsg.Channel;
            index -= 1;
            try
            {
                Permission p;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.PermissionsFor(channel.Guild.Id);
                    var perms = config.RootPermission;
                    if (index == perms.Count() - 1)
                    {
                        return;
                    }
                    else if (index == 0)
                    {
                        p = perms;
                        config.RootPermission = perms.Next;
                    }
                    else
                    {
                        p = perms.RemoveAt(index);
                    }
                    Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
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

                await channel.SendConfirmAsync($"✅ {imsg.Author.Mention} removed permission **{p.GetCommand(channel.Guild)}** from position #{index + 1}.").ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                await channel.SendErrorAsync("❗️`No command on that index found.`").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MovePerm(IUserMessage imsg, int from, int to)
        {
            from -= 1;
            to -= 1;
            var channel = (ITextChannel)imsg.Channel;
            if (!(from == to || from < 0 || to < 0))
            {
                try
                {
                    Permission fromPerm = null;
                    Permission toPerm = null;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var config = uow.GuildConfigs.PermissionsFor(channel.Guild.Id);
                        var perms = config.RootPermission;
                        var root = perms;
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
                                await channel.SendErrorAsync($"Can't find permission at index `#{++from}`").ConfigureAwait(false);
                                return;
                            }

                            if (!toFound)
                            {
                                await channel.SendErrorAsync($"Can't find permission at index `#{++to}`").ConfigureAwait(false);
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
                            root = next;
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
                        Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                        {
                            PermRole = config.PermissionRole,
                            RootPermission = config.RootPermission,
                            Verbose = config.VerbosePermissions
                        }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await channel.SendConfirmAsync($"`Moved permission:` \"{fromPerm.GetCommand(channel.Guild)}\" `from #{++from} to #{++to}.`").ConfigureAwait(false);
                    return;
                }
                catch (Exception e) when (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                {
                }
            }
            await channel.SendErrorAsync("`Invalid index(es) specified.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrCmd(IUserMessage imsg, Command command, PermissionAction action)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Server,
                    PrimaryTargetId = 0,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Text.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });

                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{command.Text}` command on this server.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrMdl(IUserMessage imsg, Module module, PermissionAction action)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of **`{module.Name}`** module on this server.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UsrCmd(IUserMessage imsg, Command command, PermissionAction action, [Remainder] IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Text.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{command.Text}` command for `{user}` user.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UsrMdl(IUserMessage imsg, Module module, PermissionAction action, [Remainder] IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{module.Name}` module for `{user}` user.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleCmd(IUserMessage imsg, Command command, PermissionAction action, [Remainder] IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                var newPerm = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Text.ToLowerInvariant(),
                    State = action.Value,
                };
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{command.Text}` command for `{role}` role.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleMdl(IUserMessage imsg, Module module, PermissionAction action, [Remainder] IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{module.Name}` module for `{role}` role.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlCmd(IUserMessage imsg, Command command, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;
            try
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var newPerm = new Permission
                    {
                        PrimaryTarget = PrimaryPermissionType.Channel,
                        PrimaryTargetId = chnl.Id,
                        SecondaryTarget = SecondaryPermissionType.Command,
                        SecondaryTargetName = command.Text.ToLowerInvariant(),
                        State = action.Value,
                    };
                    var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                    Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                    {
                        PermRole = config.PermissionRole,
                        RootPermission = config.RootPermission,
                        Verbose = config.VerbosePermissions
                    }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{command.Text}` command for `{chnl}` channel.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlMdl(IUserMessage imsg, Module module, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `{module.Name}` module for `{chnl}` channel.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllChnlMdls(IUserMessage imsg, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `ALL MODULES` for `{chnl}` channel.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllRoleMdls(IUserMessage imsg, PermissionAction action, [Remainder] IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `ALL MODULES` for `{role}` role.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllUsrMdls(IUserMessage imsg, PermissionAction action, [Remainder] IUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `ALL MODULES` for `{user}` user.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AllSrvrMdls(IUserMessage imsg, PermissionAction action)
        {
            var channel = (ITextChannel)imsg.Channel;

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
                uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, newPerm);

                var allowUser = new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = imsg.Author.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = true,
                };

                var config = uow.GuildConfigs.SetNewRootPermission(channel.Guild.Id, allowUser);
                Cache.AddOrUpdate(channel.Guild.Id, new PermissionCache()
                {
                    PermRole = config.PermissionRole,
                    RootPermission = config.RootPermission,
                    Verbose = config.VerbosePermissions
                }, (id, old) => { old.RootPermission = config.RootPermission; return old; });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendConfirmAsync($"{(action.Value ? "✅ Allowed" : "🆗 Denied")} usage of `ALL MODULES` on this server.").ConfigureAwait(false);
        }
    }
}
