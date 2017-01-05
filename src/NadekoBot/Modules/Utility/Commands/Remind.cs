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
        public class RemindCommands : ModuleBase
        {

            Regex regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                    RegexOptions.Compiled | RegexOptions.Multiline);

            private static string RemindMessageFormat { get; }

            private static IDictionary<string, Func<Reminder, string>> replacements = new Dictionary<string, Func<Reminder, string>>
            {
                { "%message%" , (r) => r.Message },
                { "%user%", (r) => $"<@!{r.UserId}>" },
                { "%target%", (r) =>  r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>"}
            };
            private  static Logger _log { get; }

            static RemindCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                List<Reminder> reminders;
                using (var uow = DbHandler.UnitOfWork())
                {
                    reminders = uow.Reminders.GetAll().ToList();

                    RemindMessageFormat = uow.BotConfig.GetOrCreate().RemindMessageFormat;
                }

                foreach (var r in reminders)
                {
                    try { var t = StartReminder(r); } catch (Exception ex) { _log.Warn(ex); }
                }
            }

            private static async Task StartReminder(Reminder r)
            {
                var now = DateTime.Now;
                var twoMins = new TimeSpan(0, 2, 0);
                TimeSpan time = r.When - now;

                if (time.TotalMilliseconds > int.MaxValue)
                    return;

                await Task.Delay(time);
                try
                {
                    IMessageChannel ch = null;
                    if (r.IsPrivate)
                    {
                        ch = await NadekoBot.Client.GetDMChannelAsync(r.ChannelId).ConfigureAwait(false);
                    }
                    else
                    {
                        var t = NadekoBot.Client.GetGuild(r.ServerId)?.GetTextChannelAsync(r.ChannelId).ConfigureAwait(false);
                        if (t != null)
                            ch = await t.Value;
                    }
                    if (ch == null)
                        return;

                    await ch.SendMessageAsync(
                        replacements.Aggregate(RemindMessageFormat,
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
                IMessageChannel target;
                if (meorhere == MeOrHere.Me)
                {
                    target = await ((IGuildUser)Context.User).CreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    target = Context.Channel;
                }
                await Remind(target, timeStr, message).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task Remind(IMessageChannel ch, string timeStr, [Remainder] string message)
            {
                var channel = (ITextChannel)Context.Channel;

                if (ch == null)
                {
                    await channel.SendErrorAsync($"{Context.User.Mention} Something went wrong (channel cannot be found) ;(").ConfigureAwait(false);
                    return;
                }

                var m = regex.Match(timeStr);

                if (m.Length == 0)
                {
                    await channel.SendErrorAsync("Not a valid time format. Type `-h .remind`").ConfigureAwait(false);
                    return;
                }

                string output = "";
                var namesAndValues = new Dictionary<string, int>();

                foreach (var groupName in regex.GetGroupNames())
                {
                    if (groupName == "0") continue;
                    int value = 0;
                    int.TryParse(m.Groups[groupName].Value, out value);

                    if (string.IsNullOrEmpty(m.Groups[groupName].Value))
                    {
                        namesAndValues[groupName] = 0;
                        continue;
                    }
                    else if (value < 1 ||
                        (groupName == "months" && value > 1) ||
                        (groupName == "weeks" && value > 4) ||
                        (groupName == "days" && value >= 7) ||
                        (groupName == "hours" && value > 23) ||
                        (groupName == "minutes" && value > 59))
                    {
                        await channel.SendErrorAsync($"Invalid {groupName} value.").ConfigureAwait(false);
                        return;
                    }
                    else
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
                    ChannelId = ch.Id,
                    IsPrivate = ch is IDMChannel,
                    When = time,
                    Message = message,
                    UserId = Context.User.Id,
                    ServerId = channel.Guild.Id
                };

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.Reminders.Add(rem);
                    await uow.CompleteAsync();
                }

                try { await channel.SendConfirmAsync($"⏰ I will remind **\"{(ch is ITextChannel ? ((ITextChannel)ch).Name : Context.User.Username)}\"** to **\"{message.SanitizeMentions()}\"** in **{output}** `({time:d.M.yyyy.} at {time:HH:mm})`").ConfigureAwait(false); } catch { }
                await StartReminder(rem);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RemindTemplate([Remainder] string arg)
            {
                var channel = (ITextChannel)Context.Channel;

                if (string.IsNullOrWhiteSpace(arg))
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.BotConfig.GetOrCreate().RemindMessageFormat = arg.Trim();
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await channel.SendConfirmAsync("🆗 New remind template set.");
            }
        }
    }
}
