using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common.TypeReaders.Models;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RemindCommands : NadekoSubmodule<RemindService>
        {
            private readonly DbService _db;
            private readonly GuildTimezoneService _tz;

            public RemindCommands(DbService db, GuildTimezoneService tz)
            {
                _db = db;
                _tz = tz;
            }

            public enum MeOrHere
            {
                Me, Here
            }

            [NadekoCommand, Usage, Description, Aliases]
            [Priority(1)]
            public async Task Remind(MeOrHere meorhere, StoopidTime time, [Leftover] string message)
            {
                ulong target;
                target = meorhere == MeOrHere.Me ? ctx.User.Id : ctx.Channel.Id;
                if (!await RemindInternal(target, meorhere == MeOrHere.Me || ctx.Guild == null, time.Time, message).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync("remind_too_long").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(0)]
            public async Task Remind(ITextChannel channel, StoopidTime time, [Leftover] string message)
            {
                var perms = ((IGuildUser)ctx.User).GetPermissions((ITextChannel)channel);
                if (!perms.SendMessages || !perms.ViewChannel)
                {
                    await ReplyErrorLocalizedAsync("cant_read_or_send").ConfigureAwait(false);
                    return;
                }
                else
                {
                    if (!await RemindInternal(channel.Id, false, time.Time, message).ConfigureAwait(false))
                    {
                        await ReplyErrorLocalizedAsync("remind_too_long").ConfigureAwait(false);
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task RemindList(int page = 1)
            {
                if (--page < 0)
                    return;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("reminder_list"));

                List<Reminder> rems;
                using (var uow = _db.GetDbContext())
                {
                    rems = uow.Reminders.RemindersFor(ctx.User.Id, page)
                        .ToList();
                }

                if (rems.Any())
                {
                    var i = 0;
                    foreach (var rem in rems)
                    {
                        var when = rem.When;
                        var diff = when - DateTime.UtcNow;
                        embed.AddField($"#{++i} {rem.When:HH:mm yyyy-MM-dd} UTC (in {(int)diff.TotalHours}h {(int)diff.Minutes}m)", $@"`Target:` {(rem.IsPrivate ? "DM" : "Channel")}
`TargetId:` {rem.ChannelId}
`Message:` {rem.Message}", false);
                    }
                }
                else
                {
                    embed.WithDescription(GetText("reminders_none"));
                }

                embed.AddPaginatedFooter(page + 1, null);
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task RemindDelete(int index)
            {
                if (--index < 0)
                    return;

                var embed = new EmbedBuilder();

                Reminder rem = null;
                using (var uow = _db.GetDbContext())
                {
                    var rems = uow.Reminders.RemindersFor(ctx.User.Id, index / 10)
                        .ToList();

                    if (rems.Count > index)
                    {
                        rem = rems[index];
                        uow.Reminders.Remove(rem);
                        uow.SaveChanges();
                    }
                }

                if (rem == null)
                {
                    await ReplyErrorLocalizedAsync("reminder_not_exist").ConfigureAwait(false);
                }
                else
                {
                    _service.RemoveReminder(rem.Id);
                    await ReplyErrorLocalizedAsync("reminder_deleted", index + 1).ConfigureAwait(false);
                }
            }

            public async Task<bool> RemindInternal(ulong targetId, bool isPrivate, TimeSpan ts, [Leftover] string message)
            {
                var time = DateTime.UtcNow + ts;

                if (ts > TimeSpan.FromDays(60))
                    return false;

                var rem = new Reminder
                {
                    ChannelId = targetId,
                    IsPrivate = isPrivate,
                    When = time,
                    Message = message,
                    UserId = ctx.User.Id,
                    ServerId = ctx.Guild?.Id ?? 0
                };

                using (var uow = _db.GetDbContext())
                {
                    uow.Reminders.Add(rem);
                    await uow.SaveChangesAsync();
                }

                var gTime = ctx.Guild == null ?
                    time :
                    TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(ctx.Guild.Id));
                try
                {
                    await ctx.Channel.SendConfirmAsync(
                        "⏰ " + GetText("remind",
                            Format.Bold(!isPrivate ? $"<#{targetId}>" : ctx.User.Username),
                            Format.Bold(message.SanitizeMentions()),
                            $"{ts.Days}d {ts.Hours}h {ts.Minutes}min",
                            gTime, gTime)).ConfigureAwait(false);
                }
                catch
                {

                }
                _service.StartReminder(rem);
                return true;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemindTemplate([Leftover] string arg)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    return;

                using (var uow = _db.GetDbContext())
                {
                    uow.BotConfig.GetOrCreate(set => set).RemindMessageFormat = arg.Trim();
                    await uow.SaveChangesAsync();
                }

                await ReplyConfirmLocalizedAsync("remind_template").ConfigureAwait(false);
            }
        }
    }
}
