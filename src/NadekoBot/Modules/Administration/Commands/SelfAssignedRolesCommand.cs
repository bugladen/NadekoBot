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
        public class SelfAssignedRolesCommands
        {
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task AdSarm()
            {
                //var channel = (ITextChannel)Context.Channel;
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
            public async Task Asar(IUserMessage umsg, [Remainder] IRole role)
            {
                //var channel = (ITextChannel)Context.Channel;

                IEnumerable<SelfAssignedRole> roles;

                string msg;
                using (var uow = DbHandler.UnitOfWork())
                {
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                    if (roles.Any(s => s.RoleId == role.Id && s.GuildId == role.GuildId))
                    {
                        await Context.Channel.SendMessageAsync($"💢 Role **{role.Name}** is already in the list.").ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        uow.SelfAssignedRoles.Add(new SelfAssignedRole {
                            RoleId = role.Id,
                            GuildId = role.GuildId
                        });
                        await uow.CompleteAsync();
                        msg = $"🆗 Role **{role.Name}** added to the list.";
                    }
                }
                await Context.Channel.SendConfirmAsync(msg.ToString()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Rsar(IUserMessage umsg, [Remainder] IRole role)
            {
                //var channel = (ITextChannel)Context.Channel;

                bool success;
                using (var uow = DbHandler.UnitOfWork())
                {
                    success = uow.SelfAssignedRoles.DeleteByGuildAndRoleId(role.GuildId, role.Id);
                    await uow.CompleteAsync();
                }
                if (!success)
                {
                    await Context.Channel.SendErrorAsync("❎ That role is not self-assignable.").ConfigureAwait(false);
                    return;
                }
                await Context.Channel.SendConfirmAsync($"🗑 **{role.Name}** has been removed from the list of self-assignable roles.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Lsar()
            {
                //var channel = (ITextChannel)Context.Channel;

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
                        removeMsg.AppendLine($"`{role.RoleId} not found. Cleaned up.`");
                    }
                    await uow.CompleteAsync();
                }
                await Context.Channel.SendConfirmAsync($"ℹ️ There are `{roleCnt}` self assignable roles:", msg.ToString() + "\n\n" + removeMsg.ToString()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Tesar()
            {
                //var channel = (ITextChannel)Context.Channel;

                bool areExclusive;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);

                    areExclusive = config.ExclusiveSelfAssignedRoles = !config.ExclusiveSelfAssignedRoles;
                    await uow.CompleteAsync();
                }
                string exl = areExclusive ? "**exclusive**." : "**not exclusive**.";
                await Context.Channel.SendConfirmAsync("ℹ️ Self assigned roles are now " + exl);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iam(IUserMessage umsg, [Remainder] IRole role)
            {
                //var channel = (ITextChannel)Context.Channel;
                var guildUser = (IGuildUser)Context.User;
                var usrMsg = (IUserMessage)umsg;

                GuildConfig conf;
                IEnumerable<SelfAssignedRole> roles;
                using (var uow = DbHandler.UnitOfWork())
                {
                    conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                }
                SelfAssignedRole roleModel;
                if ((roleModel = roles.FirstOrDefault(r=>r.RoleId == role.Id)) == null)
                {
                    await Context.Channel.SendErrorAsync("That role is not self-assignable.").ConfigureAwait(false);
                    return;
                }
                if (guildUser.Roles.Contains(role))
                {
                    await Context.Channel.SendErrorAsync($"You already have **{role.Name}** role.").ConfigureAwait(false);
                    return;
                }

                if (conf.ExclusiveSelfAssignedRoles)
                {
                    var sameRoles = guildUser.Roles.Where(r => roles.Any(rm => rm.RoleId == r.Id));
                    if (sameRoles.Any())
                    {
                        await Context.Channel.SendErrorAsync($"You already have **{sameRoles.FirstOrDefault().Name}** `exclusive self-assigned` role.").ConfigureAwait(false);
                        return;
                    }
                }
                try
                {
                    await guildUser.AddRolesAsync(role).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync($"⚠️ I am unable to add that role to you. `I can't add roles to owners or other roles higher than my role in the role hierarchy.`").ConfigureAwait(false);
                    Console.WriteLine(ex);
                    return;
                }
                var msg = await Context.Channel.SendConfirmAsync($"🆗 You now have **{role.Name}** role.").ConfigureAwait(false);

                if (conf.AutoDeleteSelfAssignedRoleMessages)
                {
                    var t = Task.Run(async () =>
                    {
                        await Task.Delay(3000).ConfigureAwait(false);
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { } // if 502 or something, i don't want bot crashing
                        try { await usrMsg.DeleteAsync().ConfigureAwait(false); } catch { }
                    });
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iamnot(IUserMessage umsg, [Remainder] IRole role)
            {
                //var channel = (ITextChannel)Context.Channel;
                var guildUser = (IGuildUser)Context.User;

                bool autoDeleteSelfAssignedRoleMessages;
                IEnumerable<SelfAssignedRole> roles;
                using (var uow = DbHandler.UnitOfWork())
                {
                    autoDeleteSelfAssignedRoleMessages = uow.GuildConfigs.For(Context.Guild.Id, set => set).AutoDeleteSelfAssignedRoleMessages;
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                }
                SelfAssignedRole roleModel;
                if ((roleModel = roles.FirstOrDefault(r => r.RoleId == role.Id)) == null)
                {
                    await Context.Channel.SendErrorAsync("💢 That role is not self-assignable.").ConfigureAwait(false);
                    return;
                }
                if (!guildUser.Roles.Contains(role))
                {
                    await Context.Channel.SendErrorAsync($"❎ You don't have **{role.Name}** role.").ConfigureAwait(false);
                    return;
                }
                try
                {
                    await guildUser.RemoveRolesAsync(role).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await Context.Channel.SendErrorAsync($"⚠️ I am unable to add that role to you. `I can't remove roles to owners or other roles higher than my role in the role hierarchy.`").ConfigureAwait(false);
                    return;
                }
                var msg = await Context.Channel.SendConfirmAsync($"🆗 You no longer have **{role.Name}** role.").ConfigureAwait(false);

                if (autoDeleteSelfAssignedRoleMessages)
                {
                    var t = Task.Run(async () =>
                    {
                        await Task.Delay(3000).ConfigureAwait(false);
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { } // if 502 or something, i don't want bot crashing
                        try { await umsg.DeleteAsync().ConfigureAwait(false); } catch { }
                    });
                }
            }
        }
    }
}
