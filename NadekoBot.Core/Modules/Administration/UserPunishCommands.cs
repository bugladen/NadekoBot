using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common.TypeReaders.Models;
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
        [Group]
        public class UserPunishCommands : NadekoSubmodule<UserPunishService>
        {
            private readonly MuteService _mute;

            public UserPunishCommands(MuteService mute)
            {
                _mute = mute;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            public async Task Warn(IGuildUser user, [Leftover] string reason = null)
            {
                if (ctx.User.Id != user.Guild.OwnerId
                    && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)ctx.User).GetRoles().Select(r => r.Position).Max()))
                {
                    await ReplyErrorLocalizedAsync("hierarchy").ConfigureAwait(false);
                    return;
                }
                try
                {
                    await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).EmbedAsync(new EmbedBuilder().WithErrorColor()
                                     .WithDescription(GetText("warned_on", ctx.Guild.ToString()))
                                     .AddField(efb => efb.WithName(GetText("moderator")).WithValue(ctx.User.ToString()))
                                     .AddField(efb => efb.WithName(GetText("reason")).WithValue(reason ?? "-")))
                        .ConfigureAwait(false);
                }
                catch
                {

                }

                PunishmentAction? punishment;
                try
                {
                    punishment = await _service.Warn(ctx.Guild, user.Id, ctx.User, reason).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex.Message);
                    await ReplyErrorLocalizedAsync("cant_apply_punishment").ConfigureAwait(false);
                    return;
                }

                if (punishment == null)
                {
                    await ReplyConfirmLocalizedAsync("user_warned", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync("user_warned_and_punished", Format.Bold(user.ToString()), Format.Bold(punishment.ToString())).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [Priority(2)]
            public Task Warnlog(int page, IGuildUser user)
                => Warnlog(page, user.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(3)]
            public Task Warnlog(IGuildUser user = null)
            {
                if (user == null)
                    user = (IGuildUser)ctx.User;
                return ctx.User.Id == user.Id || ((IGuildUser)ctx.User).GuildPermissions.BanMembers ? Warnlog(user.Id) : Task.CompletedTask;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [Priority(0)]
            public Task Warnlog(int page, ulong userId)
                => InternalWarnlog(userId, page - 1);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [Priority(1)]
            public Task Warnlog(ulong userId)
                => InternalWarnlog(userId, 0);

            private async Task InternalWarnlog(ulong userId, int page)
            {
                if (page < 0)
                    return;
                var warnings = _service.UserWarnings(ctx.Guild.Id, userId);

                warnings = warnings.Skip(page * 9)
                    .Take(9)
                    .ToArray();

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("warnlog_for", (ctx.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString()))
                    .WithFooter(efb => efb.WithText(GetText("page", page + 1)));

                if (!warnings.Any())
                {
                    embed.WithDescription(GetText("warnings_none"));
                }
                else
                {
                    var i = page * 9;
                    foreach (var w in warnings)
                    {
                        i++;
                        var name = GetText("warned_on_by", w.DateAdded.Value.ToString("dd.MM.yyy"), w.DateAdded.Value.ToString("HH:mm"), w.Moderator);
                        if (w.Forgiven)
                            name = Format.Strikethrough(name) + " " + GetText("warn_cleared_by", w.ForgivenBy);

                        embed.AddField(x => x
                            .WithName($"#`{i}` " + name)
                            .WithValue(w.Reason.TrimTo(1020)));
                    }
                }

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            public async Task WarnlogAll(int page = 1)
            {
                if (--page < 0)
                    return;
                var warnings = _service.WarnlogAll(ctx.Guild.Id);

                await ctx.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var ws = warnings.Skip(curPage * 15)
                        .Take(15)
                        .ToArray()
                        .Select(x =>
                        {
                            var all = x.Count();
                            var forgiven = x.Count(y => y.Forgiven);
                            var total = all - forgiven;
                            var usr = ((SocketGuild)ctx.Guild).GetUser(x.Key);
                            return (usr?.ToString() ?? x.Key.ToString()) + $" | {total} ({all} - {forgiven})";
                        });

                    return new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("warnings_list"))
                        .WithDescription(string.Join("\n", ws));
                }, warnings.Length, 15).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            public Task Warnclear(IGuildUser user, int index = 0)
                => Warnclear(user.Id, index);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            public async Task Warnclear(ulong userId, int index = 0)
            {
                if (index < 0)
                    return;
                var success = await _service.WarnClearAsync(ctx.Guild.Id, userId, index, ctx.User.ToString());
                var userStr = Format.Bold((ctx.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString());
                if (index == 0)
                {
                    await ReplyConfirmLocalizedAsync("warnings_cleared", userStr).ConfigureAwait(false);
                }
                else
                {
                    if (success)
                    {
                        await ReplyConfirmLocalizedAsync("warning_cleared", Format.Bold(index.ToString()), userStr)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyErrorLocalizedAsync("warning_clear_fail").ConfigureAwait(false);
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            public async Task WarnPunish(int number, PunishmentAction punish, StoopidTime time = null)
            {
                var success = _service.WarnPunish(ctx.Guild.Id, number, punish, time);

                if (!success)
                    return;

                await ReplyConfirmLocalizedAsync("warn_punish_set",
                    Format.Bold(punish.ToString()),
                    Format.Bold(number.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            public async Task WarnPunish(int number)
            {
                if (!_service.WarnPunish(ctx.Guild.Id, number))
                {
                    return;
                }

                await ReplyConfirmLocalizedAsync("warn_punish_rem",
                    Format.Bold(number.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WarnPunishList()
            {
                var ps = _service.WarnPunishList(ctx.Guild.Id);

                string list;
                if (ps.Any())
                {
                    list = string.Join("\n", ps.Select(x => $"{x.Count} -> {x.Punishment} {(x.Time <= 0 ? "" : x.Time.ToString() + "m")} "));
                }
                else
                {
                    list = GetText("warnpl_none");
                }
                await ctx.Channel.SendConfirmAsync(
                    GetText("warn_punish_list"),
                    list).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [BotPerm(GuildPerm.BanMembers)]
            [Priority(0)]
            public async Task Ban(StoopidTime time, IGuildUser user, [Leftover] string msg = null)
            {
                if (time.Time > TimeSpan.FromDays(49))
                    return;
                if (ctx.User.Id != user.Guild.OwnerId && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)ctx.User).GetRoles().Select(r => r.Position).Max()))
                {
                    await ReplyErrorLocalizedAsync("hierarchy").ConfigureAwait(false);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("bandm", Format.Bold(ctx.Guild.Name), msg)).ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                await _mute.TimedBan(user, time.Time, ctx.User.ToString() + " | " + msg).ConfigureAwait(false);
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle("⛔️ " + GetText("banned_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true))
                        .WithFooter($"{time.Time.Days}d {time.Time.Hours}h {time.Time.Minutes}m"))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [BotPerm(GuildPerm.BanMembers)]
            [Priority(2)]
            public async Task Ban(ulong userId, [Leftover] string msg = null)
            {
                var user = await ctx.Guild.GetUserAsync(userId);
                if (user is null)
                {
                    await ctx.Guild.AddBanAsync(userId, 7, ctx.User.ToString() + " | " + msg);

                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle("⛔️ " + GetText("banned_user"))
                            .AddField(efb => efb.WithName("ID").WithValue(userId.ToString()).WithIsInline(true)))
                        .ConfigureAwait(false);
                }
                else
                {
                    await Ban(user, msg);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [BotPerm(GuildPerm.BanMembers)]
            [Priority(1)]
            public async Task Ban(IGuildUser user, [Leftover] string msg = null)
            {
                if (ctx.User.Id != user.Guild.OwnerId && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)ctx.User).GetRoles().Select(r => r.Position).Max()))
                {
                    await ReplyErrorLocalizedAsync("hierarchy").ConfigureAwait(false);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("bandm", Format.Bold(ctx.Guild.Name), msg)).ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                await ctx.Guild.AddBanAsync(user, 7, ctx.User.ToString() + " | " + msg).ConfigureAwait(false);
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle("⛔️ " + GetText("banned_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [BotPerm(GuildPerm.BanMembers)]
            public async Task Unban([Leftover]string user)
            {
                var bans = await ctx.Guild.GetBansAsync().ConfigureAwait(false);

                var bun = bans.FirstOrDefault(x => x.User.ToString().ToLowerInvariant() == user.ToLowerInvariant());

                if (bun == null)
                {
                    await ReplyErrorLocalizedAsync("user_not_found").ConfigureAwait(false);
                    return;
                }

                await UnbanInternal(bun.User).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [BotPerm(GuildPerm.BanMembers)]
            public async Task Unban(ulong userId)
            {
                var bans = await ctx.Guild.GetBansAsync().ConfigureAwait(false);

                var bun = bans.FirstOrDefault(x => x.User.Id == userId);

                if (bun == null)
                {
                    await ReplyErrorLocalizedAsync("user_not_found").ConfigureAwait(false);
                    return;
                }

                await UnbanInternal(bun.User).ConfigureAwait(false);
            }

            private async Task UnbanInternal(IUser user)
            {
                await ctx.Guild.RemoveBanAsync(user).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync("unbanned_user", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.KickMembers)]
            [UserPerm(GuildPerm.ManageMessages)]
            [BotPerm(GuildPerm.BanMembers)]
            public async Task Softban(IGuildUser user, [Leftover] string msg = null)
            {
                if (ctx.User.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)ctx.User).GetRoles().Select(r => r.Position).Max())
                {
                    await ReplyErrorLocalizedAsync("hierarchy").ConfigureAwait(false);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("sbdm", Format.Bold(ctx.Guild.Name), msg)).ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                await ctx.Guild.AddBanAsync(user, 7, ctx.User.ToString() + " | " + msg).ConfigureAwait(false);
                try { await ctx.Guild.RemoveBanAsync(user).ConfigureAwait(false); }
                catch { await ctx.Guild.RemoveBanAsync(user).ConfigureAwait(false); }

                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle("☣ " + GetText("sb_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.KickMembers)]
            [BotPerm(GuildPerm.KickMembers)]
            public async Task Kick(IGuildUser user, [Leftover] string msg = null)
            {
                if (ctx.Message.Author.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)ctx.User).GetRoles().Select(r => r.Position).Max())
                {
                    await ReplyErrorLocalizedAsync("hierarchy").ConfigureAwait(false);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("kickdm", Format.Bold(ctx.Guild.Name), msg)).ConfigureAwait(false);
                    }
                    catch { }
                }

                await user.KickAsync(ctx.User.ToString() + " | " + msg).ConfigureAwait(false);
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("kicked_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.BanMembers)]
            [BotPerm(GuildPerm.BanMembers)]
            [OwnerOnly]
            public async Task MassKill([Leftover] string people)
            {
                if (string.IsNullOrWhiteSpace(people))
                    return;

                var (bans, missing) = _service.MassKill((SocketGuild)ctx.Guild, people);

                var missStr = string.Join("\n", missing);
                if (string.IsNullOrWhiteSpace(missStr))
                    missStr = "-";

                //send a message but don't wait for it
                var banningMessageTask = ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithDescription(GetText("mass_kill_in_progress", bans.Count()))
                    .AddField(GetText("invalid", missing), missStr)
                    .WithOkColor());

                Bc.Reload();

                //do the banning
                await Task.WhenAll(bans
                    .Where(x => x.Id.HasValue)
                    .Select(x => ctx.Guild.AddBanAsync(x.Id.Value, 7, x.Reason, new RequestOptions()
                    {
                        RetryMode = RetryMode.AlwaysRetry,
                    })))
                    .ConfigureAwait(false);

                //wait for the message and edit it
                var banningMessage = await banningMessageTask.ConfigureAwait(false);

                await banningMessage.ModifyAsync(x => x.Embed = new EmbedBuilder()
                    .WithDescription(GetText("mass_kill_completed", bans.Count()))
                    .AddField(GetText("invalid", missing), missStr)
                    .WithOkColor()
                    .Build()).ConfigureAwait(false);
            }
        }
    }
}
