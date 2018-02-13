using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
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
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Remind(MeOrHere meorhere, string timeStr, [Remainder] string message)
            {
                ulong target;
                target = meorhere == MeOrHere.Me ? Context.User.Id : Context.Channel.Id;
                await RemindInternal(target, meorhere == MeOrHere.Me, timeStr, message).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task Remind(ITextChannel channel, string timeStr, [Remainder] string message)
            {
                var perms = ((IGuildUser)Context.User).GetPermissions((ITextChannel)channel);
                if (!perms.SendMessages || !perms.ViewChannel)
                {
                    await ReplyErrorLocalized("cant_read_or_send").ConfigureAwait(false);
                    return;
                }
                else
                {
                    var _ = RemindInternal(channel.Id, false, timeStr, message).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task RemindList(int page = 1)
            {
                if (--page < 0)
                    return;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("reminder_list"));

                List<Reminder> rems;
                using (var uow = _db.UnitOfWork)
                {
                    rems = uow.Reminders.RemindersFor(Context.User.Id, page)
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
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task RemindDelete(int index)
            {
                if (--index < 0)
                    return;

                var embed = new EmbedBuilder();

                Reminder rem = null;
                using (var uow = _db.UnitOfWork)
                {
                    var rems = uow.Reminders.RemindersFor(Context.User.Id, index / 10)
                        .ToList();

                    if (rems.Count > index)
                    {
                        rem = rems[index];
                        uow.Reminders.Remove(rem);
                        uow.Complete();
                    }
                }

                if (rem == null)
                {
                    await ReplyErrorLocalized("reminder_not_exist").ConfigureAwait(false);
                }
                else
                {
                    _service.RemoveReminder(rem.Id);
                    await ReplyErrorLocalized("reminder_deleted", index + 1).ConfigureAwait(false);
                }
            }

            public async Task RemindInternal(ulong targetId, bool isPrivate, string timeStr, [Remainder] string message)
            {
                var m = _service.Regex.Match(timeStr);

                if (m.Length == 0)
                {
                    await ReplyErrorLocalized("remind_invalid_format").ConfigureAwait(false);
                    return;
                }

                string output = "";
                var namesAndValues = new Dictionary<string, int>();

                foreach (var groupName in _service.Regex.GetGroupNames())
                {
                    if (groupName == "0") continue;
                    int.TryParse(m.Groups[groupName].Value, out var value);

                    if (string.IsNullOrEmpty(m.Groups[groupName].Value))
                    {
                        namesAndValues[groupName] = 0;
                        continue;
                    }
                    if (value < 1 ||
                        (groupName == "months" && value > 1) ||
                        (groupName == "weeks" && value > 4) ||
                        (groupName == "days" && value >= 7) ||
                        (groupName == "hours" && value > 23) ||
                        (groupName == "minutes" && value > 59))
                    {
                        await Context.Channel.SendErrorAsync($"Invalid {groupName} value.").ConfigureAwait(false);
                        return;
                    }
                    namesAndValues[groupName] = value;
                    output += m.Groups[groupName].Value + " " + groupName + " ";
                }
                var time = DateTime.UtcNow + new TimeSpan(30 * namesAndValues["months"] +
                                                        7 * namesAndValues["weeks"] +
                                                        namesAndValues["days"],
                                                        namesAndValues["hours"],
                                                        namesAndValues["minutes"],
                                                        0);

                var rem = new Reminder
                {
                    ChannelId = targetId,
                    IsPrivate = isPrivate,
                    When = time,
                    Message = message,
                    UserId = Context.User.Id,
                    ServerId = Context.Guild.Id
                };

                using (var uow = _db.UnitOfWork)
                {
                    uow.Reminders.Add(rem);
                    await uow.CompleteAsync();
                }

                var gTime = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(Context.Guild.Id));
                try
                {
                    await Context.Channel.SendConfirmAsync(
                        "⏰ " + GetText("remind",
                            Format.Bold(!isPrivate ? $"<#{targetId}>" : Context.User.Username),
                            Format.Bold(message.SanitizeMentions()),
                            Format.Bold(output),
                            gTime, gTime)).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                _service.StartReminder(rem);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemindTemplate([Remainder] string arg)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    uow.BotConfig.GetOrCreate(set => set).RemindMessageFormat = arg.Trim();
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("remind_template").ConfigureAwait(false);
            }
        }
    }
}
