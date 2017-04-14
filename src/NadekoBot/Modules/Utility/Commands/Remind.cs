using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RemindCommands : NadekoSubmodule
        {
            readonly Regex _regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                    RegexOptions.Compiled | RegexOptions.Multiline);

            private static string remindMessageFormat { get; }

            private static readonly IDictionary<string, Func<Reminder, string>> _replacements = new Dictionary<string, Func<Reminder, string>>
            {
                { "%message%" , (r) => r.Message },
                { "%user%", (r) => $"<@!{r.UserId}>" },
                { "%target%", (r) =>  r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>"}
            };

            private new static readonly Logger _log;
            private static readonly CancellationTokenSource cancelSource;
            private static readonly CancellationToken cancelAllToken;

            static RemindCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                cancelSource = new CancellationTokenSource();
                cancelAllToken = cancelSource.Token;
                List<Reminder> reminders;
                using (var uow = DbHandler.UnitOfWork())
                {
                    reminders = uow.Reminders.GetAll().ToList();
                }
                remindMessageFormat = NadekoBot.BotConfig.RemindMessageFormat;

                foreach (var r in reminders)
                {
                    Task.Run(() => StartReminder(r, cancelAllToken));
                }
            }

            public static void Unload()
            {
                if (!cancelSource.IsCancellationRequested)
                    cancelSource.Cancel();
            }

            private static async Task StartReminder(Reminder r, CancellationToken t)
            {
                var now = DateTime.Now;

                var time = r.When - now;

                if (time.TotalMilliseconds > int.MaxValue)
                    return;

                await Task.Delay(time, t).ConfigureAwait(false);
                try
                {
                    IMessageChannel ch;
                    if (r.IsPrivate)
                    {
                        var user = NadekoBot.Client.GetGuild(r.ServerId).GetUser(r.ChannelId);
                        if(user == null)
                            return;
                        ch = await user.CreateDMChannelAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        ch = NadekoBot.Client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                    }
                    if (ch == null)
                        return;

                    await ch.SendMessageAsync(
                        _replacements.Aggregate(remindMessageFormat,
                            (cur, replace) => cur.Replace(replace.Key, replace.Value(r)))
                            .SanitizeMentions()
                            ).ConfigureAwait(false); //it works trust me
                }
                catch (Exception ex) { _log.Warn(ex); }
                finally
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.Reminders.Remove(r);
                        await uow.CompleteAsync();
                    }
                }
            }

            public enum MeOrHere
            {
                Me,Here
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
            public Task Remind(ITextChannel channel, string timeStr, [Remainder] string message) =>
                RemindInternal(channel.Id, false, timeStr, message);

            public async Task RemindInternal(ulong targetId, bool isPrivate, string timeStr, [Remainder] string message)
            {
                var m = _regex.Match(timeStr);

                if (m.Length == 0)
                {
                    await ReplyErrorLocalized("remind_invalid_format").ConfigureAwait(false);
                    return;
                }

                string output = "";
                var namesAndValues = new Dictionary<string, int>();

                foreach (var groupName in _regex.GetGroupNames())
                {
                    if (groupName == "0") continue;
                    int value;
                    int.TryParse(m.Groups[groupName].Value, out value);

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
                var time = DateTime.Now + new TimeSpan(30 * namesAndValues["months"] +
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

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.Reminders.Add(rem);
                    await uow.CompleteAsync();
                }

                try
                {
                    await Context.Channel.SendConfirmAsync(
                        "⏰ " + GetText("remind",
                            Format.Bold(!isPrivate ? $"<#{targetId}>" : Context.User.Username),
                            Format.Bold(message.SanitizeMentions()),
                            Format.Bold(output),
                            time, time)).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                await StartReminder(rem, cancelAllToken);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemindTemplate([Remainder] string arg)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.BotConfig.GetOrCreate().RemindMessageFormat = arg.Trim();
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("remind_template").ConfigureAwait(false);
            }
        }
    }
}
