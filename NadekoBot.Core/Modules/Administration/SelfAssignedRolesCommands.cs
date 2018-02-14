using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Common;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SelfAssignedRolesCommands : NadekoSubmodule
        {
            private readonly DbService _db;

            public SelfAssignedRolesCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task AdSarm()
            {
                bool newval;
                using (var uow = _db.UnitOfWork)
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
            [Priority(1)]
            public Task Asar([Remainder] IRole role) =>
                Asar(0, role);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public async Task Asar(int group, [Remainder] IRole role)
            {
                IEnumerable<SelfAssignedRole> roles;

                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                string msg;
                var error = false;
                using (var uow = _db.UnitOfWork)
                {
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id)
                        .SelectMany(x => x);
                    if (roles.Any(s => s.RoleId == role.Id && s.GuildId == role.Guild.Id))
                    {
                        msg = GetText("role_in_list", Format.Bold(role.Name));
                        error = true;
                    }
                    else
                    {
                        uow.SelfAssignedRoles.Add(new SelfAssignedRole
                        {
                            Group = group,
                            RoleId = role.Id,
                            GuildId = role.Guild.Id
                        });
                        await uow.CompleteAsync();
                        msg = GetText("role_added", Format.Bold(role.Name), Format.Bold(group.ToString()));
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
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                bool success;
                using (var uow = _db.UnitOfWork)
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
            public async Task Lsar(int page = 1)
            {
                if (--page < 0)
                    return;

                var toRemove = new ConcurrentHashSet<SelfAssignedRole>();
                var removeMsg = new StringBuilder();
                var rolesStr = new StringBuilder();
                var roleCnt = 0;
                var exclusive = false;
                using (var uow = _db.UnitOfWork)
                {
                    exclusive = uow.GuildConfigs.For(Context.Guild.Id, set => set)
                        .ExclusiveSelfAssignedRoles;
                    var roleModels = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id)
                        .OrderBy(x => x.Key);

                    var skip = page * 20;
                    foreach (var kvp in roleModels)
                    {
                        var cnt = kvp.Count();
                        if (skip >= cnt)
                        {
                            skip -= cnt;
                            continue;
                        }
                        if (skip < -20)
                            break;
                        rolesStr.AppendLine("\t\t\t\t『" + Format.Bold(GetText("self_assign_group", kvp.Key)) + "』");
                        foreach (var roleModel in kvp.AsEnumerable())
                        {
                            if (skip-- > 0)
                            {
                                continue;
                            }
                            if (skip < -20)
                            {
                                break;
                            }

                            var role = Context.Guild.Roles.FirstOrDefault(r => r.Id == roleModel.RoleId);
                            if (role == null)
                            {
                                toRemove.Add(roleModel);
                                uow.SelfAssignedRoles.Remove(roleModel);
                            }
                            else
                            {
                                if (roleModel.LevelRequirement == 0)
                                    rolesStr.AppendLine(Format.Bold(role.Name));
                                else
                                    rolesStr.AppendLine(Format.Bold(role.Name) + $" (lvl {roleModel.LevelRequirement}+)");
                                roleCnt++;
                            }
                        }
                    }
                    if (toRemove.Any())
                        rolesStr.AppendLine("\t\t\t\t『』");
                    foreach (var role in toRemove)
                    {
                        rolesStr.AppendLine(GetText("role_clean", role.RoleId));
                    }
                    await uow.CompleteAsync();
                }

                await Context.Channel.SendConfirmAsync("",
                    Format.Bold(GetText("self_assign_list", roleCnt))
                    + "\n\n" + rolesStr.ToString(),
                    footer: exclusive
                    ? GetText("self_assign_are_exclusive")
                    : GetText("self_assign_are_not_exclusive")).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Tesar()
            {
                bool areExclusive;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);

                    areExclusive = config.ExclusiveSelfAssignedRoles = !config.ExclusiveSelfAssignedRoles;
                    await uow.CompleteAsync();
                }
                if (areExclusive)
                    await ReplyConfirmLocalized("self_assign_excl").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("self_assign_no_excl").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task RoleLevelReq(int level, [Remainder] IRole role)
            {
                if (level < 0)
                    return;

                bool notExists = false;
                using (var uow = _db.UnitOfWork)
                {
                    var roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
                    var sar = roles.SelectMany(x => x).FirstOrDefault(x => x.RoleId == role.Id);
                    if (sar != null)
                    {
                        sar.LevelRequirement = level;
                        uow.Complete();
                    }
                    else
                    {
                        notExists = true;
                    }
                }

                if (notExists)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("self_assign_level_req",
                    Format.Bold(role.Name),
                    Format.Bold(level.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iam([Remainder] IRole role)
            {
                var guildUser = (IGuildUser)Context.User;

                GuildConfig conf;
                SelfAssignedRole[] roles;
                LevelStats userLevelData;
                using (var uow = _db.UnitOfWork)
                {
                    conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id)
                        .SelectMany(x => x)
                        .ToArray();

                    var stats = uow.Xp.GetOrCreateUser(Context.Guild.Id, Context.User.Id);
                    userLevelData = new LevelStats(stats.Xp + stats.AwardedXp);
                }
                var theRoleYouWant = roles.FirstOrDefault(r => r.RoleId == role.Id);
                if (theRoleYouWant == null)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }
                if (theRoleYouWant.LevelRequirement > userLevelData.Level)
                {
                    await ReplyErrorLocalized("self_assign_not_level", Format.Bold(theRoleYouWant.LevelRequirement.ToString())).ConfigureAwait(false);
                    return;
                }
                if (guildUser.RoleIds.Contains(role.Id))
                {
                    await ReplyErrorLocalized("self_assign_already", Format.Bold(role.Name)).ConfigureAwait(false);
                    return;
                }

                var roleIds = roles
                    .Where(x => x.Group == theRoleYouWant.Group)
                    .Select(x => x.RoleId).ToArray();
                if (conf.ExclusiveSelfAssignedRoles)
                {
                    var sameRoles = guildUser.RoleIds
                        .Where(r => roleIds.Contains(r));

                    foreach (var roleId in sameRoles)
                    {
                        var sameRole = Context.Guild.GetRole(roleId);
                        if (sameRole != null)
                        {
                            try
                            {
                                await guildUser.RemoveRoleAsync(sameRole).ConfigureAwait(false);
                                await Task.Delay(300).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _log.Warn(ex);
                            }
                        }
                    }
                }
                try
                {
                    await guildUser.AddRoleAsync(role).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
                    _log.Info(ex);
                    return;
                }
                var msg = await ReplyConfirmLocalized("self_assign_success", Format.Bold(role.Name)).ConfigureAwait(false);

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
                using (var uow = _db.UnitOfWork)
                {
                    autoDeleteSelfAssignedRoleMessages = uow.GuildConfigs.For(Context.Guild.Id, set => set).AutoDeleteSelfAssignedRoleMessages;
                    roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id)
                        .SelectMany(x => x)
                        .ToArray();
                }
                if (roles.FirstOrDefault(r => r.RoleId == role.Id) == null)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }
                if (!guildUser.RoleIds.Contains(role.Id))
                {
                    await ReplyErrorLocalized("self_assign_not_have", Format.Bold(role.Name)).ConfigureAwait(false);
                    return;
                }
                try
                {
                    await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
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