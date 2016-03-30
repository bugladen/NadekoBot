using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using NadekoBot.Classes.Permissions;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    internal class AdministrationModule : DiscordModule
    {
        public AdministrationModule()
        {
            commands.Add(new ServerGreetCommand(this));
            commands.Add(new LogCommand(this));
            commands.Add(new MessageRepeater(this));
            commands.Add(new PlayingRotate(this));
            commands.Add(new RatelimitCommand(this));
            commands.Add(new VoicePlusTextCommand(this));
            commands.Add(new CrossServerTextChannel(this));
            commands.Add(new SelfAssignedRolesCommand(this));
            commands.Add(new Remind(this));
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Administration;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "sr").Alias(Prefix + "setrole")
                    .Description("Sets a role for a given user.\n**Usage**: .sr @User Guest")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.CanManageRoles)
                    .Do(async e =>
                    {
                        var userName = e.GetArg("user_name");
                        var roleName = e.GetArg("role_name");

                        if (string.IsNullOrWhiteSpace(roleName)) return;

                        if (!e.User.ServerPermissions.ManageRoles)
                        {
                            await e.Channel.SendMessage("You have insufficient permissions.");
                        }

                        var usr = e.Server.FindUsers(userName).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid username");
                            return;
                        }

                        var role = e.Server.FindRoles(roleName).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid role");
                            return;
                        }

                        try
                        {
                            await usr.AddRoles(role);
                            await e.Channel.SendMessage($"Successfully added role **{role.Name}** to user **{usr.Name}**");
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Failed to add roles. Bot has insufficient permissions.\n");
                            Console.WriteLine(ex.ToString());
                        }
                    });

                cgb.CreateCommand(Prefix + "rr").Alias(Prefix + "removerole")
                    .Description("Removes a role from a given user.\n**Usage**: .rr @User Admin")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.CanManageRoles)
                    .Do(async e =>
                    {
                        var userName = e.GetArg("user_name");
                        var roleName = e.GetArg("role_name");

                        if (string.IsNullOrWhiteSpace(roleName)) return;

                        var usr = e.Server.FindUsers(userName).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid username");
                            return;
                        }

                        var role = e.Server.FindRoles(roleName).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid role");
                            return;
                        }

                        try
                        {
                            await usr.RemoveRoles(role);
                            await e.Channel.SendMessage($"Successfully removed role **{role.Name}** from user **{usr.Name}**");
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Failed to remove roles. Most likely reason: Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "r").Alias(Prefix + "role").Alias(Prefix + "cr")
                    .Description("Creates a role with a given name.**Usage**: .r Awesome Role")
                    .Parameter("role_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.CanManageRoles)
                    .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("role_name")))
                            return;
                        try
                        {
                            var r = await e.Server.CreateRole(e.GetArg("role_name"));
                            await e.Channel.SendMessage($"Successfully created role **{r.Name}**.");
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage(":warning: Unspecified error.");
                        }
                    });

                cgb.CreateCommand(Prefix + "rolecolor").Alias(Prefix + "rc")
                    .Parameter("role_name", ParameterType.Required)
                    .Parameter("r", ParameterType.Optional)
                    .Parameter("g", ParameterType.Optional)
                    .Parameter("b", ParameterType.Optional)
                    .Description("Set a role's color to the hex or 0-255 rgb color value provided.\n**Usage**: .color Admin 255 200 100 or .color Admin ffba55")
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageRoles)
                        {
                            await e.Channel.SendMessage("You don't have permission to use this!");
                            return;
                        }

                        var args = e.Args.Where(s => s != string.Empty);

                        if (args.Count() != 2 && args.Count() != 4)
                        {
                            await e.Channel.SendMessage("The parameters are invalid.");
                            return;
                        }

                        var role = e.Server.FindRoles(e.Args[0]).FirstOrDefault();

                        if (role == null)
                        {
                            await e.Channel.SendMessage("That role does not exist.");
                            return;
                        }
                        try
                        {
                            var rgb = args.Count() == 4;

                            var red = Convert.ToByte(rgb ? int.Parse(e.Args[1]) : Convert.ToInt32(e.Args[1].Substring(0, 2), 16));
                            var green = Convert.ToByte(rgb ? int.Parse(e.Args[2]) : Convert.ToInt32(e.Args[1].Substring(2, 2), 16));
                            var blue = Convert.ToByte(rgb ? int.Parse(e.Args[3]) : Convert.ToInt32(e.Args[1].Substring(4, 2), 16));

                            await role.Edit(color: new Color(red, green, blue));
                            await e.Channel.SendMessage($"Role {role.Name}'s color has been changed.");
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Error occured, most likely invalid parameters or insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "roles")
                  .Description("List all roles on this server or a single user if specified.")
                  .Parameter("user", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
                      {
                          var usr = e.Server.FindUsers(e.GetArg("user")).FirstOrDefault();
                          if (usr == null) return;

                          await e.Channel.SendMessage($"`List of roles for **{usr.Name}**:` \n• " + string.Join("\n• ", usr.Roles));
                          return;
                      }
                      await e.Channel.SendMessage("`List of roles:` \n• " + string.Join("\n• ", e.Server.Roles));
                  });

                cgb.CreateCommand(Prefix + "b").Alias(Prefix + "ban")
                    .Parameter("user", ParameterType.Required)
                    .Parameter("msg", ParameterType.Optional)
                    .Description("Bans a user by id or name with an optional message.\n**Usage**: .b \"@some Guy\" Your behaviour is toxic.")
                        .Do(async e =>
                        {
                            var msg = e.GetArg("msg");
                            var user = e.GetArg("user");
                            if (e.User.ServerPermissions.BanMembers)
                            {
                                var usr = e.Server.FindUsers(user).FirstOrDefault();
                                if (usr == null)
                                {
                                    await e.Channel.SendMessage("User not found.");
                                    return;
                                }
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    await usr.SendMessage($"**You have been BANNED from `{e.Server.Name}` server.**\n" +
                                                          $"Reason: {msg}");
                                    await Task.Delay(2000); // temp solution; give time for a message to be send, fu volt
                                }
                                try
                                {
                                    await e.Server.Ban(usr);

                                    await e.Channel.SendMessage("Banned user " + usr.Name + " Id: " + usr.Id);
                                }
                                catch
                                {
                                    await e.Channel.SendMessage("Error. Most likely I don't have sufficient permissions.");
                                }
                            }
                        });

                cgb.CreateCommand(Prefix + "k").Alias(Prefix + "kick")
                    .Parameter("user")
                    .Parameter("msg", ParameterType.Unparsed)
                    .Description("Kicks a mentioned user.")
                    .Do(async e =>
                    {
                        var msg = e.GetArg("msg");
                        var user = e.GetArg("user");
                        if (e.User.ServerPermissions.KickMembers)
                        {
                            var usr = e.Server.FindUsers(user).FirstOrDefault();
                            if (usr == null)
                            {
                                await e.Channel.SendMessage("User not found.");
                                return;
                            }
                            if (!string.IsNullOrWhiteSpace(msg))
                            {
                                await usr.SendMessage($"**You have been KICKED from `{e.Server.Name}` server.**\n" +
                                                      $"Reason: {msg}");
                                await Task.Delay(2000); // temp solution; give time for a message to be send, fu volt
                            }
                            try
                            {
                                await usr.Kick();
                                await e.Channel.SendMessage("Kicked user " + usr.Name + " Id: " + usr.Id);
                            }
                            catch
                            {
                                await e.Channel.SendMessage("Error. Most likely I don't have sufficient permissions.");
                            }
                        }
                    });
                cgb.CreateCommand(Prefix + "mute")
                    .Description("Mutes mentioned user or users.")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.MuteMembers)
                        {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isMuted: true);
                            }
                            await e.Channel.SendMessage("Mute successful");
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(Prefix + "unmute")
                    .Description("Unmutes mentioned user or users.")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.MuteMembers)
                        {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isMuted: false);
                            }
                            await e.Channel.SendMessage("Unmute successful");
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(Prefix + "deafen")
                    .Alias(Prefix + "deaf")
                    .Description("Deafens mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.DeafenMembers)
                        {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isDeafened: true);
                            }
                            await e.Channel.SendMessage("Deafen successful");
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(Prefix + "undeafen")
                    .Alias(Prefix + "undeaf")
                    .Description("Undeafens mentioned user or users")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.DeafenMembers)
                        {
                            await e.Channel.SendMessage("You do not have permission to do that.");
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isDeafened: false);
                            }
                            await e.Channel.SendMessage("Undeafen successful");
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.");
                        }
                    });

                cgb.CreateCommand(Prefix + "rvch")
                    .Description("Removes a voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Voice).FirstOrDefault()?.Delete();
                                await e.Channel.SendMessage($"Removed channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "vch").Alias(Prefix + "cvch")
                    .Description("Creates a new voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Voice);
                                await e.Channel.SendMessage($"Created voice channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "rch").Alias(Prefix + "rtch")
                    .Description("Removes a text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                var channel = e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Text).FirstOrDefault();
                                if (channel == null) return;
                                await channel.Delete();
                                await e.Channel.SendMessage($"Removed text channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "ch").Alias(Prefix + "tch")
                    .Description("Creates a new text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Text);
                                await e.Channel.SendMessage($"Added text channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "st").Alias(Prefix + "settopic")
                    .Alias(Prefix + "topic")
                    .Description("Sets a topic on the current channel.")
                    .AddCheck(SimpleCheckers.ManageChannels())
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var topic = e.GetArg("topic");
                        if (string.IsNullOrWhiteSpace(topic))
                            return;
                        await e.Channel.Edit(topic: topic);
                        await e.Channel.SendMessage(":ok: **New channel topic set.**");
                    });

                cgb.CreateCommand(Prefix + "uid").Alias(Prefix + "userid")
                    .Description("Shows user ID.")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usr = e.User;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user"))) usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        if (usr == null)
                            return;
                        await e.Channel.SendMessage($"Id of the user { usr.Name } is { usr.Id }");
                    });

                cgb.CreateCommand(Prefix + "cid").Alias(Prefix + "channelid")
                    .Description("Shows current channel ID.")
                    .Do(async e => await e.Channel.SendMessage("This channel's ID is " + e.Channel.Id));

                cgb.CreateCommand(Prefix + "sid").Alias(Prefix + "serverid")
                    .Description("Shows current server ID.")
                    .Do(async e => await e.Channel.SendMessage("This server's ID is " + e.Server.Id));

                cgb.CreateCommand(Prefix + "stats")
                    .Description("Shows some basic stats for Nadeko.")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(await NadekoStats.Instance.GetStats());
                    });

                cgb.CreateCommand(Prefix + "dysyd")
                    .Description("Shows some basic stats for Nadeko.")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage((await NadekoStats.Instance.GetStats()).Matrix().TrimTo(1990));
                    });

                cgb.CreateCommand(Prefix + "heap")
                  .Description("Shows allocated memory - **Owner Only!**")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .Do(async e =>
                  {
                      var heap = await Task.Run(() => NadekoStats.Instance.Heap());
                      await e.Channel.SendMessage($"`Heap Size:` {heap}");
                  });
                cgb.CreateCommand(Prefix + "prune")
                    .Parameter("num", ParameterType.Required)
                    .Description("Prunes a number of messages from the current channel.\n**Usage**: .prune 5")
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageMessages) return;
                        int val;
                        if (string.IsNullOrWhiteSpace(e.GetArg("num")) || !int.TryParse(e.GetArg("num"), out val) || val < 0)
                            return;

                        foreach (var msg in await e.Channel.DownloadMessages(val))
                        {
                            await msg.Delete();
                            await Task.Delay(100);
                        }
                    });

                cgb.CreateCommand(Prefix + "die")
                    .Alias(Prefix + "graceful")
                    .Description("Shuts the bot down and notifies users about the restart. **Owner Only!**")
                    .Do(async e =>
                    {
                        if (NadekoBot.IsOwner(e.User.Id))
                        {
                            await e.Channel.SendMessage("`Shutting down.`");
                            await Task.Delay(2000);
                            Environment.Exit(0);
                        }
                    });

                cgb.CreateCommand(Prefix + "clr")
                    .Description("Clears some of Nadeko's messages from the current channel. If given a user, will clear the user's messages from the current channel (**Owner Only!**) \n**Usage**: .clr @X")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usrId = NadekoBot.Client.CurrentUser.Id;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user")) && e.User.ServerPermissions.ManageMessages)
                        {
                            var usr = e.Server.FindUsers(e.GetArg("user")).FirstOrDefault();
                            if (usr != null)
                                usrId = usr.Id;
                        }
                        await Task.Run(async () =>
                        {
                            var msgs = (await e.Channel.DownloadMessages(100)).Where(m => m.User.Id == usrId);
                            foreach (var m in msgs)
                            {
                                try
                                {
                                    await m.Delete();
                                }
                                catch { }
                                await Task.Delay(200);
                            }

                        });
                    });

                cgb.CreateCommand(Prefix + "newname")
                    .Alias(Prefix + "setname")
                    .Description("Give the bot a new name. **Owner Only!**")
                    .Parameter("new_name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id) || e.GetArg("new_name") == null) return;

                        await client.CurrentUser.Edit(NadekoBot.Creds.Password, e.GetArg("new_name"));
                    });

                cgb.CreateCommand(Prefix + "newavatar")
                    .Alias(Prefix + "setavatar")
                    .Description("Sets a new avatar image for the NadekoBot. **Owner Only!**")
                    .Parameter("img", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id) || string.IsNullOrWhiteSpace(e.GetArg("img")))
                            return;
                        // Gather user provided URL.
                        var avatarAddress = e.GetArg("img");
                        var imageStream = await SearchHelper.GetResponseStreamAsync(avatarAddress);
                        var image = System.Drawing.Image.FromStream(imageStream);
                        // Save the image to disk.
                        image.Save("data/avatar.png", System.Drawing.Imaging.ImageFormat.Png);
                        await client.CurrentUser.Edit(NadekoBot.Creds.Password, avatar: image.ToStream());
                        // Send confirm.
                        await e.Channel.SendMessage("New avatar set.");
                    });

                cgb.CreateCommand(Prefix + "setgame")
                  .Description("Sets the bots game. **Owner Only!**")
                  .Parameter("set_game", ParameterType.Unparsed)
                  .Do(e =>
                  {
                      if (!NadekoBot.IsOwner(e.User.Id) || e.GetArg("set_game") == null) return;

                      client.SetGame(e.GetArg("set_game"));
                  });

                cgb.CreateCommand(Prefix + "checkmyperms")
                    .Description("Checks your userspecific permissions on this channel.")
                    .Do(async e =>
                    {
                        var output = "```\n";
                        foreach (var p in e.User.ServerPermissions.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
                        {
                            output += p.Name + ": " + p.GetValue(e.User.ServerPermissions, null).ToString() + "\n";
                        }
                        output += "```";
                        await e.User.SendMessage(output);
                    });

                Server commsServer = null;
                User commsUser = null;
                Channel commsChannel = null;

                cgb.CreateCommand(Prefix + "commsuser")
                            .Description("Sets a user for through-bot communication. Only works if server is set. Resets commschannel. **Owner Only!**")
                            .Parameter("name", ParameterType.Unparsed)
                            .Do(async e =>
                            {
                                if (!NadekoBot.IsOwner(e.User.Id)) return;
                                commsUser = commsServer?.FindUsers(e.GetArg("name")).FirstOrDefault();
                                if (commsUser != null)
                                {
                                    commsChannel = null;
                                    await e.Channel.SendMessage("User for comms set.");
                                }
                                else
                                    await e.Channel.SendMessage("No server specified or user.");
                            });

                cgb.CreateCommand(Prefix + "commsserver")
                    .Description("Sets a server for through-bot communication. **Owner Only!**")
                    .Parameter("server", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        commsServer = client.FindServers(e.GetArg("server")).FirstOrDefault();
                        if (commsServer != null)
                            await e.Channel.SendMessage("Server for comms set.");
                        else
                            await e.Channel.SendMessage("No such server.");
                    });

                cgb.CreateCommand(Prefix + "commschannel")
                    .Description("Sets a channel for through-bot communication. Only works if server is set. Resets commsuser. **Owner Only!**")
                    .Parameter("ch", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        commsChannel = commsServer?.FindChannels(e.GetArg("ch"), ChannelType.Text).FirstOrDefault();
                        if (commsChannel != null)
                        {
                            commsUser = null;
                            await e.Channel.SendMessage("Server for comms set.");
                        }
                        else
                            await e.Channel.SendMessage("No server specified or channel is invalid.");
                    });

                cgb.CreateCommand(Prefix + "send")
                    .Description("Send a message to someone on a different server through the bot. **Owner Only!**\n **Usage**: .send Message text multi word!")
                    .Parameter("msg", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (commsUser != null)
                            await commsUser.SendMessage(e.GetArg("msg"));
                        else if (commsChannel != null)
                            await commsChannel.SendMessage(e.GetArg("msg"));
                        else
                            await e.Channel.SendMessage("Failed. Make sure you've specified server and [channel or user]");
                    });

                cgb.CreateCommand(Prefix + "menrole")
                    .Alias(Prefix + "mentionrole")
                    .Description("Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission.")
                    .Parameter("roles", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.User.ServerPermissions.MentionEveryone) return;
                            var arg = e.GetArg("roles").Split(',').Select(r => r.Trim());
                            string send = $"--{e.User.Mention} has invoked a mention on the following roles--";
                            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str)))
                            {
                                var role = e.Server.FindRoles(roleStr).FirstOrDefault();
                                if (role == null) continue;
                                send += $"\n`{role.Name}`\n";
                                send += string.Join(", ", role.Members.Select(r => r.Mention));
                            }

                            while (send.Length > 2000)
                            {
                                var curstr = send.Substring(0, 2000);
                                await
                                    e.Channel.Send(curstr.Substring(0,
                                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1));
                                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                                       send.Substring(2000);
                            }
                            await e.Channel.Send(send);
                        });
                    });

                cgb.CreateCommand(Prefix + "parsetosql")
                  .Description("Loads exported parsedata from /data/parsedata/ into sqlite database.")
                  .Do(async e =>
                  {
                      if (!NadekoBot.IsOwner(e.User.Id))
                          return;
                      await Task.Run(() =>
                      {
                          SaveParseToDb<Announcement>("data/parsedata/Announcements.json");
                          SaveParseToDb<Classes._DataModels.Command>("data/parsedata/CommandsRan.json");
                          SaveParseToDb<Request>("data/parsedata/Requests.json");
                          SaveParseToDb<Stats>("data/parsedata/Stats.json");
                          SaveParseToDb<TypingArticle>("data/parsedata/TypingArticles.json");
                      });
                  });

                cgb.CreateCommand(Prefix + "unstuck")
                  .Description("Clears the message queue. **Owner Only!**")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .Do(e =>
                  {
                      NadekoBot.Client.MessageQueue.Clear();
                  });

                cgb.CreateCommand(Prefix + "donators")
                    .Description("List of lovely people who donated to keep this project alive.")
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            var rows = DbHandler.Instance.GetAllRows<Donator>();
                            var donatorsOrdered = rows.OrderByDescending(d => d.Amount);
                            string str = $"**Thanks to the people listed below for making this project happen!**\n";

                            await e.Channel.SendMessage(str + string.Join("⭐", donatorsOrdered.Select(d => d.UserName)));
                        });
                    });

                //THIS IS INTENTED TO BE USED ONLY BY THE ORIGINAL BOT OWNER
                cgb.CreateCommand(Prefix + "adddon")
                    .Alias(Prefix + "donadd")
                    .Description("Add a donator to the database.")
                    .Parameter("donator")
                    .Parameter("amount")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await Task.Run(() =>
                        {
                            if (!NadekoBot.IsOwner(e.User.Id))
                                return;
                            var donator = e.Server.FindUsers(e.GetArg("donator")).FirstOrDefault();
                            var amount = int.Parse(e.GetArg("amount"));
                            if (donator == null) return;
                            try
                            {
                                DbHandler.Instance.InsertData(new Donator
                                {
                                    Amount = amount,
                                    UserName = donator.Name,
                                    UserId = (long)e.User.Id
                                });
                                e.Channel.SendMessage("Successfuly added a new donator. 👑");
                            }
                            catch { }
                        });
                    });

                cgb.CreateCommand(Prefix + "videocall")
                  .Description("Creates a private appear.in video call link for you and other mentioned people. The link is sent to mentioned people via a private message.")
                  .Parameter("arg", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      try
                      {
                          var allUsrs = e.Message.MentionedUsers.Union(new User[] { e.User });
                          var allUsrsArray = allUsrs as User[] ?? allUsrs.ToArray();
                          var str = allUsrsArray.Aggregate("http://appear.in/", (current, usr) => current + Uri.EscapeUriString(usr.Name[0].ToString()));
                          str += new Random().Next();
                          foreach (var usr in allUsrsArray)
                          {
                              await usr.SendMessage(str);
                          }
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine(ex);
                      }
                  });
            });
        }

        public void SaveParseToDb<T>(string where) where T : IDataModel
        {
            try
            {
                var data = File.ReadAllText(where);
                var arr = JObject.Parse(data)["results"] as JArray;
                if (arr == null)
                    return;
                var objects = arr.Select(x => x.ToObject<T>());
                DbHandler.Instance.InsertMany(objects);
            }
            catch { }
        }
    }
}
