using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Drawing.Imaging;
using NadekoBot.Extensions;
using NadekoBot.Properties;

namespace NadekoBot.Modules
{
    class Conversations : DiscordModule
    {
        private string firestr = "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥";
        public Conversations() : base()
        {
            commands.Add(new CopyCommand());
        }

        public override void Install(ModuleManager manager)
        {
            Random rng = new Random();

            manager.CreateCommands("", cgb =>
            {
                var client = manager.Client;

                cgb.CreateCommand("\\o\\")
                    .Description("Nadeko replies with /o/")
                    .Do(async e =>
                    {
                        await e.Send(e.User.Mention + "/o/");
                    });

                cgb.CreateCommand("/o/")
                    .Description("Nadeko replies with \\o\\")
                    .Do(async e =>
                    {
                        await e.Send( e.User.Mention + "\\o\\");
                    });
            });

            manager.CreateCommands(NadekoBot.botMention, cgb =>
            {
                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("uptime")
                    .Description("Shows how long is Nadeko running for.")
                    .Do(async e => {
                        var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
                        string str = "I am online for " + time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
                        await e.Send(str);
                    });

                cgb.CreateCommand("die")
                    .Description("Works only for the owner. Shuts the bot down.")
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            Timer t = new Timer();
                            t.Interval = 2000;
                            t.Elapsed += (s, ev) => { Environment.Exit(0); };
                            t.Start();
                            await e.Send(e.User.Mention + ", Yes, my love.");
                        }
                        else
                            await e.Send(e.User.Mention + ", No.");
                    });

                Stopwatch randServerSW = new Stopwatch();
                randServerSW.Start();

                cgb.CreateCommand("randserver")
                    .Description("Generates an invite to a random server and prints some stats.")
                    .Do(async e => {
                        if (client.Servers.Count() < 10) {
                            await e.Send("I need to be connected to at least 10 servers for this command to work.");
                            return;
                        }

                        if (randServerSW.ElapsedMilliseconds / 1000 < 1800) {
                            await e.Send("You have to wait " + (1800 - randServerSW.ElapsedMilliseconds / 1000) + " more seconds to use this function.");
                            return;
                        }
                        randServerSW.Reset();
                        while (true) {
                            var server = client.Servers.OrderBy(x => rng.Next()).FirstOrDefault();
                            if (server == null)
                                continue;
                            try {
                                var inv = await server.CreateInvite(100, 5);
                                await e.Send("**Server:** " + server.Name +
                                            "\n**Owner:** " + server.Owner.Name +
                                            "\n**Channels:** " + server.AllChannels.Count() +
                                            "\n**Total Members:** " + server.Users.Count() +
                                            "\n**Online Members:** " + server.Users.Where(u => u.Status == UserStatus.Online).Count() +
                                            "\n**Invite:** " + inv.Url);
                                break;
                            } catch (Exception) { continue; }
                        }
                    });
                /*
                cgb.CreateCommand("avalanche!")
                    .Description("Mentions a person in every channel of the server, then deletes it")
                    .Parameter("name", ParameterType.Required)
                    .Do(e => {
                        var usr = e.Server.FindUsers(e.GetArg("name")).FirstOrDefault();
                        if (usr == null) return;
                        e.Server.AllChannels.ForEach(async c => {
                            try {
                                var m = await c.SendMessage(usr.Mention);
                                await m.Delete();
                            } catch (Exception ex) {
                                Console.WriteLine(ex);
                            }
                        });
                    });
                    */
                cgb.CreateCommand("do you love me")
                    .Description("Replies with positive answer only to the bot owner.")
                    .Do(async e => {
                        if (e.User.Id == NadekoBot.OwnerID)
                            await e.Send(e.User.Mention + ", Of course I do, my Master.");
                        else
                            await e.Send(e.User.Mention + ", Don't be silly.");
                    });

