using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using SixLabors.ImageSharp.PixelFormats;
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
                var msgs = await ((SocketTextChannel)ctx.Channel).GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
                var prev = (IUserMessage)msgs.FirstOrDefault(x => x is IUserMessage && x.Id != ctx.Message.Id);

                if (prev == null)
                    return;

                if (input.Length % 2 != 0)
                    return;

                var g = (SocketGuild)ctx.Guild;

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

                if(_service.Add(ctx.Guild.Id, new ReactionRoleMessage()
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
                    await ctx.Channel.SendConfirmAsync(":ok:")
                        .ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalizedAsync("reaction_roles_full").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [Priority(0)]
            public Task ReactionRoles(params string[] input) =>
                InternalReactionRoles(false, input);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [Priority(1)]
            public Task ReactionRoles(Excl _, params string[] input) =>
                InternalReactionRoles(true, input);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ReactionRolesList()
            {
                var embed = new EmbedBuilder()
                    .WithOkColor();
                if(!_service.Get(ctx.Guild.Id, out var rrs) || 
                    !rrs.Any())
                {
                    embed.WithDescription(GetText("no_reaction_roles"));
                }
                else
                {
                    var g = ((SocketGuild)ctx.Guild);
                    foreach (var rr in rrs)
                    {
                        var ch = g.GetTextChannel(rr.ChannelId);
                        var msg = (await (ch?.GetMessageAsync(rr.MessageId)).ConfigureAwait(false)) as IUserMessage;
                        var content = msg?.Content.TrimTo(30) ?? "DELETED!"; 
                        embed.AddField($"**{rr.Index + 1}.** {(ch?.Name ?? "DELETED!")}", 
                            GetText("reaction_roles_message", rr.ReactionRoles?.Count ?? 0, content));
                    }
                }
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NoPublicBot]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ReactionRolesRemove(int index)
            {
                if(index < 1 || index > 5 || 
                    !_service.Get(ctx.Guild.Id, out var rrs) ||
                    !rrs.Any() || rrs.Count < index)
                {
                    return;
                }
                index--;
                var rr = rrs[index];
                _service.Remove(ctx.Guild.Id, index);
                await ReplyConfirmLocalizedAsync("reaction_role_removed", index + 1).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task Setrole(IGuildUser usr, [Leftover] IRole role)
            {
                var guser = (IGuildUser)ctx.User;
                var maxRole = guser.GetRoles().Max(x => x.Position);
                if ((ctx.User.Id != ctx.Guild.OwnerId) && (maxRole <= role.Position || maxRole <= usr.GetRoles().Max(x => x.Position)))
                    return;
                try
                {
                    await usr.AddRoleAsync(role).ConfigureAwait(false);
                           
                    await ReplyConfirmLocalizedAsync("setrole", Format.Bold(role.Name), Format.Bold(usr.ToString()))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await ReplyErrorLocalizedAsync("setrole_err").ConfigureAwait(false);
                    _log.Info(ex);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task Removerole(IGuildUser usr, [Leftover] IRole role)
            {
                var guser = (IGuildUser)ctx.User;
                if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= usr.GetRoles().Max(x => x.Position))
                    return;
                try
                {
                    await usr.RemoveRoleAsync(role).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("remrole", Format.Bold(role.Name), Format.Bold(usr.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("remrole_err").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RenameRole(IRole roleToEdit, string newname)
            {
                var guser = (IGuildUser)ctx.User;
                if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= roleToEdit.Position)
                    return;
                try
                {
                    if (roleToEdit.Position > (await ctx.Guild.GetCurrentUserAsync().ConfigureAwait(false)).GetRoles().Max(r => r.Position))
                    {
                        await ReplyErrorLocalizedAsync("renrole_perms").ConfigureAwait(false);
                        return;
                    }
                    await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("renrole").ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync("renrole_err").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RemoveAllRoles([Leftover] IGuildUser user)
            {
                var guser = (IGuildUser)ctx.User;

                var userRoles = user.GetRoles().Except(new[] { guser.Guild.EveryoneRole });
                if (user.Id == ctx.Guild.OwnerId || (ctx.User.Id != ctx.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= userRoles.Max(x => x.Position)))
                    return;
                try
                {
                    await user.RemoveRolesAsync(userRoles).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("rar", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync("rar_err").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task CreateRole([Leftover] string roleName = null)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return;

                var r = await ctx.Guild.CreateRoleAsync(roleName).ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync("cr", Format.Bold(r.Name)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task DeleteRole([Leftover] IRole role)
            {
                var guser = (IGuildUser)ctx.User;
                if (ctx.User.Id != guser.Guild.OwnerId 
                    && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                await role.DeleteAsync().ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync("dr", Format.Bold(role.Name)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RoleHoist(IRole role)
            {
                await role.ModifyAsync(r => r.Hoist = !role.IsHoisted).ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync("rh", Format.Bold(role.Name), Format.Bold(role.IsHoisted.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task RoleColor([Leftover] IRole role)
            {
                await ctx.Channel.SendConfirmAsync("Role Color", role.Color.RawValue.ToString("X")).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [Priority(0)]
            public async Task RoleColor(IRole role, Rgba32 color)
            {
                try
                {
                    await role.ModifyAsync(r => r.Color = new Color(color.R, color.G, color.B)).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("rc", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync("rc_perms").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.MentionEveryone)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task MentionRole([Leftover] IRole role)
            {
                if(!role.IsMentionable)
                {
                    await role.ModifyAsync(x => x.Mentionable = true).ConfigureAwait(false);
                    await ctx.Channel.SendMessageAsync(role.Mention).ConfigureAwait(false);
                    await role.ModifyAsync(x => x.Mentionable = false).ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(role.Mention).ConfigureAwait(false);
                }
            }
        }
    }
}
