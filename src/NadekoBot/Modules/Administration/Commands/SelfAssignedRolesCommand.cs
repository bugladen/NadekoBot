using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SelfAssignedRolesCommands : NadekoSubmodule
        {
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task AdSarm()
            {
                bool newval;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    newval = config.AutoDeleteSelfAssignedRoleMessages = !config.AutoDeleteSelfAssignedRoleMessages;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync($"ℹ️ Automatic deleting of `iam` and `iamn` confirmations has been {(newval ? "**enabled**" : "**disabled**")}.")
                             .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Asar([Remainder] IRole role)
            {
                IEnumerable<SelfAssignedRole> roles;

                string msg;
                var error = false;
                using (var uow = DbHandler.UnitOfWork())
                {
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                    if (roles.Any(s => s.RoleId == role.Id && s.GuildId == role.Guild.Id))
                    {
                        msg = GetText("role_in_list", Format.Bold(role.Name));
                        error = true;
                    }
                    else
                    {
                        uow.SelfAssignedRoles.Add(new SelfAssignedRole
                        {
                            RoleId = role.Id,
                            GuildId = role.Guild.Id
                        });
                        await uow.CompleteAsync();
                        msg = GetText("role_added", Format.Bold(role.Name));
                    }
                }
                if (error)
                    await Context.Channel.SendErrorAsync(msg).ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync(msg).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Rsar([Remainder] IRole role)
            {
                bool success;
                using (var uow = DbHandler.UnitOfWork())
                {
                    success = uow.SelfAssignedRoles.DeleteByGuildAndRoleId(role.Guild.Id, role.Id);
                    await uow.CompleteAsync();
                }
                if (!success)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }
                await ReplyConfirmLocalized("self_assign_rem", Format.Bold(role.Name)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Lsar()
            {
                var toRemove = new ConcurrentHashSet<SelfAssignedRole>();
                var removeMsg = new StringBuilder();
                var msg = new StringBuilder();
                var roleCnt = 0;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var roleModels = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id).ToList();
                    roleCnt = roleModels.Count;
                    msg.AppendLine();
                    
                    foreach (var roleModel in roleModels)
                    {
                        var role = Context.Guild.Roles.FirstOrDefault(r => r.Id == roleModel.RoleId);
                        if (role == null)
                        {
                            uow.SelfAssignedRoles.Remove(roleModel);
                        }
                        else
                        {
                            msg.Append($"**{role.Name}**, ");
                        }
                    }
                    foreach (var role in toRemove)
                    {
                        removeMsg.AppendLine(GetText("role_clean", role.RoleId));
                    }
                    await uow.CompleteAsync();
                }
                await Context.Channel.SendConfirmAsync(GetText("self_assign_list", roleCnt), msg + "\n\n" + removeMsg).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Tesar()
            {
                bool areExclusive;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);

                    areExclusive = config.ExclusiveSelfAssignedRoles = !config.ExclusiveSelfAssignedRoles;
                    await uow.CompleteAsync();
                }
                if(areExclusive)
                    await ReplyConfirmLocalized("self_assign_excl").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("self_assign_no_excl").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iam([Remainder] IRole role)
            {
                var guildUser = (IGuildUser)Context.User;

                GuildConfig conf;
                IEnumerable<SelfAssignedRole> roles;
                using (var uow = DbHandler.UnitOfWork())
                {
                    conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                }
                if (roles.FirstOrDefault(r=>r.RoleId == role.Id) == null)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }
                if (guildUser.RoleIds.Contains(role.Id))
                {
                    await ReplyErrorLocalized("self_assign_already", Format.Bold(role.Name)).ConfigureAwait(false);
                    return;
                }

                if (conf.ExclusiveSelfAssignedRoles)
                {
                    var sameRoleId = guildUser.RoleIds.FirstOrDefault(r => roles.Select(sar => sar.RoleId).Contains(r));
                    
                    if (sameRoleId != default(ulong))
                    {
                        var sameRole = Context.Guild.GetRole(sameRoleId);
                        if (sameRole != null)
                            await guildUser.RemoveRolesAsync(sameRole).ConfigureAwait(false);
                        //await ReplyErrorLocalized("self_assign_already_excl", Format.Bold(sameRole?.Name)).ConfigureAwait(false);
                        //return;
                    }
                }
                try
                {
                    await guildUser.AddRolesAsync(role).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
                    Console.WriteLine(ex);
                    return;
                }
                var msg = await ReplyConfirmLocalized("self_assign_success",Format.Bold(role.Name)).ConfigureAwait(false);

                if (conf.AutoDeleteSelfAssignedRoleMessages)
                {
                    msg.DeleteAfter(3);
                    Context.Message.DeleteAfter(3);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iamnot([Remainder] IRole role)
            {
                var guildUser = (IGuildUser)Context.User;

                bool autoDeleteSelfAssignedRoleMessages;
                IEnumerable<SelfAssignedRole> roles;
                using (var uow = DbHandler.UnitOfWork())
                {
                    autoDeleteSelfAssignedRoleMessages = uow.GuildConfigs.For(Context.Guild.Id, set => set).AutoDeleteSelfAssignedRoleMessages;
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                }
                if (roles.FirstOrDefault(r => r.RoleId == role.Id) == null)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }
                if (!guildUser.RoleIds.Contains(role.Id))
                {
                    await ReplyErrorLocalized("self_assign_not_have",Format.Bold(role.Name)).ConfigureAwait(false);
                    return;
                }
                try
                {
                    await guildUser.RemoveRolesAsync(role).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
                    return;
                }
                var msg = await ReplyConfirmLocalized("self_assign_remove", Format.Bold(role.Name)).ConfigureAwait(false);

                if (autoDeleteSelfAssignedRoleMessages)
                {
                    msg.DeleteAfter(3);
                    Context.Message.DeleteAfter(3);
                }
            }
        }
    }
}
