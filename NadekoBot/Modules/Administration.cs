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

namespace NadekoBot.Modules {
    class Administration : DiscordModule {
        public Administration() : base() {
            commands.Add(new HelpCommand());
            commands.Add(new ServerGreetCommand());
        }

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {
                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(".sr").Alias(".setrole")
                    .Description("Sets a role for a given user.\n**Usage**: .sr @User Guest")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Required)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles) return;
                        var usr = e.Server.FindUsers(e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send("You failed to supply a valid username");
                            return;
                        }

                        var role = e.Server.FindRoles(e.GetArg("role_name")).FirstOrDefault();
                        if (role == null) {
                            await e.Send("You failed to supply a valid role");
                            return;
                        }

                        try {
                            await usr.AddRoles(new Role[] { role });
                            await e.Send($"Successfully added role **{role.Name}** to user **{usr.Mention}**");
                        } catch (Exception ex) {
                            await e.Send("Failed to add roles. Most likely reason: Insufficient permissions.\n");
                            Console.WriteLine(ex.ToString());
                        }
                    });

                cgb.CreateCommand(".rr").Alias(".removerole")
                    .Description("Removes a role from a given user.\n**Usage**: .rr @User Admin")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Required)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles) return;

                        var usr = e.Server.FindUsers(e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send("You failed to supply a valid username");
                            return;
                        }
                        var role = e.Server.FindRoles(e.GetArg("role_name")).FirstOrDefault();
                        if (role == null) {
                            await e.Send("You failed to supply a valid role");
                            return;
                        }

                        try {
                            await usr.RemoveRoles(new Role[] { role });
                            await e.Send($"Successfully removed role **{role.Name}** from user **{usr.Mention}**");
                        } catch (InvalidOperationException) {
                        } catch (Exception) {
                            await e.Send("Failed to remove roles. Most likely reason: Insufficient permissions.");
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
                            await e.Send($"Successfully created role **{r.Name}**.");
                        } catch (Exception ex) {
                            await e.Send(":warning: Unspecified error.");
                        }
                    });

                cgb.CreateCommand(".rolecolor").Alias(".rc")
                    .Parameter("Rolename", ParameterType.Required)
                    .Parameter("r", ParameterType.Optional)
                    .Parameter("g", ParameterType.Optional)
                    .Parameter("b", ParameterType.Optional)
                    .Description("Set a role's color to the hex or 0-255 color value provided.\n**Usage*: .color Admin 255 200 100 or .color Admin ffba55")
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageRoles) {
                            await e.Channel.SendMessage("You don't have permission to use this!");
                            return;
                        }

                        var args = e.Args.Where(s => s != String.Empty);

                        if (args.Count() != 2 && args.Count() != 4) {
                            await e.Send("The parameters are invalid.");
                            return;
                        }

                        Role role = e.Server.FindRoles(e.Args[0]).FirstOrDefault();

                        if (role == null) {
                            await e.Send("That role does not exist.");
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
                            await e.Send(":warning: Unspecified error, please report this.");
                            Console.WriteLine($".rolecolor error: {ex}");
                        }

                    });
                cgb.CreateCommand(".roles")
                  .Description("List all roles on this server")
                  .Do(async e => {
                      await e.Send("`List of roles:` \n• " + string.Join("\n• ", e.Server.Roles).Replace("@everyone", "[everyone]"));
                  });

                cgb.CreateCommand(".b").Alias(".ban")
                    .Parameter("everything", ParameterType.Unparsed)
                    .Description("Bans a mentioned user")
                        .Do(async e => {
                            try {
                                if (e.User.ServerPermissions.BanMembers && e.Message.MentionedUsers.Any()) {
                                    var usr = e.Message.MentionedUsers.First();
                                    await usr.Server.Ban(usr);
                                    await e.Send("Banned user " + usr.Name + " Id: " + usr.Id);
                                }
                            } catch (Exception ex) { }
                        });

                cgb.CreateCommand(".ub").Alias(".unban")
                    .Parameter("everything", ParameterType.Unparsed)
                    .Description("Unbans a mentioned user")
                        .Do(async e => {
                            try {
                                if (e.User.ServerPermissions.BanMembers && e.Message.MentionedUsers.Any()) {
                                    var usr = e.Message.MentionedUsers.First();
                                    await usr.Server.Unban(usr);
                                    await e.Send("Unbanned user " + usr.Name + " Id: " + usr.Id);
                                }
                            } catch (Exception) { }
                        });

                cgb.CreateCommand(".k").Alias(".kick")
                    .Parameter("user")
                    .Description("Kicks a mentioned user.")
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.KickMembers && e.Message.MentionedUsers.Any()) {
                                var usr = e.Message.MentionedUsers.First();
                                await e.Message.MentionedUsers.First().Kick();
                                await e.Send("Kicked user " + usr.Name + " Id: " + usr.Id);
                            }
                        } catch (Exception) {
                            await e.Send("No sufficient permissions.");
                        }
                    });
                cgb.CreateCommand(".mute")
                    .Description("Mutes mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.MuteMembers) {
                            await e.Send("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isMuted: true);
                            }
                            await e.Send("Mute successful");
                        } catch (Exception) {
                            await e.Send("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".unmute")
                    .Description("Unmutes mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.MuteMembers) {
                            await e.Send("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isMuted: false);
                            }
                            await e.Send("Unmute successful");
                        } catch (Exception) {
                            await e.Send("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".deafen")
                    .Alias(".deaf")
                    .Description("Deafens mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.DeafenMembers) {
                            await e.Send("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isDeafened: true);
                            }
                            await e.Send("Deafen successful");
                        } catch (Exception) {
                            await e.Send("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".undeafen")
                    .Alias(".undeaf")
                    .Description("Undeafens mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e => {
                        if (!e.User.ServerPermissions.DeafenMembers) {
                            await e.Send("You do not have permission to do that.");
                            return;
                        }
                        if (e.Message.MentionedUsers.Count() == 0)
                            return;
                        try {
                            foreach (var u in e.Message.MentionedUsers) {
                                await u.Edit(isDeafened: false);
                            }
                            await e.Send("Undeafen successful");
                        } catch (Exception) {
                            await e.Send("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(".rvch")
                    .Description("Removes a voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Voice).FirstOrDefault()?.Delete();
                                await e.Send($"Removed channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch (Exception) {
                            await e.Send("No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".vch").Alias(".cvch")
                    .Description("Creates a new voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Voice);
                                await e.Send($"Created voice channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch (Exception) {
                            await e.Send("No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".rch").Alias(".rtch")
                    .Description("Removes a text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Text).FirstOrDefault()?.Delete();
                                await e.Send($"Removed text channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch (Exception) {
                            await e.Send("No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".ch").Alias(".tch")
                    .Description("Creates a new text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels) {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Text);
                                await e.Send($"Added text channel **{e.GetArg("channel_name")}**.");
                            }
                        } catch (Exception) {
                            await e.Send("No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".st").Alias(".settopic")
                    .Description("Sets a topic on the current channel.")
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e => {
                        try {
                            if (e.User.ServerPermissions.ManageChannels)
                                await e.Channel.Edit(topic: e.GetArg("topic"));
                        } catch (Exception) { }
                    });

                cgb.CreateCommand(".uid").Alias(".userid")
                    .Description("Shows user id")
                    .Parameter("user", ParameterType.Optional)
                    .Do(async e => {
                        var usr = e.User;
                        if (e.GetArg("user") != null) usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        await e.Send($"Id of the user { usr.Name } is { usr.Id }");
                    });

                cgb.CreateCommand(".cid").Alias(".channelid")
                    .Description("Shows current channel id")
                    .Do(async e => await e.Send("This channel's id is " + e.Channel.Id));

                cgb.CreateCommand(".sid").Alias(".serverid")
                    .Description("Shows current server id")
                    .Do(async e => await e.Send("This server's id is " + e.Server.Id));

                cgb.CreateCommand(".stats")
                    .Description("Shows some basic stats for nadeko")
                    .Do(async e => {
                        var t = Task.Run(() => {
                            return NadekoStats.Instance.GetStats() + "\n`" + Music.GetMusicStats() + "`";
                        });

                        await e.Send(await t);

                    });

                cgb.CreateCommand(".leaveall")
                    .Description("Nadeko leaves all servers **OWNER ONLY**")
                    .Do(e => {
                        if (e.User.Id == NadekoBot.OwnerID)
                            NadekoBot.client.Servers.ForEach(async s => { if (s.Name == e.Server.Name) return; await s.Leave(); });
                    });
                
                ConcurrentDictionary<Server, bool> pruneDict = new ConcurrentDictionary<Server, bool>();
                cgb.CreateCommand(".prune")
                    .Parameter("num", ParameterType.Required)
                    .Description("Prunes a number of messages from the current channel.\n**Usage**: .prune 5")
                    .Do(async e => {
                        if (!e.User.ServerPermissions.ManageMessages) return;
                        if (pruneDict.ContainsKey(e.Server))
                            return;
                        int num;
                        if (!Int32.TryParse(e.GetArg("num"), out num) || num < 1) {
                            await e.Send("Incorrect amount.");
                            return;
                        }
                        if (num > 10)
                            num = 10;
                        var msgs = await e.Channel.DownloadMessages(num);
                        foreach (var m in msgs) {
                            await m.Delete();
                            await Task.Delay(500);
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
                            await e.Send("`Shutting down.`");
                        }
                    });

                ConcurrentDictionary<Server, bool> clearDictionary = new ConcurrentDictionary<Server, bool>();
                cgb.CreateCommand(".clr")
                    .Description("Clears some of nadeko's messages from the current channel.")
                    .Do(async e => {
                        try {
                            if (clearDictionary.ContainsKey(e.Server))
                                return;
                            clearDictionary.TryAdd(e.Server, true);
                            var msgs = await e.Channel.DownloadMessages(100);
                            await Task.Run(async () => {
                                var ms = msgs.Where(msg => msg.User.Id == client.CurrentUser.Id);
                                foreach (var m in ms) {
                                    try { await m.Delete(); } catch (Exception) { }
                                    await Task.Delay(500);
                                }
                            });
                        } catch (Exception) { }
                        bool throwaway;
                        clearDictionary.TryRemove(e.Server, out throwaway);
                    });
                cgb.CreateCommand(".newname")
                    .Alias(".setname")
                    .Description("Give the bot a new name.")
                    .Parameter("new_name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID || e.GetArg("new_name") == null) return;

                        await client.CurrentUser.Edit(NadekoBot.password, e.GetArg("new_name"));
                    });
                /*
                cgb.CreateCommand(".newavatar")
                    .Alias(".setavatar")
                    .Description("Sets the new avatar from the image URL. PNG and JPEG supported")
                    .Parameter("new_avatar", ParameterType.Required)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID || e.GetArg("new_avatar") == null) return;
                        var arg = e.GetArg("new_avatar").Trim();
                        ImageType imgType = arg.EndsWith("png") ? ImageType.Png: ImageType.Jpeg;
                        var res = await Searches.GetResponseStream(e.GetArg("new_avatar"));
                        await client.CurrentUser.Edit(NadekoBot.password, avatar: res, avatarType: imgType);
                    });
                */
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
                            await e.Send("User for comms set.");
                        } else
                            await e.Send("No server specified or user.");
                    });

                cgb.CreateCommand(".commsserver")
                    .Description("Sets a server for through-bot communication.**Owner only**.")
                    .Parameter("server", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        commsServer = client.FindServers(e.GetArg("server")).FirstOrDefault();
                        if (commsServer != null)
                            await e.Send("Server for comms set.");
                        else
                            await e.Send("No such server.");
                    });

                cgb.CreateCommand(".commschannel")
                    .Description("Sets a channel for through-bot communication. Only works if server is set. Resets commsuser.**Owner only**.")
                    .Parameter("ch", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        commsChannel = commsServer?.FindChannels(e.GetArg("ch"), ChannelType.Text).FirstOrDefault();
                        if (commsChannel != null) {
                            commsUser = null;
                            await e.Send("Server for comms set.");
                        } else
                            await e.Send("No server specified or channel is invalid.");
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
                            await e.Send("Failed. Make sure you've specified server and [channel or user]");
                    });

                cgb.CreateCommand(".menrole")
                    .Alias(".mentionrole")
                    .Description("Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention @everyone permission.")
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

                /*cgb.CreateCommand(".voicetext")
                    .Description("Enabled or disabled voice to text channel connection. Only people in a certain voice channel will see ")
                
               cgb.CreateCommand(".jsontype")
                   .Do(async e => {
                       Newtonsoft.Json.Linq.JArray data = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText("data.json"));
                       if (data == null || data.Count == 0) return;

                       var wer = data.Where(jt => jt["Description"].ToString().Length > 120);
                       var list = wer.Select(jt => { 
                           var obj = new Parse.ParseObject("TypingArticles");
                           obj["text"] = jt["Description"].ToString();
                           return obj;
                       });
                       await Parse.ParseObject.SaveAllAsync(list);
                       await e.Send("saved to parse");

                   });

               cgb.CreateCommand(".repeat")
                   .Do(async e => {
                       if (e.User.Id != NadekoBot.OwnerID) return;

                       string[] notifs = { "Admin use .bye .greet", "Unstable - fixing", "fixing ~ani, ~mang", "join NadekoLog server", "-h is help, .stats",};
                       int i = notifs.Length;
                       while (true) {
                           await e.Channel.SendMessage($".setgame {notifs[--i]}");
                           await Task.Delay(20000);
                           if (i == 0) i = notifs.Length;
                       }
                   });
               */
            });
        }

        public void SaveParseToDb<T>(string where) where T : IDataModel {
            var data = File.ReadAllText(where);
            var arr = JObject.Parse(data)["results"] as JArray;
            var objects = new List<T>();
            foreach (JObject obj in arr) {
                objects.Add(obj.ToObject<T>());
            }
            Classes.DBHandler.Instance.InsertMany(objects);
        }
    }
}
