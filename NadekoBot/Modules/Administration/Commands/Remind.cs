using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using NadekoBot.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;

namespace NadekoBot.Modules.Administration.Commands
{
    class Remind : DiscordCommand
    {

        Regex regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        List<Timer> reminders = new List<Timer>();

        public Remind(DiscordModule module) : base(module)
        {
            var remList = DbHandler.Instance.GetAllRows<Reminder>();

            Console.WriteLine(string.Join("\n-", remList.Select(r => r.When.ToString())));
            reminders = remList.Select(StartNewReminder).ToList();
        }

        private Timer StartNewReminder(Reminder r)
        {
            var now = DateTime.Now;
            var twoMins = new TimeSpan(0, 2, 0);
            TimeSpan time = (r.When - now) < twoMins
                            ? twoMins       //if the time is less than 2 minutes,
                            : r.When - now; //it will send the message 2 minutes after start
                                            //To account for high bot startup times
            if (time.TotalMilliseconds > int.MaxValue)
                return null;
            var t = new Timer(time.TotalMilliseconds);
            t.Elapsed += async (s, e) =>
            {
                try
                {
                    Channel ch;
                    if (r.IsPrivate)
                    {
                        ch = NadekoBot.Client.PrivateChannels.FirstOrDefault(c => (long)c.Id == r.ChannelId);
                        if (ch == null)
                            ch = await NadekoBot.Client.CreatePrivateChannel((ulong)r.ChannelId);
                    }
                    else
                        ch = NadekoBot.Client.GetServer((ulong)r.ServerId)?.GetChannel((ulong)r.ChannelId);

                    if (ch == null)
                        return;

                    await ch.SendMessage($"❗⏰**I've been told to remind you to '{r.Message}' now by <@{r.UserId}>.**⏰❗");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Timer error! {ex}");
                }
                finally
                {
                    DbHandler.Instance.Delete<Reminder>(r.Id.Value);
                    t.Stop();
                    t.Dispose();
                }
            };
            t.Start();
            return t;
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "remind")
                .Description("Sends a message to you or a channel after certain amount of time. " +
                             "First argument is me/here/'channelname'. Second argument is time in a descending order (mo>w>d>h>m) example: 1w5d3h10m. " +
                             "Third argument is a (multiword)message. " +
                             "\n**Usage**: `.remind me 1d5h Do something` or `.remind #general Start now!`")
                .Parameter("meorchannel", ParameterType.Required)
                .Parameter("time", ParameterType.Required)
                .Parameter("message", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var meorchStr = e.GetArg("meorchannel").ToUpperInvariant();
                    Channel ch;
                    bool isPrivate = false;
                    if (meorchStr == "ME")
                    {
                        isPrivate = true;
                        ch = await e.User.CreatePMChannel();
                    }
                    else if (meorchStr == "HERE")
                    {
                        ch = e.Channel;
                    }
                    else
                    {
                        ch = e.Server.FindChannels(meorchStr).FirstOrDefault();
                    }

                    if (ch == null)
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} Something went wrong (channel cannot be found) ;(");
                        return;
                    }

                    var timeStr = e.GetArg("time");

                    var m = regex.Match(timeStr);

                    if (m.Length == 0)
                    {
                        await e.Channel.SendMessage("Not a valid time format blablabla");
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
                            await e.Channel.SendMessage($"Invalid {groupName} value.");
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
                        ChannelId = (long)ch.Id,
                        IsPrivate = isPrivate,
                        When = time,
                        Message = e.GetArg("message"),
                        UserId = (long)e.User.Id,
                        ServerId = (long)e.Server.Id
                    };
                    DbHandler.Instance.InsertData(rem);

                    reminders.Add(StartNewReminder(rem));

                    await e.Channel.SendMessage($"⏰ I will remind \"{ch.Name}\" to \"{e.GetArg("message").ToString()}\" in {output}. ({time:d.M.yyyy.} at {time:HH:m})");
                });
        }
    }
}