                cgb.CreateCommand("how are you")
                    .Description("Replies positive only if bot owner is online.")
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            await e.Send(e.User.Mention + " I am great as long as you are here.");
                        }
                        else
                        {
                            var kw = e.Server.GetUser(NadekoBot.OwnerID);
                            if (kw != null && kw.Status == UserStatus.Online)
                            {
                                await e.Send(e.User.Mention + " I am great as long as " + kw.Mention + " is with me.");
                            }
                            else
                            {
                                await e.Send(e.User.Mention + " I am sad. My Master is not with me.");
                            }
                        }
                    });

                cgb.CreateCommand("insult")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Only works for owner. Insults @X person.\n**Usage**: @NadekoBot insult @X.")
                    .Do(async e =>
                    {
                        List<string> insults = new List<string> { " you are a poop.", " you jerk.", " i will eat you when i get my powers back." };
                        Random r = new Random();
                        var u = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (u == null) {
                            await e.Send("Invalid user specified.");
                            return;
                        }

                        if (u.Id == NadekoBot.OwnerID) {
                            await e.Send("I would never insult my master <3");
                            return;
                        }
                        await e.Send(u.Mention + insults[r.Next(0, insults.Count)]);
                    });

                cgb.CreateCommand("praise")
                    .Description("Only works for owner. Praises @X person.\n**Usage**: @NadekoBot praise @X.")
                    .Parameter("mention", ParameterType.Required)
                    .Do(async e =>
                    {
                        List<string> praises = new List<string> { " You are cool.", " You are nice!", " You did a good job.", " You did something nice.", " is awesome!" };
                        Random r = new Random();
                        var u = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();

                        if (u == null)
                        {
                            await e.Send("Invalid user specified.");
                            return;
                        }

                        if (u.Id == NadekoBot.OwnerID) {
                            await e.Send(e.User.Mention + " I don't need your permission to praise my beloved Master <3");
                            return;
                        }
                        await e.Send(u.Mention + praises[r.Next(0, praises.Count)]);
                    });

                cgb.CreateCommand("are you real")
                    .Description("Useless.")
                    .Do(async e =>
                    {
                        await e.Send(e.User.Mention + " I will be soon.");
                    });

                cgb.CreateCommand("are you there")
                    .Description("Checks if nadeko is operational.")
                    .Alias(new string[] { "!", "?" })
                    .Do(SayYes());

                cgb.CreateCommand("draw")
                    .Description("Nadeko instructs you to type $draw. Gambling functions start with $")
                    .Do(async e =>
                    {
                        await e.Send("Sorry, I don't gamble, type $draw for that function.");
                    });
                cgb.CreateCommand("fire")
                    .Description("Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire.\n**Usage**: @NadekoBot fire [x]")
                    .Parameter("times", ParameterType.Optional)
                    .Do(async e =>
                    {
                        int count = 0;
                        if (e.Args?.Length > 0)
                            int.TryParse(e.Args[0], out count);

                        if (count < 1)
                            count = 1;
                        else if (count > 12)
                            count = 12;
                        string str = "";
                        for (int i = 0; i < count; i++)
                        {
                            str += firestr;
                        }
                        await e.Send(str);
                    });

                cgb.CreateCommand("rip")
                    .Description("Shows a grave image.Optional parameter [@X] instructs her to put X's name on the grave.\n**Usage**: @NadekoBot rip [@X]")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        string text = "";
                        if (usr == null)
                        {
                            text = e.GetArg("user");
                        }
                        else {
                            text = usr.Name;
                        }
                        await e.Channel.SendFile("ripzor_m8.png", RipName(text));
                    });

                cgb.CreateCommand("j")
                    .Description("Joins a server using a code.")
                    .Parameter("id", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            await (await client.GetInvite(e.Args[0])).Accept();
                            await e.Send("I got in!");
                        }
                        catch (Exception)
                        {
                            await e.Send("Invalid code.");
                        }
                    });

                cgb.CreateCommand("save")
                    .Description("Saves something for the owner in a file.")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            string m = "";
                            try
                            {
                                FileStream f = File.OpenWrite("saves.txt");
                                m = e.Args[0];
                                byte[] b = Encoding.ASCII.GetBytes(m + "\n");
                                f.Seek(f.Length, SeekOrigin.Begin);
                                f.Write(b, 0, b.Length);
                                f.Close();
                            }
                            catch (Exception)
                            {
                                await e.Send("Error saving. Sorry :(");
                            }
                            if (m.Length > 0)
                                await e.Send("I saved this for you: " + Environment.NewLine + "```" + m + "```");
                            else
                                await e.Send("No point in saving empty message...");
                        }
                        else await e.Send("Not for you, only my Master <3");
                    });

                cgb.CreateCommand("ls")
                    .Description("Shows all saved items.")
                    .Do(async e =>
                    {
                        FileStream f = File.OpenRead("saves.txt");
                        if (f.Length == 0)
                        {
                            await e.Send("Saves are empty.");
                            return;
                        }
                        byte[] b = new byte[f.Length / sizeof(byte)];
                        f.Read(b, 0, b.Length);
                        f.Close();
                        string str = Encoding.ASCII.GetString(b);
                        await e.User.Send("```" + (str.Length < 1950 ? str : str.Substring(0, 1950)) + "```");
                    });
                cgb.CreateCommand("slm")
                    .Description("Shows the message where you were last mentioned in this channel (checks last 10k messages)")
                    .Do(async e => {

                        Message msg;
                        var msgs = e.Channel.Messages
                                    .Where(m => m.MentionedUsers.Contains(e.User))
                                    .OrderBy(m => m.Timestamp);
                        if (msgs.Count() > 0)
                            msg = msgs.FirstOrDefault();
                        else {
                            var msgsarr = await e.Channel.DownloadMessages(10000);
                            msg = msgsarr
                                    .Where(m => m.MentionedUsers.Contains(e.User))
                                    .OrderBy(m => m.Timestamp)
                                    .FirstOrDefault();
                        }
                        if (msg != null)
                            await e.Send("Last message mentioning you was at " + msg.Timestamp + "\n**Message:** " + msg.RawText);
                        else
                            await e.Send("I can't find a message mentioning you.");
                    });
                cgb.CreateCommand("cs")
                    .Description("Deletes all saves")
                    .Do(async e =>
                    {
                        File.Delete("saves.txt");
                        await e.Send("Cleared all saves.");
                    });

                cgb.CreateCommand("bb")
                    .Description("Says bye to someone. **Usage**: @NadekoBot bb @X")
                    .Parameter("ppl", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string str = "Bye";
                        foreach (var u in e.Message.MentionedUsers)
                        {
                            str += " " + u.Mention;
                        }
                        await e.Send(str);
                    });

                cgb.CreateCommand("req")
                    .Alias("request")
                    .Description("Requests a feature for nadeko.\n**Usage**: @NadekoBot req new_feature")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string str = e.Args[0];

                        try
                        {
                            StatsCollector.SaveRequest(e, str);
                        }
                        catch (Exception)
                        {
                            await e.Send("Something went wrong.");
                            return;
                        }
                        await e.Send("Thank you for your request.");
                    });

                cgb.CreateCommand("lr")
                    .Description("PMs the user all current nadeko requests.")
                    .Do(async e =>
                    {
                        string str = StatsCollector.GetRequests();
                        if (str.Trim().Length > 110)
                            await e.User.Send(str);
                        else
                            await e.User.Send("No requests atm.");
                    });

                cgb.CreateCommand("dr")
                    .Description("Deletes a request. Only owner is able to do this.")
                    .Parameter("reqNumber", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            try
                            {
                                if (StatsCollector.DeleteRequest(int.Parse(e.Args[0])))
                                {
                                    await e.Send(e.User.Mention + " Request deleted.");
                                }
                                else
                                {
                                    await e.Send("No request on that number.");
                                }
                            }
                            catch
                            {
                                await e.Send("Error deleting request, probably NaN error.");
                            }
                        }
                        else await e.Send("You don't have permission to do that.");
                    });

                cgb.CreateCommand("rr")
                    .Description("Resolves a request. Only owner is able to do this.")
                    .Parameter("reqNumber", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            try
                            {
                                var sc = StatsCollector.ResolveRequest(int.Parse(e.Args[0]));
                                if (sc != null)
                                {
                                    await e.Send(e.User.Mention + " Request resolved, notice sent.");
                                    await client.GetServer(sc.ServerId).GetUser(sc.Id).Send("**This request of yours has been resolved:**\n" + sc.Text);
                                }
                                else
                                {
                                    await e.Send("No request on that number.");
                                }
                            }
                            catch
                            {
                                await e.Send("Error resolving request, probably NaN error.");
                            }
                        }
                        else await e.Send("You don't have permission to do that.");
                    });

                cgb.CreateCommand("call")
                    .Description("Useless. Writes calling @X to chat.\n**Usage**: @NadekoBot call @X ")
                    .Parameter("who", ParameterType.Required)
                    .Do(async e =>
                    {
                        await e.Send("Calling " + e.Args[0] + "...");
                    });
                cgb.CreateCommand("hide")
                    .Description("Hides nadeko in plain sight!11!!")
                    .Do(async e =>
                    {
                        try
                        {
                            using (Stream ms = Resources.hidden.ToStream(ImageFormat.Png))
                            {
                                await client.CurrentUser.Edit(NadekoBot.password, avatar: ms);
                            }
                            await e.Send("*hides*");
                        }
                        catch (Exception ex)
                        {
                            StatsCollector.DEBUG_LOG(ex.ToString());
                        }
                    });

                cgb.CreateCommand("unhide")
                    .Description("Unhides nadeko in plain sight!1!!1")
                    .Do(async e =>
                    {
                        try
                        {
                            using (Stream ms = Resources.nadeko.ToStream()) {
                                await client.CurrentUser.Edit(NadekoBot.password, avatar: ms,avatarType:ImageType.Jpeg);
                            }
                            await e.Send("*unhides*");
                        }
                        catch (Exception ex)
                        {
                            StatsCollector.DEBUG_LOG(ex.ToString());
                        }
                    });

                cgb.CreateCommand("dump")
                    .Description("Dumps all of the invites it can to dump.txt.** Owner Only.**")
                    .Do(async e =>
                    {
                        if (NadekoBot.OwnerID != e.User.Id) return;
                        int i = 0;
                        int j = 0;
                        string invites = "";
                        foreach (var s in client.Servers) {
                            try
                            {
                                var invite = await s.CreateInvite(0);
                                invites+=invite.Url+"\n";
                                i++;
                            }
                            catch (Exception) {
                                j++;
                                continue;
                            }
                        }
                        File.WriteAllText("dump.txt", invites);
                        await e.Send($"Got invites for {i} servers and failed to get invites for {j} servers");
                    });

                

                cgb.CreateCommand("av").Alias("avatar")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Shows a mentioned person's avatar. **Usage**: ~av @X")
                    .Do(async e =>
                    {
                        var usr = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send("Invalid user specified.");
                            return;
                        }
                        string av = usr.AvatarUrl;
                        await e.Send(Searches.ShortenUrl(av));
                    });

                //TODO add eval
                /*
                cgb.CreateCommand(">")
                    .Parameter("code", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (e.Message.User.Id == NadekoBot.OwnerId)
                        {
                            var result = await CSharpScript.EvaluateAsync(e.Args[0]);
                            await e.Send( result?.ToString() ?? "null");
                            return;
                        }
                    });*/
            });
        }

        public Stream RipName(string name)
        {
            Bitmap bm = Resources.rip;

            int offset = name.Length * 5;

            int fontSize = 20;

            if (name.Length > 10)
            {
                fontSize -= (name.Length - 10) / 2;
            }

            //TODO use measure string
            Graphics g = Graphics.FromImage(bm);
            g.DrawString(name, new Font("Comic Sans MS", fontSize, FontStyle.Bold), Brushes.Black, 100 - offset, 200);
            g.DrawString("? - " + DateTime.Now.Year, new Font("Consolas", 12, FontStyle.Bold), Brushes.Black, 80, 235);
            g.Flush();
            g.Dispose();

            return bm.ToStream(ImageFormat.Png);
        }

        private Func<CommandEventArgs, Task> SayYes() 
            => async e => await e.Send("Yes. :)");
    }
}
