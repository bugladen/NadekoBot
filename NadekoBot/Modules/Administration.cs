using Discord.Modules;
using Discord.Commands;
using Discord;
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using NadekoBot.Commands;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using NadekoBot.Classes._DataModels;
using System.Threading;
using Timer = System.Timers.Timer;

namespace NadekoBot.Modules {
    class Administration : DiscordModule {
        public Administration() : base() {
            commands.Add(new ServerGreetCommand());
            commands.Add(new LogCommand());
            commands.Add(new PlayingRotate());
        }

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {
                
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(".sr").Alias(".setrole")
                    .Description("Sets a role for a given user.\n**Usage**: .sr @User Guest")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles ||
                            string.IsNullOrWhiteSpace(e.GetArg("role_name"))) return;
                        var usr = e.Server.FindUsers(e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("You failed to supply a valid username");
                            return;
                        }

                        var role = e.Server.FindRoles(e.GetArg("role_name")).FirstOrDefault();
                        if (role == null) {
                            await e.Channel.SendMessage("You failed to supply a valid role");
                            return;
                        }

                        try {
                            await usr.AddRoles(new Role[] { role });
                            await e.Channel.SendMessage($"Successfully added role **{role.Name}** to user **{usr.Name}**");
                        } catch (Exception ex) {
                            await e.Channel.SendMessage("Failed to add roles. Most likely reason: Insufficient permissions.\n");
                            Console.WriteLine(ex.ToString());
                        }
                    });

                cgb.CreateCommand(".rr").Alias(".removerole")
                    .Description("Removes a role from a given user.\n**Usage**: .rr @User Admin")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles ||
                            string.IsNullOrWhiteSpace("role_name")) return;

                        var usr = e.Server.FindUsers(e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("You failed to supply a valid username");
                            return;
                        }
                        var role = e.Server.FindRoles(e.GetArg("role_name")).FirstOrDefault();
                        if (role == null) {
                            await e.Channel.SendMessage("You failed to supply a valid role");
                            return;
                        }

                        try {
                            await usr.RemoveRoles(new Role[] { role });
                            await e.Channel.SendMessage($"Successfully removed role **{role.Name}** from user **{usr.Name}**");
                        } catch (InvalidOperationException) {
                        } catch  {
                            await e.Channel.SendMessage("Failed to remove roles. Most likely reason: Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".r").Alias(".role").Alias(".cr")
                    .Description("Creates a role with a given name.**Usage**: .r Awesome Role")
                    .Parameter("role_name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles) return;
                        if (string.IsNullOrWhiteSpace(e.GetArg("role_name")))
                            return;
                        try {
                            var r = await e.Server.CreateRole(e.GetArg("role_name"));
                            await e.Channel.SendMessage($"Successfully created role **{r.Name}**.");
                        } catch (Exception ex) {
                            await e.Channel.SendMessage(":warning: Unspecified error.");
                        }
                    });

                cgb.CreateCommand(".rolecolor").Alias(".rc")
                    .Parameter("Rolename", ParameterType.Required)
                    .Parameter("r", ParameterType.Optional)
                    .Parameter("g", ParameterType.Optional)
                    .Parameter("b", ParameterType.Optional)
                    .Description("Set a role's color to the hex or 0-255 rgb color value provided.\n**Usage**: .color Admin 255 200 100 or .color Admin ffba55")
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles) {
                            await e.Channel.SendMessage("You don't have permission to use this!");
                            return;
                        }

                        var args = e.Args.Where(s => s != String.Empty);

                        if (args.Count() != 2 && args.Count() != 4) {
                            await e.Channel.SendMessage("The parameters are invalid.");
                            return;
                        }

                        Role role = e.Server.FindRoles(e.Args[0]).FirstOrDefault();

                        if (role == null) {
                            await e.Channel.SendMessage("That role does not exist.");
                            return;
                        }
                        try {
                            bool rgb = args.Count() == 4;

                            byte red = Convert.ToByte(rgb ? int.Parse(e.Args[1]) : Convert.ToInt32(e.Args[1].Substring(0, 2), 16));
                            byte green = Convert.ToByte(rgb ? int.Parse(e.Args[2]) : Convert.ToInt32(e.Args[1].Substring(2, 2), 16));
                            byte blue = Convert.ToByte(rgb ? int.Parse(e.Args[3]) : Convert.ToInt32(e.Args[1].Substring(4, 2), 16));

                            await role.Edit(color: new Color(red, green, blue));
                            await e.Channel.SendMessage($"Role {role.Name}'s color has been changed.");
                        } catch (Exception ex) {
                            await e.Channel.SendMessage(":warning: Unspecified error, please report this.");
                            Console.WriteLine($".rolecolor error: {ex}");
                        }

                    });

                cgb.CreateCommand(".roles")
                  .Description("List all roles on this server or a single user if specified.")
                  .Parameter("user", ParameterType.Unparsed)
                  .Do(async e => {

                      if (!string.IsNullOrWhiteSpace(e.GetArg("user"))) {
                          var usr = e.Server.FindUsers(e.GetArg("user")).FirstOrDefault();
                          if (usr != null) {
                              await e.Channel.SendMessage($"`List of roles for **{usr.Name}**:` \n• " + string.Join("\n• ", usr.Roles));
                              return;
                          }
                      }
                      await e.Channel.SendMessage("`List of roles:` \n• " + string.Join("\n• ", e.Server.Roles));
                  });

                cgb.CreateCommand(".b").Alias(".ban")
                    .Parameter("everything", ParameterType.Unparsed)
                    .Description("Bans a mentioned user.")
                        .Do(async e => {
                            try {
                                if (e.User.ServerPermissions.BanMembers && e.Message.MentionedUsers.Any()) {
                                    var usr = e.Message.MentionedUsers.First();
                                    await usr.Server.Ban(usr);
                                    await e.Channel.SendMessage("Banned user " + usr.Name + " Id: " + usr.Id);
                                }
                            } catch (Exception ex) { }
                        });

                cgb.CreateCommand(".ub").Alias(".unban")
                    .Parameter("everything", ParameterType.Unparsed)
                    .Description("Unbans a mentioned user.")
                        .Do(async e => {
                            try {
                                if (e.User.ServerPermissions.BanMembers && e.Message.MentionedUsers.Any()) {
                                    var usr = e.Message.MentionedUsers.First();
                                    await usr.Server.Unban(usr);
                                    await e.Channel.SendMessage("Unbanned user " + usr.Name + " Id: " + usr.Id);
                                }
                            } catch  { }
                        });

                cgb.CreateCommand(".k").Alias(".kick")
                    .Parameter("user")
                    .Description("Kicks a mentioned user.")
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.KickMembers && e.Message.MentionedUsers.Any()) {
                                var usr = e.Message.MentionedUsers.First();
                                await e.Message.MentionedUsers.First().Kick();
                                await e.Channel.SendMessage("Kicked user " + usr.Name + " Id: " + usr.Id);
                            }
                        } catch  {
                            await e.Channel.SendMessage("No sufficient permissions.");
                        }
                    });
                cgb.CreateCommand(".mute")
                    .Description("Mutes mentioned user or users.")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.MuteMembers) {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isMuted: true);
                            }
                            await e.Channel.SendMessage("Mute successful");
                        } catch  {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".unmute")
                    .Description("Unmutes mentioned user or users.")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.MuteMembers) {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isMuted: false);
                            }
                            await e.Channel.SendMessage("Unmute successful");
                        } catch  {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".deafen")
                    .Alias(".deaf")
                    .Description("Deafens mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.DeafenMembers) {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isDeafened: true);
                            }
                            await e.Channel.SendMessage("Deafen successful");
                        } catch  {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".undeafen")
                    .Alias(".undeaf")
                    .Description("Undeafens mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.DeafenMembers) {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isDeafened: false);
                            }
                            await e.Channel.SendMessage("Undeafen successful");
                        } catch  {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".rvch")
                    .Description("Removes a voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Voice).FirstOrDefault()?.Delete();
                                await e.Channel.SendMessage($"Removed channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch  {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".vch").Alias(".cvch")
                    .Description("Creates a new voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Voice);
                                await e.Channel.SendMessage($"Created voice channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch  {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".rch").Alias(".rtch")
                    .Description("Removes a text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Text).FirstOrDefault()?.Delete();
                                await e.Channel.SendMessage($"Removed text channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch  {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".ch").Alias(".tch")
                    .Description("Creates a new text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Text);
                                await e.Channel.SendMessage($"Added text channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch  {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".st").Alias(".settopic")
                    .Description("Sets a topic on the current channel.")
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels)
                                await e.Channel.Edit(topic: e.GetArg("topic"));
                        } catch  { }
                    });

                cgb.CreateCommand(".uid").Alias(".userid")
                    .Description("Shows user ID.")
                    .Parameter("user", ParameterType.Optional)
                    .Do(async e => {
                        var usr = e.User;
                        if (e.GetArg("user") != null) usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        await e.Channel.SendMessage($"Id of the user { usr.Name } is { usr.Id }");
                    });

                cgb.CreateCommand(".cid").Alias(".channelid")
                    .Description("Shows current channel ID.")
                    .Do(async e => await e.Channel.SendMessage("This channel's ID is " + e.Channel.Id));

                cgb.CreateCommand(".sid").Alias(".serverid")
                    .Description("Shows current server ID.")
                    .Do(async e => await e.Channel.SendMessage("This server's ID is " + e.Server.Id));

                cgb.CreateCommand(".stats")
                    .Description("Shows some basic stats for Nadeko.")
                    .Do(async e => {
                        var t = Task.Run(() => {
                            return NadekoStats.Instance.GetStats() + "`" + Music.GetMusicStats() + "`";
                        });

                        await e.Channel.SendMessage(await t);

                    });

                cgb.CreateCommand(".leaveall")
                    .Description("Nadeko leaves all servers **OWNER ONLY**")
                    .Do(e => {
                        if (e.User.Id == NadekoBot.OwnerID)
                            NadekoBot.client.Servers.ForEach(async s => { if (s.Name == e.Server.Name) return; await s.Leave(); });
                    });

                cgb.CreateCommand(".prune")
                    .Parameter("num", ParameterType.Required)
                    .Description("Prunes a number of messages from the current channel.\n**Usage**: .prune 5")
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageMessages) return;
                        int val;
                        if (string.IsNullOrWhiteSpace(e.GetArg("num")) || !int.TryParse(e.GetArg("num"), out val) || val < 0)
                            return;

                        foreach (var msg in await e.Channel.DownloadMessages(val)) {
                            await msg.Delete();
                            await Task.Delay(100);
                        }
                    });

                cgb.CreateCommand(".die")
                    .Alias(".graceful")
                    .Description("Works only for the owner. Shuts the bot down and notifies users about the restart.")
                    .Do(async e => {
                        if (e.User.Id == NadekoBot.OwnerID) {
                            Timer t = new Timer();
                            t.Interval = 2000;
                            t.Elapsed += (s, ev) => { Environment.Exit(0); };
                            t.Start();
                            await e.Channel.SendMessage("`Shutting down.`");
                        }
                    });

                ConcurrentDictionary<Server, bool> clearDictionary = new ConcurrentDictionary<Server, bool>();
                cgb.CreateCommand(".clr")
                    .Description("Clears some of Nadeko's (or some other user's if supplied) messages from the current channel.\n**Usage**: .clr @X")
                    .Parameter("user",ParameterType.Unparsed)
                    .Do(async e => {
                        var usrId = NadekoBot.client.CurrentUser.Id;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user"))) {
                            var usr = e.Server.FindUsers(e.GetArg("user")).FirstOrDefault();
                            if (usr != null)
                                usrId = usr.Id;
                        }
                        await Task.Run(async () => {
                            var msgs = (await e.Channel.DownloadMessages(100)).Where(m => m.User.Id == usrId);
                            foreach (var m in msgs) {
                                try {
                                    await m.Delete();
                                }
                                catch { }
                                await Task.Delay(200);
                            }
                                
                        });
                    });

                cgb.CreateCommand(".newname")
                    .Alias(".setname")
                    .Description("Give the bot a new name.")
                    .Parameter("new_name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID || e.GetArg("new_name") == null) return;

                        await client.CurrentUser.Edit(NadekoBot.password, e.GetArg("new_name"));
                    });

                cgb.CreateCommand(".newavatar")
                    .Alias(".setavatar")
                    .Description("Sets a new avatar image for the NadekoBot.")
                    .Parameter("img", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID || string.IsNullOrWhiteSpace(e.GetArg("img")))
                            return;
                        // Gather user provided URL.
                        string avatarAddress = e.GetArg("img");
                        // Creates an HTTPWebRequest object, which references the URL given by the user.
                        System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(Uri.EscapeUriString(avatarAddress));
                        // Discard the response if image isnt downloaded in 5 s as to not lock Nadeko. Prevents loading from faulty links.
                        webRequest.Timeout = 5000;
                        // Gathers the webRequest response as a Stream object.
                        System.Net.WebResponse webResponse = await webRequest.GetResponseAsync();
                        // Create image object from the response we got from the webRequest stream. This is because there is no "GetResponseStream".
                        System.Drawing.Image image = System.Drawing.Image.FromStream(webResponse.GetResponseStream());
                        // Save the image to disk.
                        image.Save("data/avatar.png", System.Drawing.Imaging.ImageFormat.Png);
                        await client.CurrentUser.Edit(NadekoBot.password, avatar: image.ToStream());
                        // Send confirm.
                        await e.Channel.SendMessage("New avatar set.");
                    });

                cgb.CreateCommand(".setgame")
                  .Description("Sets the bots game.")
                  .Parameter("set_game", ParameterType.Unparsed)
                  .Do(e => {
                      if (e.User.Id != NadekoBot.OwnerID || e.GetArg("set_game") == null) return;

                      client.SetGame(e.GetArg("set_game"));
                  });

                cgb.CreateCommand(".checkmyperms")
                    .Description("Checks your userspecific permissions on this channel.")
                    .Do(async e => {
                        string output = "```\n";
                        foreach (var p in e.User.ServerPermissions.GetType().GetProperties().Where(p => p.GetGetMethod().GetParameters().Count() == 0)) {
                            output += p.Name + ": " + p.GetValue(e.User.ServerPermissions, null).ToString() + "\n";
                        }
                        output += "```";
                        await e.User.SendMessage(output);
                    });

                Server commsServer = null;
                User commsUser = null;
                Channel commsChannel = null;

                cgb.CreateCommand(".commsuser")
                    .Description("Sets a user for through-bot communication. Only works if server is set. Resets commschannel.**Owner only**.")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        commsUser = commsServer?.FindUsers(e.GetArg("name")).FirstOrDefault();
                        if (commsUser != null) {
                            commsChannel = null;
                            await e.Channel.SendMessage("User for comms set.");
                        } else
                            await e.Channel.SendMessage("No server specified or user.");
                    });

                cgb.CreateCommand(".commsserver")
                    .Description("Sets a server for through-bot communication.**Owner only**.")
                    .Parameter("server", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        commsServer = client.FindServers(e.GetArg("server")).FirstOrDefault();
                        if (commsServer != null)
                            await e.Channel.SendMessage("Server for comms set.");
                        else
                            await e.Channel.SendMessage("No such server.");
                    });

                cgb.CreateCommand(".commschannel")
                    .Description("Sets a channel for through-bot communication. Only works if server is set. Resets commsuser.**Owner only**.")
                    .Parameter("ch", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        commsChannel = commsServer?.FindChannels(e.GetArg("ch"), ChannelType.Text).FirstOrDefault();
                        if (commsChannel != null) {
                            commsUser = null;
                            await e.Channel.SendMessage("Server for comms set.");
                        } else
                            await e.Channel.SendMessage("No server specified or channel is invalid.");
                    });

                cgb.CreateCommand(".send")
                    .Description("Send a message to someone on a different server through the bot.**Owner only.**\n **Usage**: .send Message text multi word!")
                    .Parameter("msg", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        if (commsUser != null)
                            await commsUser.SendMessage(e.GetArg("msg"));
                        else if (commsChannel != null)
                            await commsChannel.SendMessage(e.GetArg("msg"));
                        else
                            await e.Channel.SendMessage("Failed. Make sure you've specified server and [channel or user]");
                    });

                cgb.CreateCommand(".menrole")
                    .Alias(".mentionrole")
                    .Description("Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission.")
                    .Parameter("roles", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.MentionEveryone) return;
                        var arg = e.GetArg("roles").Split(',').Select(r => r.Trim());
                        string send = $"--{e.User.Mention} has invoked a mention on the following roles--";
                        foreach (var roleStr in arg) {
                            if (string.IsNullOrWhiteSpace(roleStr)) continue;
                            var role = e.Server.FindRoles(roleStr).FirstOrDefault();
                            if (role == null) continue;
                            send += $"\n`{role.Name}`\n";
                            send += string.Join(", ", role.Members.Select(r => r.Mention));
                        }

                        while (send.Length > 2000) {
                            var curstr = send.Substring(0, 2000);
                            await e.Channel.Send(curstr.Substring(0, curstr.LastIndexOf(", ") + 1));
                            send = curstr.Substring(curstr.LastIndexOf(", ") + 1) + send.Substring(2000);
                        }
                        await e.Channel.Send(send);
                    });

                cgb.CreateCommand(".parsetosql")
                  .Description("Loads exported parsedata from /data/parsedata/ into sqlite database.")
                  .Do(async e => {
                      if (e.User.Id != NadekoBot.OwnerID)
                          return;
                      await Task.Run(() => {
                          SaveParseToDb<Announcement>("data/parsedata/Announcements.json");
                          SaveParseToDb<Classes._DataModels.Command>("data/parsedata/CommandsRan.json");
                          SaveParseToDb<Request>("data/parsedata/Requests.json");
                          SaveParseToDb<Stats>("data/parsedata/Stats.json");
                          SaveParseToDb<TypingArticle>("data/parsedata/TypingArticles.json");
                      });
                  });

                cgb.CreateCommand(".unstuck")
                  .Description("Clears the message queue. **OWNER ONLY**")
                  .Do(async e => {
                      if (e.User.Id != NadekoBot.OwnerID)
                          return;
                      await Task.Run(() => NadekoBot.client.MessageQueue.Clear());
                  });

                cgb.CreateCommand(".donators")
                    .Description("List of lovely people who donated to keep this project alive.")
                    .Do(async e => {
                        await Task.Run(async () => {
                            var rows = Classes.DBHandler.Instance.GetAllRows<Donator>();
                            var donatorsOrdered = rows.OrderByDescending(d => d.Amount);
                            string str = $"**Thanks to the people listed below for making this project happen!**\n";

                            await e.Channel.SendMessage(str + string.Join("⭐", donatorsOrdered.Select(d => d.UserName)));
                        });
                    });

                //THIS IS INTENTED TO BE USED ONLY BY THE ORIGINAL BOT OWNER
                cgb.CreateCommand(".adddon")
                    .Alias(".donadd")
                    .Description("Add a donator to the database.")
                    .Parameter("donator")
                    .Parameter("amount")
                    .Do(e => {
                        try {
                            if (NadekoBot.OwnerID != e.User.Id)
                                return;
                            var donator = e.Server.FindUsers(e.GetArg("donator")).FirstOrDefault();
                            var amount = int.Parse(e.GetArg("amount"));
                            Classes.DBHandler.Instance.InsertData(new Donator {
                                Amount = amount,
                                UserName = donator.Name,
                                UserId = (long)e.User.Id
                            });
                            e.Channel.SendMessage("Successfuly added a new donator. 👑");
                        } catch (Exception ex) {
                            Console.WriteLine(ex);
                            Console.WriteLine("---------------\nInner error:\n" + ex.InnerException);
                        }
                    });

                cgb.CreateCommand(".videocall")
                  .Description("Creates a private appear.in video call link for you and other mentioned people. The link is sent to mentioned people via a private message.")
                  .Parameter("arg", ParameterType.Unparsed)
                  .Do(async e => {
                      try {
                          string str = "http://appear.in/";
                          var allUsrs = e.Message.MentionedUsers.Union(new User[] { e.User });
                          foreach (var usr in allUsrs) {
                              str += Uri.EscapeUriString(usr.Name[0].ToString());
                          }
                          str += new Random().Next(100000, 1000000);
                          foreach (var usr in allUsrs) {
                              await usr.SendMessage(str);
                          }
                      }
                      catch (Exception ex) {
                          Console.WriteLine(ex);
                      }
                  });
            });
        }

        public void SaveParseToDb<T>(string where) where T : IDataModel {
            try {
                var data = File.ReadAllText(where);
                var arr = JObject.Parse(data)["results"] as JArray;
                var objects = new List<T>();
                foreach (JObject obj in arr) {
                    objects.Add(obj.ToObject<T>());
                }
                Classes.DBHandler.Instance.InsertMany(objects);
            }
            catch { }
        }
    }
}
