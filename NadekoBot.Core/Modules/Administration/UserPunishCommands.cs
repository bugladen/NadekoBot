using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;
using System;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class UserPunishCommands : NadekoSubmodule<UserPunishService>
        {
            private readonly DbService _db;
            private readonly ICurrencyService _cs;

            public UserPunishCommands(DbService db, MuteService muteService,
                ICurrencyService cs)
            {
                _db = db;
                _cs = cs;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Warn(IGuildUser user, [Remainder] string reason = null)
            {
                if (Context.User.Id != user.Guild.OwnerId && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max()))
                {
                    await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                    return;
                }
                try
                {
                    await (await user.GetOrCreateDMChannelAsync()).EmbedAsync(new EmbedBuilder().WithErrorColor()
                                     .WithDescription(GetText("warned_on", Context.Guild.ToString()))
                                     .AddField(efb => efb.WithName(GetText("moderator")).WithValue(Context.User.ToString()))
                                     .AddField(efb => efb.WithName(GetText("reason")).WithValue(reason ?? "-")))
                        .ConfigureAwait(false);
                }
                catch
                {

                }
                var punishment = await _service.Warn(Context.Guild, user.Id, Context.User.ToString(), reason).ConfigureAwait(false);

                if (punishment == null)
                {
                    await ReplyConfirmLocalized("user_warned", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("user_warned_and_punished", Format.Bold(user.ToString()), Format.Bold(punishment.ToString())).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [Priority(2)]
            public Task Warnlog(int page, IGuildUser user)
                => Warnlog(page, user.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(3)]
            public Task Warnlog(IGuildUser user = null)
            {
                if (user == null)
                    user = (IGuildUser)Context.User;
                return Context.User.Id == user.Id || ((IGuildUser)Context.User).GuildPermissions.BanMembers ? Warnlog(user.Id) : Task.CompletedTask;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [Priority(0)]
            public Task Warnlog(int page, ulong userId)
                => InternalWarnlog(userId, page - 1);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [Priority(1)]
            public Task Warnlog(ulong userId)
                => InternalWarnlog(userId, 0);

            private async Task InternalWarnlog(ulong userId, int page)
            {
                if (page < 0)
                    return;
                Warning[] warnings;
                using (var uow = _db.UnitOfWork)
                {
                    warnings = uow.Warnings.For(Context.Guild.Id, userId);
                }

                warnings = warnings.Skip(page * 9)
                    .Take(9)
                    .ToArray();

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("warnlog_for", (Context.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString()))
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

                await Context.Channel.EmbedAsync(embed);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task WarnlogAll(int page = 1)
            {
                if (--page < 0)
                    return;
                IGrouping<ulong, Warning>[] warnings;
                using (var uow = _db.UnitOfWork)
                {
                    warnings = uow.Warnings.GetForGuild(Context.Guild.Id).GroupBy(x => x.UserId).ToArray();
                }

                await Context.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var ws = warnings.Skip(curPage * 15)
                        .Take(15)
                        .ToArray()
                        .Select(x =>
                        {
                            var all = x.Count();
                            var forgiven = x.Count(y => y.Forgiven);
                            var total = all - forgiven;
                            var usr = ((SocketGuild)Context.Guild).GetUser(x.Key);
                            return (usr?.ToString() ?? x.Key.ToString()) + $" | {total} ({all} - {forgiven})";
                        });

                    return new EmbedBuilder()
                        .WithTitle(GetText("warnings_list"))
                        .WithDescription(string.Join("\n", ws));
                }, warnings.Length, 15);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public Task Warnclear(IGuildUser user, int index = 0)
                => Warnclear(user.Id, index);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Warnclear(ulong userId, int index = 0)
            {
                if (index < 0)
                    return;
                var success = false;
                using (var uow = _db.UnitOfWork)
                {
                    if (index == 0)
                    {
                        await uow.Warnings.ForgiveAll(Context.Guild.Id, userId, Context.User.ToString()).ConfigureAwait(false);
                    }
                    else
                    {
                        success = uow.Warnings.Forgive(Context.Guild.Id, userId, Context.User.ToString(), index - 1);
                    }
                    uow.Complete();
                }
                var userStr = Format.Bold((Context.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString());
                if (index == 0)
                {
                    await ReplyConfirmLocalized("warnings_cleared", userStr).ConfigureAwait(false);
                }
                else
                {
                    if (success)
                    {
                        await ReplyConfirmLocalized("warning_cleared", Format.Bold(index.ToString()), userStr)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyErrorLocalized("warning_clear_fail").ConfigureAwait(false);
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task WarnPunish(int number, PunishmentAction punish, int time = 0)
            {
                if (punish != PunishmentAction.Mute && time != 0)
                    return;
                if (number <= 0)
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    var ps = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
                    ps.RemoveAll(x => x.Count == number);

                    ps.Add(new WarningPunishment()
                    {
                        Count = number,
                        Punishment = punish,
                        Time = time,
                    });
                    uow.Complete();
                }

                await ReplyConfirmLocalized("warn_punish_set",
                    Format.Bold(punish.ToString()),
                    Format.Bold(number.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task WarnPunish(int number)
            {
                if (number <= 0)
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    var ps = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
                    var p = ps.FirstOrDefault(x => x.Count == number);

                    if (p != null)
                    {
                        uow._context.Remove(p);
                        uow.Complete();
                    }
                }

                await ReplyConfirmLocalized("warn_punish_rem",
                    Format.Bold(number.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WarnPunishList()
            {
                WarningPunishment[] ps;
                using (var uow = _db.UnitOfWork)
                {
                    ps = uow.GuildConfigs.For(Context.Guild.Id, gc => gc.Include(x => x.WarnPunishments))
                        .WarnPunishments
                        .OrderBy(x => x.Count)
                        .ToArray();
                }

                string list;
                if (ps.Any())
                {
                    list = string.Join("\n", ps.Select(x => $"{x.Count} -> {x.Punishment}"));
                }
                else
                {
                    list = GetText("warnpl_none");
                }
                await Context.Channel.SendConfirmAsync(
                    GetText("warn_punish_list"),
                    list).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.BanMembers)]
            public async Task Ban(IGuildUser user, [Remainder] string msg = null)
            {
                if (Context.User.Id != user.Guild.OwnerId && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max()))
                {
                    await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("bandm", Format.Bold(Context.Guild.Name), msg));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                await Context.Guild.AddBanAsync(user, 7, Context.User.ToString() + " | " + msg).ConfigureAwait(false);
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle("⛔️ " + GetText("banned_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.BanMembers)]
            public async Task Unban([Remainder]string user)
            {
                var bans = await Context.Guild.GetBansAsync();

                var bun = bans.FirstOrDefault(x => x.User.ToString().ToLowerInvariant() == user.ToLowerInvariant());

                if (bun == null)
                {
                    await ReplyErrorLocalized("user_not_found").ConfigureAwait(false);
                    return;
                }

                await UnbanInternal(bun.User).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.BanMembers)]
            public async Task Unban(ulong userId)
            {
                var bans = await Context.Guild.GetBansAsync();

                var bun = bans.FirstOrDefault(x => x.User.Id == userId);

                if (bun == null)
                {
                    await ReplyErrorLocalized("user_not_found").ConfigureAwait(false);
                    return;
                }

                await UnbanInternal(bun.User).ConfigureAwait(false);
            }

            private async Task UnbanInternal(IUser user)
            {
                await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);

                await ReplyConfirmLocalized("unbanned_user", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireBotPermission(GuildPermission.BanMembers)]
            public async Task Softban(IGuildUser user, [Remainder] string msg = null)
            {
                if (Context.User.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max())
                {
                    await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("sbdm", Format.Bold(Context.Guild.Name), msg));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                await Context.Guild.AddBanAsync(user, 7, Context.User.ToString() + " | " + msg).ConfigureAwait(false);
                try { await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false); }
                catch { await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false); }

                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle("☣ " + GetText("sb_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireBotPermission(GuildPermission.KickMembers)]
            public async Task Kick(IGuildUser user, [Remainder] string msg = null)
            {
                if (Context.Message.Author.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max())
                {
                    await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        await user.SendErrorAsync(GetText("kickdm", Format.Bold(Context.Guild.Name), msg));
                    }
                    catch { }
                }

                await user.KickAsync(Context.User.ToString() + " | " + msg).ConfigureAwait(false);
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("kicked_user"))
                        .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.BanMembers)]
            [OwnerOnly]
            public async Task MassKill([Remainder] string people)
            {
                if (string.IsNullOrWhiteSpace(people))
                    return;

                var gusers = ((SocketGuild)Context.Guild).Users;
                //get user objects and reasons
                var bans = people.Split("\n")
                    .Select(x =>
                    {
                        var split = x.Trim().Split(" ");

                        var reason = string.Join(" ", split.Skip(1));

                        if (ulong.TryParse(split[0], out var id))
                            return (Original: split[0], Id: id, Reason: reason);

                        return (Original: split[0],
                            Id: gusers
                                .FirstOrDefault(u => u.ToString().ToLowerInvariant() == x)
                                ?.Id,
                            Reason: reason);
                    })
                    .ToArray();

                //if user is null, means that person couldn't be found
                var missing = bans
                    .Where(x => !x.Id.HasValue)
                    .ToArray();

                //get only data for found users
                var found = bans
                    .Where(x => x.Id.HasValue)
                    .Select(x => x.Id.Value)
                    .ToArray();

                var missStr = string.Join("\n", missing);
                if (string.IsNullOrWhiteSpace(missStr))
                    missStr = "-";

                //send a message but don't wait for it
                var banningMessageTask = Context.Channel.EmbedAsync(new EmbedBuilder()
                    .WithDescription(GetText("mass_kill_in_progress", bans.Length))
                    .AddField(GetText("invalid", missing.Length), missStr)
                    .WithOkColor());

                using (var uow = _db.UnitOfWork)
                {
                    var bc = uow.BotConfig.GetOrCreate(set => set.Include(x => x.Blacklist));
                    //blacklist the users
                    bc.Blacklist.AddRange(found.Select(x =>
                        new BlacklistItem
                        {
                            ItemId = x,
                            Type = BlacklistType.User,
                        }));
                    //clear their currencies
                    uow.DiscordUsers.RemoveFromMany(found.Select(x => (long)x).ToList());
                    uow.Complete();
                }

                _bc.Reload();

                //do the banning
                await Task.WhenAll(bans
                    .Where(x => x.Id.HasValue)
                    .Select(x => Context.Guild.AddBanAsync(x.Id.Value, 7, x.Reason, new RequestOptions()
                    {
                        RetryMode = RetryMode.AlwaysRetry,
                    })))
                    .ConfigureAwait(false);

                //wait for the message and edit it
                var banningMessage = await banningMessageTask.ConfigureAwait(false);

                await banningMessage.ModifyAsync(x => x.Embed = new EmbedBuilder()
                    .WithDescription(GetText("mass_kill_completed", bans.Length))
                    .AddField(GetText("invalid", missing.Length), missStr)
                    .WithOkColor()
                    .Build()).ConfigureAwait(false);
            }
        }
    }
}
