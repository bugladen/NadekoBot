using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using NadekoBot.Extensions;
using NadekoBot.Properties;
using NadekoBot.Commands;

namespace NadekoBot.Modules {
    internal class Conversations : DiscordModule {
        private const string firestr = "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥";
        public Conversations() {
            commands.Add(new CopyCommand(this));
            commands.Add(new RequestsCommand(this));
        }

        public override string Prefix { get; } = String.Format(NadekoBot.Config.CommandPrefixes.Conversations, NadekoBot.Creds.BotId);

        public override void Install(ModuleManager manager) {
            var rng = new Random();

            manager.CreateCommands("", cgb => {
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                cgb.CreateCommand("e")
                    .Description("You did it.")
                    .Do(async e => {
                        await e.Channel.SendMessage($"{e.User.Name} did it. 😒 🔫");
                    });

                cgb.CreateCommand("\\o\\")
                    .Description("Nadeko replies with /o/")
                    .Do(async e => await e.Channel.SendMessage(e.User.Mention + "/o/"));

                cgb.CreateCommand("/o/")
                    .Description("Nadeko replies with \\o\\")
                    .Do(async e => await e.Channel.SendMessage(e.User.Mention + "\\o\\"));

                cgb.CreateCommand("..")
                    .Description("Adds a new quote with the specified name (single word) and message (no limit).\n**Usage**: .. abc My message")
                    .Parameter("keyword", ParameterType.Required)
                    .Parameter("text", ParameterType.Unparsed)
                    .Do(async e => {
                        var text = e.GetArg("text");
                        if (string.IsNullOrWhiteSpace(text))
                            return;
                        await Task.Run(() =>
                            Classes.DbHandler.Instance.InsertData(new Classes._DataModels.UserQuote() {
                                DateAdded = DateTime.Now,
                                Keyword = e.GetArg("keyword").ToLowerInvariant(),
                                Text = text,
                                UserName = e.User.Name,
                            }));

                        await e.Channel.SendMessage("`New quote added.`");
                    });

                cgb.CreateCommand("...")
                    .Description("Shows a random quote with a specified name.\n**Usage**: .. abc")
                    .Parameter("keyword", ParameterType.Required)
                    .Do(async e => {
                        var keyword = e.GetArg("keyword")?.ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(keyword))
                            return;

                        var quote =
                            Classes.DbHandler.Instance.GetRandom<Classes._DataModels.UserQuote>(
                                uqm => uqm.Keyword == keyword);

                        if (quote != null)
                            await e.Channel.SendMessage($"📣 {quote.Text}");
                        else
                            await e.Channel.SendMessage("💢`No quote found.`");
                    });
            });

