using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Core.Services;
using Discord;
using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions : NadekoTopLevelModule<PermissionService>
    {
        private readonly DbService _db;

        public Permissions(DbService db)
        {
            _db = db;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Verbose(PermissionAction action)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                config.VerbosePermissions = action.Value;
                await uow.CompleteAsync().ConfigureAwait(false);
                _service.UpdateCache(config);
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
        [RequireUserPermission(GuildPermission.Administrator)]
        [Priority(0)]
        public async Task PermRole([Remainder] IRole role = null)
        {
            if (role != null && role == role.Guild.EveryoneRole)
                return;
            
            if (role == null)
            {
                var cache = _service.GetCache(Context.Guild.Id);
                if (!ulong.TryParse(cache.PermRole, out var roleId) ||
                    (role = ((SocketGuild)Context.Guild).GetRole(roleId)) == null)
                {
                    await ReplyConfirmLocalized("permrole_not_set", Format.Bold(cache.PermRole)).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("permrole", Format.Bold(role.ToString())).ConfigureAwait(false);
                }
                return;
            }

            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                config.PermissionRole = role.Id.ToString();
                uow.Complete();
                _service.UpdateCache(config);
            }

            await ReplyConfirmLocalized("permrole_changed", Format.Bold(role.Name)).ConfigureAwait(false);
        }

        public enum Reset { Reset };

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Priority(1)]
        public async Task PermRole(Reset _)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                config.PermissionRole = null;
                await uow.CompleteAsync().ConfigureAwait(false);
                _service.UpdateCache(config);
            }

            await ReplyConfirmLocalized("permrole_reset").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListPerms(int page = 1)
        {
            if (page < 1)
                return;

            IList<Permissionv2> perms;

            if (_service.Cache.TryGetValue(Context.Guild.Id, out var permCache))
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
                                 .Skip(startPos)
                                 .Take(20)
                                 .Select(p =>
                                 {
                                     var str =
                                         $"`{p.Index + 1}.` {Format.Bold(p.GetCommand(Prefix, (SocketGuild)Context.Guild))}";
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
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                    var permsCol = new PermissionsCollection<Permissionv2>(config.Permissions);
                    p = permsCol[index];
                    permsCol.RemoveAt(index);
                    uow._context.Remove(p);
                    await uow.CompleteAsync().ConfigureAwait(false);
                    _service.UpdateCache(config);
                }
                await ReplyConfirmLocalized("removed",
                    index + 1,
                    Format.Code(p.GetCommand(Prefix, (SocketGuild)Context.Guild))).ConfigureAwait(false);
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
                    using (var uow = _db.UnitOfWork)
                    {
                        var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
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
                        _service.UpdateCache(config);
                    }
                    await ReplyConfirmLocalized("moved_permission",
                            Format.Code(fromPerm.GetCommand(Prefix, (SocketGuild)Context.Guild)),
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
        public async Task SrvrCmd(CommandOrCrInfo command, PermissionAction action)
        {
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Server,
                PrimaryTargetId = 0,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("sx_enable",
                    Format.Code(command.Name),
                    GetText("of_command")).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("sx_disable",
                    Format.Code(command.Name),
                    GetText("of_command")).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrMdl(ModuleOrCrInfo module, PermissionAction action)
        {
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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
        public async Task UsrCmd(CommandOrCrInfo command, PermissionAction action, [Remainder] IGuildUser user)
        {
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("ux_enable",
                    Format.Code(command.Name),
                    GetText("of_command"),
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("ux_disable",
                    Format.Code(command.Name),
                    GetText("of_command"),
                    Format.Code(user.ToString())).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UsrMdl(ModuleOrCrInfo module, PermissionAction action, [Remainder] IGuildUser user)
        {
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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
        public async Task RoleCmd(CommandOrCrInfo command, PermissionAction action, [Remainder] IRole role)
        {
            if (role == role.Guild.EveryoneRole)
                return;

            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("rx_enable",
                    Format.Code(command.Name),
                    GetText("of_command"),
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("rx_disable",
                    Format.Code(command.Name),
                    GetText("of_command"),
                    Format.Code(role.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleMdl(ModuleOrCrInfo module, PermissionAction action, [Remainder] IRole role)
        {
            if (role == role.Guild.EveryoneRole)
                return;

            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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
        public async Task ChnlCmd(CommandOrCrInfo command, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
            });

            if (action.Value)
            {
                await ReplyConfirmLocalized("cx_enable",
                    Format.Code(command.Name),
                    GetText("of_command"),
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("cx_disable",
                    Format.Code(command.Name),
                    GetText("of_command"),
                    Format.Code(chnl.Name)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlMdl(ModuleOrCrInfo module, PermissionAction action, [Remainder] ITextChannel chnl)
        {
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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

            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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
            await _service.AddPermissions(Context.Guild.Id, new Permissionv2
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

            await _service.AddPermissions(Context.Guild.Id,
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