using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        public class RoleCommands : NadekoSubmodule<RoleCommandsService>
        {
            public enum Excl { Excl }

            public async Task InternalReactionRoles(bool exclusive, params string[] input)
            {
                var msgs = await ((SocketTextChannel)Context.Channel).GetMessagesAsync().FlattenAsync();
                var prev = (IUserMessage)msgs.FirstOrDefault(x => x is IUserMessage && x.Id != Context.Message.Id);

                if (prev == null)
                    return;

                if (input.Length % 2 != 0)
                    return;

                var g = (SocketGuild)Context.Guild;

                var grp = 0;
                var all = input
                    .GroupBy(x => grp++ / 2)
                    .Select(x =>
                    {
                        var inputRoleStr = x.First().ToLowerInvariant();
                        var role = g.Roles.FirstOrDefault(y => y.Name.ToLowerInvariant() == inputRoleStr);
                        if (role == null)
                        {
                            _log.Warn("Role {0} not found.", inputRoleStr);
                            return null;
                        }
                        var emote = g.Emotes.FirstOrDefault(y => y.ToString() == x.Last());
                        if (emote == null)
                        {
                            _log.Warn("Emote {0} not found.", x.Last());
                            return null;
                        }
                        else
                            return new { role, emote };
                    })
                    .Where(x => x != null);

                if (!all.Any())
                    return;

                foreach (var x in all)
                {
                    await prev.AddReactionAsync(x.emote, new RequestOptions()
                    {
                        RetryMode = RetryMode.Retry502 | RetryMode.RetryRatelimit
                    }).ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                }

                if(_service.Add(Context.Guild.Id, new ReactionRoleMessage()
                {
                    Exclusive = exclusive,
                    MessageId = prev.Id,
                    ChannelId = prev.Channel.Id,
                    ReactionRoles = all.Select(x =>
                    {
                        return new ReactionRole()
                        {
                            EmoteName = x.emote.Name,
                            RoleId = x.role.Id,
                        };
                    }).ToList(),
                }))
                {
                    await Context.Channel.SendConfirmAsync(":ok:")
                        .ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalized("reaction_roles_full").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public Task ReactionRoles(params string[] input) =>
                InternalReactionRoles(false, input);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [Priority(1)]
            public Task ReactionRoles(Excl _, params string[] input) =>
                InternalReactionRoles(true, input);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task ReactionRolesList()
            {
                var embed = new EmbedBuilder()
                    .WithOkColor();
                if(!_service.Get(Context.Guild.Id, out var rrs) || 
                    !rrs.Any())
                {
                    embed.WithDescription(GetText("no_reaction_roles"));
                }
                else
                {
                    var g = ((SocketGuild)Context.Guild);
                    foreach (var rr in rrs)
                    {
                        var ch = g.GetTextChannel(rr.ChannelId);
                        var msg = (await ch?.GetMessageAsync(rr.MessageId)) as IUserMessage;
                        var content = msg?.Content.TrimTo(30) ?? "DELETED!"; 
                        embed.AddField($"**{rr.Index + 1}.** {(ch?.Name ?? "DELETED!")}", 
                            GetText("reaction_roles_message", rr.ReactionRoles?.Count ?? 0, content));
                    }
                }
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task ReactionRolesRemove(int index)
            {
                if(index < 1 || index > 5 || 
                    !_service.Get(Context.Guild.Id, out var rrs) ||
                    !rrs.Any() || rrs.Count < index)
                {
                    return;
                }
                index--;
                var rr = rrs[index];
                _service.Remove(Context.Guild.Id, index);
                await ReplyConfirmLocalized("reaction_role_removed", index + 1).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task Setrole(IGuildUser usr, [Remainder] IRole role)
            {
                var guser = (IGuildUser)Context.User;
                var maxRole = guser.GetRoles().Max(x => x.Position);
                if ((Context.User.Id != Context.Guild.OwnerId) && (maxRole <= role.Position || maxRole <= usr.GetRoles().Max(x => x.Position)))
                    return;
                try
                {
                    await usr.AddRoleAsync(role).ConfigureAwait(false);
                           
                    await ReplyConfirmLocalized("setrole", Format.Bold(role.Name), Format.Bold(usr.ToString()))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await ReplyErrorLocalized("setrole_err").ConfigureAwait(false);
                    _log.Info(ex);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task Removerole(IGuildUser usr, [Remainder] IRole role)
            {
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= usr.GetRoles().Max(x => x.Position))
                    return;
                try
                {
                    await usr.RemoveRoleAsync(role).ConfigureAwait(false);
                    await ReplyConfirmLocalized("remrole", Format.Bold(role.Name), Format.Bold(usr.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("remrole_err").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RenameRole(IRole roleToEdit, string newname)
            {
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= roleToEdit.Position)
                    return;
                try
                {
                    if (roleToEdit.Position > (await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false)).GetRoles().Max(r => r.Position))
                    {
                        await ReplyErrorLocalized("renrole_perms").ConfigureAwait(false);
                        return;
                    }
                    await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
                    await ReplyConfirmLocalized("renrole").ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalized("renrole_err").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RemoveAllRoles([Remainder] IGuildUser user)
            {
                var guser = (IGuildUser)Context.User;

                var userRoles = user.GetRoles().Except(new[] { guser.Guild.EveryoneRole });
                if (user.Id == Context.Guild.OwnerId || (Context.User.Id != Context.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= userRoles.Max(x => x.Position)))
                    return;
                try
                {
                    await user.RemoveRolesAsync(userRoles).ConfigureAwait(false);
                    await ReplyConfirmLocalized("rar", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalized("rar_err").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task CreateRole([Remainder] string roleName = null)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return;

                var r = await Context.Guild.CreateRoleAsync(roleName).ConfigureAwait(false);
                await ReplyConfirmLocalized("cr", Format.Bold(r.Name)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task DeleteRole([Remainder] IRole role)
            {
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId 
                    && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                await role.DeleteAsync().ConfigureAwait(false);
                await ReplyConfirmLocalized("dr", Format.Bold(role.Name)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RoleHoist(IRole role)
            {
                await role.ModifyAsync(r => r.Hoist = !role.IsHoisted).ConfigureAwait(false);
                await ReplyConfirmLocalized("rh", Format.Bold(role.Name), Format.Bold(role.IsHoisted.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task RoleColor([Remainder] IRole role)
            {
                await Context.Channel.SendConfirmAsync("Role Color", role.Color.RawValue.ToString("X"));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public async Task RoleColor(params string[] args)
            {
                if (args.Length != 2 && args.Length != 4)
                {
                    await ReplyErrorLocalized("rc_params").ConfigureAwait(false);
                    return;
                }
                var roleName = args[0].ToUpperInvariant();
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name.ToUpperInvariant() == roleName);

                if (role == null)
                {
                    await ReplyErrorLocalized("rc_not_exist").ConfigureAwait(false);
                    return;
                }
                try
                {
                    var rgb = args.Length == 4;
                    var arg1 = args[1].Replace("#", "");

                    var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                    var green = Convert.ToByte(rgb ? int.Parse(args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                    var blue = Convert.ToByte(rgb ? int.Parse(args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));

                    await role.ModifyAsync(r => r.Color = new Color(red, green, blue)).ConfigureAwait(false);
                    await ReplyConfirmLocalized("rc", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalized("rc_perms").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MentionEveryone)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task MentionRole([Remainder] IRole role)
            {
                if(!role.IsMentionable)
                {
                    await role.ModifyAsync(x => x.Mentionable = true);
                    await Context.Channel.SendMessageAsync(role.Mention);
                    await role.ModifyAsync(x => x.Mentionable = false);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(role.Mention);
                }
            }
        }
    }
}