            manager.CreateCommands(NadekoBot.BotMention, cgb => {
                var client = manager.Client;

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("uptime")
                    .Description("Shows how long Nadeko has been running for.")
                    .Do(async e => {
                        var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
                        var str = string.Format("I have been running for {0} days, {1} hours, and {2} minutes.", time.Days, time.Hours, time.Minutes);
                        await e.Channel.SendMessage(str);
                    });

                cgb.CreateCommand("die")
                    .Description("Works only for the owner. Shuts the bot down.")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id)) {
                            await e.Channel.SendMessage(e.User.Mention + ", Yes, my love.");
                            await Task.Delay(5000);
                            Environment.Exit(0);
                        } else
                            await e.Channel.SendMessage(e.User.Mention + ", No.");
                    });

                var randServerSw = new Stopwatch();
                randServerSw.Start();

                cgb.CreateCommand("do you love me")
                    .Description("Replies with positive answer only to the bot owner.")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id))
                            await e.Channel.SendMessage(e.User.Mention + ", Of course I do, my Master.");
                        else
                            await e.Channel.SendMessage(e.User.Mention + ", Don't be silly.");
                    });

                cgb.CreateCommand("how are you")
                    .Description("Replies positive only if bot owner is online.")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id)) {
                            await e.Channel.SendMessage(e.User.Mention + " I am great as long as you are here.");
                            return;
                        }
                        var kw = e.Server.GetUser(NadekoBot.Creds.OwnerIds[0]);
                        if (kw != null && kw.Status == UserStatus.Online) {
                            await e.Channel.SendMessage(e.User.Mention + " I am great as long as " + kw.Mention + " is with me.");
                        } else {
                            await e.Channel.SendMessage(e.User.Mention + " I am sad. My Master is not with me.");
                        }
                    });

                cgb.CreateCommand("insult")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Insults @X person.\n**Usage**: @NadekoBot insult @X.")
                    .Do(async e => {
                        var u = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (u == null) {
                            await e.Channel.SendMessage("Invalid user specified.");
                            return;
                        }

                        if (NadekoBot.IsOwner(u.Id)) {
                            await e.Channel.SendMessage("I would never insult my master <3");
                            return;
                        }
                        await e.Channel.SendMessage(u.Mention + NadekoBot.Locale.Insults[rng.Next(0, NadekoBot.Locale.Insults.Length)]);
                    });

                cgb.CreateCommand("praise")
                    .Description("Praises @X person.\n**Usage**: @NadekoBot praise @X.")
                    .Parameter("mention", ParameterType.Required)
                    .Do(async e => {
                        var u = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();

                        if (u == null) {
                            await e.Channel.SendMessage("Invalid user specified.");
                            return;
                        }

                        if (NadekoBot.IsOwner(u.Id)) {
                            await e.Channel.SendMessage(e.User.Mention + " I don't need your permission to praise my beloved Master <3");
                            return;
                        }
                        await e.Channel.SendMessage(u.Mention + NadekoBot.Locale.Praises[rng.Next(0, NadekoBot.Locale.Praises.Length)]);
                    });

                cgb.CreateCommand("pat")
                  .Description("Pat someone ^_^")
                  .Parameter("user", ParameterType.Unparsed)
                  .Do(async e => {
                      var userStr = e.GetArg("user");
                      if (string.IsNullOrWhiteSpace(userStr) || !e.Message.MentionedUsers.Any()) return;
                      var user = e.Server.FindUsers(userStr).FirstOrDefault();
                      if (user == null)
                          return;
                      try {
                          await e.Channel.SendMessage(
                                    $"{user.Mention} " +
                                    $"{NadekoBot.Config.PatResponses[rng.Next(0, NadekoBot.Config.PatResponses.Length)]}");
                      } catch {
                          await e.Channel.SendMessage("Error while handling PatResponses check your data/config.json");
                      }
                  });

                cgb.CreateCommand("cry")
                  .Description("Tell Nadeko to cry. You are a heartless monster if you use this command.")
                  .Do(async e => {
                      try {
                          await
                              e.Channel.SendMessage(
                                  $"(•̥́ _•ૅ｡)\n{NadekoBot.Config.CryResponses[rng.Next(0, NadekoBot.Config.CryResponses.Length)]}");
                      } catch {
                          await e.Channel.SendMessage("Error while handling CryResponses check your data/config.json");
                      }
                  });

                cgb.CreateCommand("disguise")
                  .Description("Tell Nadeko to disguise herself.")
                  .Do(async e => {
                      try {
                          await
                              e.Channel.SendMessage(
                                  $"{NadekoBot.Config.DisguiseResponses[rng.Next(0, NadekoBot.Config.DisguiseResponses.Length)]}");
                      } catch {
                          await e.Channel.SendMessage("Error while handling DisguiseResponses check your data/config.json");
                      }
                  });

                cgb.CreateCommand("are you real")
                    .Description("Useless.")
                    .Do(async e => {
                        await e.Channel.SendMessage(e.User.Mention + " I will be soon.");
                    });

                cgb.CreateCommand("are you there")
                    .Description("Checks if Nadeko is operational.")
                    .Alias("!", "?")
                    .Do(SayYes());

                cgb.CreateCommand("draw")
                    .Description("Nadeko instructs you to type $draw. Gambling functions start with $")
                    .Do(async e => {
                        await e.Channel.SendMessage("Sorry, I don't gamble, type $draw for that function.");
                    });
                cgb.CreateCommand("fire")
                    .Description("Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire.\n**Usage**: @NadekoBot fire [x]")
                    .Parameter("times", ParameterType.Optional)
                    .Do(async e => {
                        var count = 1;
                        int.TryParse(e.Args[0], out count);
                        if (count == 0)
                            count = 1;
                        if (count < 1 || count > 12) {
                            await e.Channel.SendMessage("Number must be between 0 and 12");
                            return;
                        }

                        var str = "";
                        for (var i = 0; i < count; i++) {
                            str += firestr;
                        }
                        await e.Channel.SendMessage(str);
                    });

                cgb.CreateCommand("rip")
                    .Description("Shows a grave image of someone with a start year\n**Usage**: @NadekoBot rip @Someone 2000")
                    .Parameter("user", ParameterType.Required)
                    .Parameter("year", ParameterType.Optional)
                    .Do(async e => {
                        if (string.IsNullOrWhiteSpace(e.GetArg("user")))
                            return;
                        var usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        var text = "";
                        text = usr?.Name ?? e.GetArg("user");
                        await
                            e.Channel.SendFile("ripzor_m8.png",
                                RipName(text, string.IsNullOrWhiteSpace(e.GetArg("year")) ? null : e.GetArg("year")));
                    });
                if (!NadekoBot.Config.DontJoinServers) {
                    cgb.CreateCommand("j")
                        .Description("Joins a server using a code.")
                        .Parameter("id", ParameterType.Required)
                        .Do(async e => {
                            var invite = await client.GetInvite(e.Args[0]);
                            if (invite != null) {
                                try {
                                    await invite.Accept();
                                } catch {
                                    await e.Channel.SendMessage("Failed to accept invite.");
                                }
                                await e.Channel.SendMessage("I got in!");
                                return;
                            }
                            await e.Channel.SendMessage("Invalid code.");
                        });
                }

                cgb.CreateCommand("slm")
                    .Description("Shows the message where you were last mentioned in this channel (checks last 10k messages)")
                    .Do(async e => {

                        Message msg = null;
                        var msgs = (await e.Channel.DownloadMessages(100))
                                    .Where(m => m.MentionedUsers.Contains(e.User))
                                    .OrderByDescending(m => m.Timestamp);
                        if (msgs.Any())
                            msg = msgs.First();
                        else {
                            var attempt = 0;
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
                            await e.Channel.SendMessage($"Last message mentioning you was at {msg.Timestamp}\n**Message from {msg.User.Name}:** {msg.RawText}");
                        else
                            await e.Channel.SendMessage("I can't find a message mentioning you.");
                    });

                cgb.CreateCommand("bb")
                    .Description("Says bye to someone.\n**Usage**: @NadekoBot bb @X")
                    .Parameter("ppl", ParameterType.Unparsed)
                    .Do(async e => {
                        var str = "Bye";
                        foreach (var u in e.Message.MentionedUsers) {
                            if (u.Id != NadekoBot.Client.CurrentUser.Id)
                                str += " " + u.Mention;
                        }
                        await e.Channel.SendMessage(str);
                    });

                cgb.CreateCommand("call")
                    .Description("Useless. Writes calling @X to chat.\n**Usage**: @NadekoBot call @X ")
                    .Parameter("who", ParameterType.Required)
                    .Do(async e => {
                        await e.Channel.SendMessage("Calling " + e.Args[0] + "...");
                    });
                cgb.CreateCommand("hide")
                    .Description("Hides Nadeko in plain sight!11!!")
                    .Do(async e => {
                        using (var ms = Resources.hidden.ToStream(ImageFormat.Png)) {
                            await client.CurrentUser.Edit(NadekoBot.Creds.Password, avatar: ms);
                        }
                        await e.Channel.SendMessage("*hides*");
                    });

                cgb.CreateCommand("unhide")
                    .Description("Unhides Nadeko in plain sight!1!!1")
                    .Do(async e => {
                        using (var fs = new FileStream("data/avatar.png", FileMode.Open)) {
                            await client.CurrentUser.Edit(NadekoBot.Creds.Password, avatar: fs);
                        }
                        await e.Channel.SendMessage("*unhides*");
                    });

                cgb.CreateCommand("dump")
                    .Description("Dumps all of the invites it can to dump.txt.** Owner Only.**")
                    .Do(async e => {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        var i = 0;
                        var j = 0;
                        var invites = "";
                        foreach (var s in client.Servers) {
                            try {
                                var invite = await s.CreateInvite(0);
                                invites += invite.Url + "\n";
                                i++;
                            } catch {
                                j++;
                                continue;
                            }
                        }
                        File.WriteAllText("dump.txt", invites);
                        await e.Channel.SendMessage($"Got invites for {i} servers and failed to get invites for {j} servers");
                    });

                cgb.CreateCommand("ab")
                    .Description("Try to get 'abalabahaha'")
                    .Do(async e => {
                        string[] strings = { "ba", "la", "ha" };
                        var construct = "@a";
                        var cnt = rng.Next(4, 7);
                        while (cnt-- > 0) {
                            construct += strings[rng.Next(0, strings.Length)];
                        }
                        await e.Channel.SendMessage(construct + "~");
                    });

                cgb.CreateCommand("av").Alias("avatar")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Shows a mentioned person's avatar.\n**Usage**: ~av @X")
                    .Do(async e => {
                        var usr = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("Invalid user specified.");
                            return;
                        }
                        await e.Channel.SendMessage(await usr.AvatarUrl.ShortenUrl());
                    });
            });
        }

        public Stream RipName(string name, string year = null) {
            var bm = Resources.rip;

            var offset = name.Length * 5;

            var fontSize = 20;

            if (name.Length > 10) {
                fontSize -= (name.Length - 10) / 2;
            }

            //TODO use measure string
            var g = Graphics.FromImage(bm);
            g.DrawString(name, new Font("Comic Sans MS", fontSize, FontStyle.Bold), Brushes.Black, 100 - offset, 200);
            g.DrawString((year ?? "?") + " - " + DateTime.Now.Year, new Font("Consolas", 12, FontStyle.Bold), Brushes.Black, 80, 235);
            g.Flush();
            g.Dispose();

            return bm.ToStream(ImageFormat.Png);
        }

        private static Func<CommandEventArgs, Task> SayYes()
            => async e => await e.Channel.SendMessage("Yes. :)");
    }
}
