using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
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
        public class RemindCommands
        {

            Regex regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                    RegexOptions.Compiled | RegexOptions.Multiline);

            private string RemindMessageFormat { get; }

            IDictionary<string, Func<Reminder, string>> replacements = new Dictionary<string, Func<Reminder, string>>
            {
                { "%message%" , (r) => r.Message },
                { "%user%", (r) => $"<@!{r.UserId}>" },
                { "%target%", (r) =>  r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>"}
            };

            public RemindCommands()
            {
                List<Reminder> reminders;
                using (var uow = DbHandler.UnitOfWork())
                {
                    reminders = uow.Reminders.GetAll().ToList();

                    RemindMessageFormat = uow.BotConfig.GetOrCreate().RemindMessageFormat;
                }

                foreach (var r in reminders)
                {
                    var t = StartReminder(r);
                }
            }

            private async Task StartReminder(Reminder r)
            {
                var now = DateTime.Now;
                var twoMins = new TimeSpan(0, 2, 0);
                TimeSpan time = r.When - now; 

                if (time.TotalMilliseconds > int.MaxValue)
                    return;

                await Task.Delay(time);
                try
                {
                    IMessageChannel ch;
                    if (r.IsPrivate)
                    {
                        ch = await NadekoBot.Client.GetDMChannelAsync(r.ChannelId).ConfigureAwait(false);
                    }
                    else
                    {
                        ch = NadekoBot.Client.GetGuilds()
                                .Where(g => g.Id == r.ServerId)
                                .FirstOrDefault()
                                .GetTextChannels()
                                .Where(c => c.Id == r.ChannelId)
                                .FirstOrDefault();
                    }
                    if (ch == null)
                        return;

                    await ch.SendMessageAsync(
                        replacements.Aggregate(RemindMessageFormat,
                        (cur, replace) => cur.Replace(replace.Key, replace.Value(r)))
                            ).ConfigureAwait(false); //it works trust me

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Timer error! {ex}");
                }
                finally
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.Reminders.Remove(r);
                        await uow.CompleteAsync();
                    }
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Remind(IMessage imsg, string meorchannel, string timeStr, [Remainder] string message)
            {
                var channel = (ITextChannel)imsg.Channel;

                var meorchStr = meorchannel.ToUpperInvariant();
                IMessageChannel ch;
                bool isPrivate = false;
                if (meorchStr == "ME")
                {
                    isPrivate = true;
                    ch = await ((IGuildUser)imsg.Author).CreateDMChannelAsync().ConfigureAwait(false);
                }
                else if (meorchStr == "HERE")
                {
                    ch = channel;
                }
                else
                {
                    ch = channel.Guild.GetTextChannels().FirstOrDefault(c => c.Name == meorchStr || c.Id.ToString() == meorchStr);
                }

                if (ch == null)
                {
                    await channel.SendMessageAsync($"{imsg.Author.Mention} Something went wrong (channel cannot be found) ;(").ConfigureAwait(false);
                    return;
                }

                var m = regex.Match(timeStr);

                if (m.Length == 0)
                {
                    await channel.SendMessageAsync("Not a valid time format blablabla").ConfigureAwait(false);
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
                        await channel.SendMessageAsync($"Invalid {groupName} value.").ConfigureAwait(false);
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
                    IsPrivate = isPrivate,
                    When = time,
                    Message = message,
                    UserId = imsg.Author.Id,
                    ServerId = channel.Guild.Id
                };

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.Reminders.Add(rem);
                    await uow.CompleteAsync();
                }

                await channel.SendMessageAsync($"⏰ I will remind \"{(ch is ITextChannel ? ((ITextChannel)ch).Name : imsg.Author.Username)}\" to \"{message.ToString()}\" in {output}. ({time:d.M.yyyy.} at {time:HH:mm})").ConfigureAwait(false);
                await StartReminder(rem);
            }

            ////todo owner only
            //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
            //[RequireContext(ContextType.Guild)]
            //public async Task RemindTemplate(IMessage imsg, [Remainder] string arg)
            //{
            //    var channel = (ITextChannel)imsg.Channel;


            //    arg = arg?.Trim();
            //    if (string.IsNullOrWhiteSpace(arg))
            //        return;

            //    NadekoBot.Config.RemindMessageFormat = arg;
            //    await channel.SendMessageAsync("`New remind message set.`");
            //}
        }
    }
}