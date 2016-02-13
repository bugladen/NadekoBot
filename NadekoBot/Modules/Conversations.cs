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
using System.Drawing.Imaging;
using NadekoBot.Extensions;
using NadekoBot.Properties;
using NadekoBot.Commands;

namespace NadekoBot.Modules {
    class Conversations : DiscordModule {
        private string firestr = "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥";
        public Conversations() : base() {
            commands.Add(new CopyCommand());
            commands.Add(new RequestsCommand());
        }

        public override void Install(ModuleManager manager) {
            Random rng = new Random();

            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                var client = manager.Client;

                cgb.CreateCommand("\\o\\")
                    .Description("Nadeko replies with /o/")
                    .Do(async e => {
                        await e.Send(e.User.Mention + "/o/");
                    });

                cgb.CreateCommand("/o/")
                    .Description("Nadeko replies with \\o\\")
                    .Do(async e => {
                        await e.Send(e.User.Mention + "\\o\\");
                    });
            });

            manager.CreateCommands(NadekoBot.botMention, cgb => {
                var client = manager.Client;

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

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
                    .Do(async e => {
                        if (e.User.Id == NadekoBot.OwnerID) {
                            Timer t = new Timer();
                            t.Interval = 2000;
                            t.Elapsed += (s, ev) => { Environment.Exit(0); };
                            t.Start();
                            await e.Send(e.User.Mention + ", Yes, my love.");
                        } else
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

                        if (randServerSW.Elapsed.Seconds < 1800) {
                            await e.Send("You have to wait " + (1800 - randServerSW.Elapsed.Seconds) + " more seconds to use this function.");
                            return;
                        }
                        randServerSW.Restart();
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
                    .Do(async e => {
                        if (e.User.Id == NadekoBot.OwnerID) {
                            await e.Send(e.User.Mention + " I am great as long as you are here.");
                        } else {
                            var kw = e.Server.GetUser(NadekoBot.OwnerID);
                            if (kw != null && kw.Status == UserStatus.Online) {
                                await e.Send(e.User.Mention + " I am great as long as " + kw.Mention + " is with me.");
                            } else {
                                await e.Send(e.User.Mention + " I am sad. My Master is not with me.");
                            }
                        }
                    });

                cgb.CreateCommand("insult")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Only works for owner. Insults @X person.\n**Usage**: @NadekoBot insult @X.")
                    .Do(async e => {
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
                    .Do(async e => {
                        List<string> praises = new List<string> { " You are cool.",
                            " You are nice!",
                            " You did a good job.",
                            " You did something nice.",
                            " is awesome!",
                            " Wow."};

                        Random r = new Random();
                        var u = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();

                        if (u == null) {
                            await e.Send("Invalid user specified.");
                            return;
                        }

                        if (u.Id == NadekoBot.OwnerID) {
                            await e.Send(e.User.Mention + " I don't need your permission to praise my beloved Master <3");
                            return;
                        }
                        await e.Send(u.Mention + praises[r.Next(0, praises.Count)]);
                    });

                cgb.CreateCommand("pat")
                  .Description("Pat someone ^_^")
                  .Parameter("user", ParameterType.Unparsed)
                  .Do(async e => {
                      var user = e.GetArg("user");
                      if (user == null || e.Message.MentionedUsers.Count() == 0) return;
                      string[] pats = new string[] { "http://i.imgur.com/IiQwK12.gif",
                                                     "http://i.imgur.com/JCXj8yD.gif",
                                                     "http://i.imgur.com/qqBl2bm.gif",
                                                     "http://i.imgur.com/eOJlnwP.gif",
                                                     "https://45.media.tumblr.com/229ec0458891c4dcd847545c81e760a5/tumblr_mpfy232F4j1rxrpjzo1_r2_500.gif",
                                                     "https://media.giphy.com/media/KZQlfylo73AMU/giphy.gif",
                                                     "https://media.giphy.com/media/12hvLuZ7uzvCvK/giphy.gif",
                                                     "http://gallery1.anivide.com/_full/65030_1382582341.gif",
                                                     "https://49.media.tumblr.com/8e8a099c4eba22abd3ec0f70fd087cce/tumblr_nxovj9oY861ur1mffo1_500.gif ",
                      };
                      await e.Send($"{e.Message.MentionedUsers.First().Mention} {pats[new Random().Next(0, pats.Length)]}");
                  });

                cgb.CreateCommand("cry")
                  .Description("Tell Nadeko to cry. You are a heartless monster if you use this command.")
                  .Do(async e => {
                      string[] pats = new string[] { "http://i.imgur.com/Xg3i1Qy.gif",
                                                     "http://i.imgur.com/3K8DRrU.gif",
                                                     "http://i.imgur.com/k58BcAv.gif",
                                                     "http://i.imgur.com/I2fLXwo.gif" };
                      await e.Send($"(•̥́ _•ૅ｡)\n{pats[new Random().Next(0, pats.Length)]}");
                  });

                cgb.CreateCommand("are you real")
                    .Description("Useless.")
                    .Do(async e => {
                        await e.Send(e.User.Mention + " I will be soon.");
                    });

                cgb.CreateCommand("are you there")
                    .Description("Checks if nadeko is operational.")
                    .Alias(new string[] { "!", "?" })
                    .Do(SayYes());

                cgb.CreateCommand("draw")
                    .Description("Nadeko instructs you to type $draw. Gambling functions start with $")
                    .Do(async e => {
                        await e.Send("Sorry, I don't gamble, type $draw for that function.");
                    });
                cgb.CreateCommand("fire")
                    .Description("Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire.\n**Usage**: @NadekoBot fire [x]")
                    .Parameter("times", ParameterType.Optional)
                    .Do(async e => {
                        int count = 0;
                        if (e.Args?.Length > 0)
                            int.TryParse(e.Args[0], out count);

                        if (count < 1)
                            count = 1;
                        else if (count > 12)
                            count = 12;
                        string str = "";
                        for (int i = 0; i < count; i++) {
                            str += firestr;
                        }
                        await e.Send(str);
                    });

                cgb.CreateCommand("rip")
                    .Description("Shows a grave image of someone with a start year\n**Usage**: @NadekoBot rip @Someone 2000")
                    .Parameter("user", ParameterType.Optional)
                    .Parameter("year", ParameterType.Optional)
                    .Do(async e => {
                        var usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        string text = "";
                        text = usr?.Name;
                        await e.Channel.SendFile("ripzor_m8.png", RipName(text, e.GetArg("year") == "" ? null : e.GetArg("year")));
                    });

                cgb.CreateCommand("j")
                    .Description("Joins a server using a code.")
                    .Parameter("id", ParameterType.Required)
                    .Do(async e => {
                        try {
                            await (await client.GetInvite(e.Args[0])).Accept();
                            await e.Send("I got in!");
                        } catch (Exception) {
                            await e.Send("Invalid code.");
                        }
                    });
                cgb.CreateCommand("slm")
                    .Description("Shows the message where you were last mentioned in this channel (checks last 10k messages)")
                    .Do(async e => {

                        Message msg = null;
                        var msgs = (await e.Channel.DownloadMessages(100))
                                    .Where(m => m.MentionedUsers.Contains(e.User))
                                    .OrderByDescending(m => m.Timestamp);
                        if (msgs.Count() > 0)
                            msg = msgs.First();
                        else {
                            int attempt = 0;
                            Message lastMessage = null;
                            while (msg == null && attempt++ < 5) {
                                var msgsarr = await e.Channel.DownloadMessages(100, lastMessage?.Id);
                                msg = msgsarr
                                        .Where(m => m.MentionedUsers.Contains(e.User))
                                        .OrderByDescending(m => m.Timestamp)
                                        .FirstOrDefault();
                                lastMessage = msgsarr.OrderBy(m => m.Timestamp).First();
                            }
                        }
                        if (msg != null)
                            await e.Send($"Last message mentioning you was at {msg.Timestamp}\n**Message from {msg.User.Name}:** {msg.RawText.Replace("@everyone", "@everryone")}");
                        else
                            await e.Send("I can't find a message mentioning you.");
                    });

                cgb.CreateCommand("bb")
                    .Description("Says bye to someone. **Usage**: @NadekoBot bb @X")
                    .Parameter("ppl", ParameterType.Unparsed)
                    .Do(async e => {
                        string str = "Bye";
                        foreach (var u in e.Message.MentionedUsers) {
                            if (u.Id != NadekoBot.client.CurrentUser.Id)
                                str += " " + u.Mention;
                        }
                        await e.Send(str);
                    });

                cgb.CreateCommand("call")
                    .Description("Useless. Writes calling @X to chat.\n**Usage**: @NadekoBot call @X ")
                    .Parameter("who", ParameterType.Required)
                    .Do(async e => {
                        await e.Send("Calling " + e.Args[0].Replace("@everyone", "[everyone]") + "...");
                    });
                cgb.CreateCommand("hide")
                    .Description("Hides nadeko in plain sight!11!!")
                    .Do(async e => {
                        using (Stream ms = Resources.hidden.ToStream(ImageFormat.Png)) {
                            await client.CurrentUser.Edit(NadekoBot.password, avatar: ms);
                        }
                        await e.Send("*hides*");
                    });

                cgb.CreateCommand("unhide")
                    .Description("Unhides nadeko in plain sight!1!!1")
                    .Do(async e => {
                        using (Stream ms = Resources.nadeko.ToStream()) {
                            await client.CurrentUser.Edit(NadekoBot.password, avatar: ms, avatarType: ImageType.Jpeg);
                        }
                        await e.Send("*unhides*");
                    });

                cgb.CreateCommand("dump")
                    .Description("Dumps all of the invites it can to dump.txt.** Owner Only.**")
                    .Do(async e => {
                        if (NadekoBot.OwnerID != e.User.Id) return;
                        int i = 0;
                        int j = 0;
                        string invites = "";
                        foreach (var s in client.Servers) {
                            try {
                                var invite = await s.CreateInvite(0);
                                invites += invite.Url + "\n";
                                i++;
                            } catch (Exception) {
                                j++;
                                continue;
                            }
                        }
                        File.WriteAllText("dump.txt", invites);
                        await e.Send($"Got invites for {i} servers and failed to get invites for {j} servers");
                    });

                cgb.CreateCommand("ab")
                    .Description("Try to get 'abalabahaha'")
                    .Do(async e => {
                        string[] strings = { "ba", "la", "ha" };
                        string construct = "@a";
                        int cnt = rng.Next(4, 7);
                        while (cnt-- > 0) {
                            construct += strings[rng.Next(0, strings.Length)];
                        }
                        await e.Send(construct);
                    });

                cgb.CreateCommand("av").Alias("avatar")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Shows a mentioned person's avatar. **Usage**: ~av @X")
                    .Do(async e => {
                        var usr = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send("Invalid user specified.");
                            return;
                        }
                        await e.Send(await usr.AvatarUrl.ShortenUrl());
                    });
                /*
                string saved = "";
                cgb.CreateCommand("save")
                  .Description("Saves up to 5 last messages as a quote")
                  .Parameter("number", ParameterType.Required)
                  .Do(e => {
                      var arg = e.GetArg("number");
                      int num;
                      if (!int.TryParse(arg, out num) || num < 1 || num > 5)
                          num = 1;
                      saved = string.Join("\n", e.Channel.Messages.Skip(1).Take(num));
                  });

                cgb.CreateCommand("quote")
                  .Description("Shows the previously saved quote")
                  .Parameter("arg", ParameterType.Required)
                  .Do(async e => {
                      var arg = e.GetArg("arg");
                      await e.Send("```"+saved+"```");
                  });
                  */
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

        public Stream RipName(string name, string year = null) {
            Bitmap bm = Resources.rip;

            int offset = name.Length * 5;

            int fontSize = 20;

            if (name.Length > 10) {
                fontSize -= (name.Length - 10) / 2;
            }

            //TODO use measure string
            Graphics g = Graphics.FromImage(bm);
            g.DrawString(name, new Font("Comic Sans MS", fontSize, FontStyle.Bold), Brushes.Black, 100 - offset, 200);
            g.DrawString((year == null ? "?" : year) + " - " + DateTime.Now.Year, new Font("Consolas", 12, FontStyle.Bold), Brushes.Black, 80, 235);
            g.Flush();
            g.Dispose();

            return bm.ToStream(ImageFormat.Png);
        }

        private Func<CommandEventArgs, Task> SayYes()
            => async e => await e.Send("Yes. :)");
    }
}
