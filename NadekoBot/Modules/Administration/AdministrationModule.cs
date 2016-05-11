using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Commands;
using NadekoBot.Modules.Permissions.Classes;
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
            commands.Add(new InfoCommands(this));
            commands.Add(new CustomReactionsCommands(this));
            commands.Add(new AutoAssignRole(this));
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Administration;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "restart")
                    .Description("Restarts the bot. Might not work.")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("`Restarting in 2 seconds...`");
                        await Task.Delay(2000);
                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        Environment.Exit(0);
                    });

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
                            await e.Channel.SendMessage("You have insufficient permissions.").ConfigureAwait(false);
                        }

                        var usr = e.Server.FindUsers(userName).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid username").ConfigureAwait(false);
                            return;
                        }

                        var role = e.Server.FindRoles(roleName).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid role").ConfigureAwait(false);
                            return;
                        }

                        try
                        {
                            await usr.AddRoles(role).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Successfully added role **{role.Name}** to user **{usr.Name}**").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Failed to add roles. Bot has insufficient permissions.\n").ConfigureAwait(false);
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
                            await e.Channel.SendMessage("You failed to supply a valid username").ConfigureAwait(false);
                            return;
                        }

                        var role = e.Server.FindRoles(roleName).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid role").ConfigureAwait(false);
                            return;
                        }

                        try
                        {
                            await usr.RemoveRoles(role).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Successfully removed role **{role.Name}** from user **{usr.Name}**").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "renr")
                    .Alias(Prefix + "renamerole")
                    .Description($"Renames a role. Role you are renaming must be lower than bot's highest role.\n**Usage**: `{Prefix}renr \"First role\" SecondRole`")
                    .Parameter("r1", ParameterType.Required)
                    .Parameter("r2", ParameterType.Required)
                    .AddCheck(new SimpleCheckers.ManageRoles())
                    .Do(async e =>
                    {
                        var r1 = e.GetArg("r1").Trim();
                        var r2 = e.GetArg("r2").Trim();

                        var roleToEdit = e.Server.FindRoles(r1).FirstOrDefault();
                        if (roleToEdit == null)
                        {
                            await e.Channel.SendMessage("Can't find that role.");
                            return;
                        }

                        try
                        {
                            if (roleToEdit.Position > e.Server.CurrentUser.Roles.Max(r => r.Position))
                            {
                                await e.Channel.SendMessage("I can't edit roles higher than my highest role.");
                                return;
                            }
                            await roleToEdit.Edit(r2);
                            await e.Channel.SendMessage("Role renamed.");
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage("Failed to rename role. Probably insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(Prefix + "rar").Alias(Prefix + "removeallroles")
                    .Description("Removes all roles from a mentioned user.\n**Usage**: .rar @User")
                    .Parameter("user_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.CanManageRoles)
                    .Do(async e =>
                    {
                        var userName = e.GetArg("user_name");

                        var usr = e.Server.FindUsers(userName).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("You failed to supply a valid username").ConfigureAwait(false);
                            return;
                        }

                        try
                        {
                            await usr.RemoveRoles(usr.Roles.ToArray()).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Successfully removed **all** roles from user **{usr.Name}**").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "r").Alias(Prefix + "role").Alias(Prefix + "cr")
                    .Description("Creates a role with a given name.**Usage**: `.r Awesome Role`")
                    .Parameter("role_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.CanManageRoles)
                    .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("role_name")))
                            return;
                        try
                        {
                            var r = await e.Server.CreateRole(e.GetArg("role_name")).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Successfully created role **{r.Name}**.").ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage(":warning: Unspecified error.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "rolecolor").Alias(Prefix + "rc")
                    .Parameter("role_name", ParameterType.Required)
                    .Parameter("r", ParameterType.Optional)
                    .Parameter("g", ParameterType.Optional)
                    .Parameter("b", ParameterType.Optional)
                    .Description("Set a role's color to the hex or 0-255 rgb color value provided.\n**Usage**: `.color Admin 255 200 100` or `.color Admin ffba55`")
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageRoles)
                        {
                            await e.Channel.SendMessage("You don't have permission to use this!").ConfigureAwait(false);
                            return;
                        }

                        var args = e.Args.Where(s => s != string.Empty);

                        if (args.Count() != 2 && args.Count() != 4)
                        {
                            await e.Channel.SendMessage("The parameters are invalid.").ConfigureAwait(false);
                            return;
                        }

                        var role = e.Server.FindRoles(e.Args[0]).FirstOrDefault();

                        if (role == null)
                        {
                            await e.Channel.SendMessage("That role does not exist.").ConfigureAwait(false);
                            return;
                        }
                        try
                        {
                            var rgb = args.Count() == 4;
                            var arg1 = e.Args[1].Replace("#", "");

                            var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                            var green = Convert.ToByte(rgb ? int.Parse(e.Args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                            var blue = Convert.ToByte(rgb ? int.Parse(e.Args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));

                            await role.Edit(color: new Color(red, green, blue)).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Role {role.Name}'s color has been changed.").ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage("Error occured, most likely invalid parameters or insufficient permissions.").ConfigureAwait(false);
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

                          await e.Channel.SendMessage($"`List of roles for **{usr.Name}**:` \n• " + string.Join("\n• ", usr.Roles)).ConfigureAwait(false);
                          return;
                      }
                      await e.Channel.SendMessage("`List of roles:` \n• " + string.Join("\n• ", e.Server.Roles)).ConfigureAwait(false);
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
                                    await e.Channel.SendMessage("User not found.").ConfigureAwait(false);
                                    return;
                                }
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    await usr.SendMessage($"**You have been BANNED from `{e.Server.Name}` server.**\n" +
                                                          $"Reason: {msg}").ConfigureAwait(false);
                                    await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
                                }
                                try
                                {
                                    await e.Server.Ban(usr).ConfigureAwait(false);

                                    await e.Channel.SendMessage("Banned user " + usr.Name + " Id: " + usr.Id).ConfigureAwait(false);
                                }
                                catch
                                {
                                    await e.Channel.SendMessage("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
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
                                await e.Channel.SendMessage("User not found.").ConfigureAwait(false);
                                return;
                            }
                            if (!string.IsNullOrWhiteSpace(msg))
                            {
                                await usr.SendMessage($"**You have been KICKED from `{e.Server.Name}` server.**\n" +
                                                      $"Reason: {msg}").ConfigureAwait(false);
                                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
                            }
                            try
                            {
                                await usr.Kick().ConfigureAwait(false);
                                await e.Channel.SendMessage("Kicked user " + usr.Name + " Id: " + usr.Id).ConfigureAwait(false);
                            }
                            catch
                            {
                                await e.Channel.SendMessage("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
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
                            await e.Channel.SendMessage("You do not have permission to do that.").ConfigureAwait(false);
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isMuted: true).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage("Mute successful").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "unmute")
                    .Description("Unmutes mentioned user or users.")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.MuteMembers)
                        {
                            await e.Channel.SendMessage("You do not have permission to do that.").ConfigureAwait(false);
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isMuted: false).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage("Unmute successful").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.").ConfigureAwait(false);
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
                            await e.Channel.SendMessage("You do not have permission to do that.").ConfigureAwait(false);
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isDeafened: true).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage("Deafen successful").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.").ConfigureAwait(false);
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
                            await e.Channel.SendMessage("You do not have permission to do that.").ConfigureAwait(false);
                            return;
                        }
                        if (!e.Message.MentionedUsers.Any())
                            return;
                        try
                        {
                            foreach (var u in e.Message.MentionedUsers)
                            {
                                await u.Edit(isDeafened: false).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage("Undeafen successful").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("I do not have permission to do that most likely.").ConfigureAwait(false);
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
                                var ch = e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Voice).FirstOrDefault();
                                if (ch == null)
                                    return;
                                await ch.Delete().ConfigureAwait(false);
                                await e.Channel.SendMessage($"Removed channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
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
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Voice).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Created voice channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.").ConfigureAwait(false);
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
                                await channel.Delete().ConfigureAwait(false);
                                await e.Channel.SendMessage($"Removed text channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.").ConfigureAwait(false);
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
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Text).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Added text channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Insufficient permissions.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "st").Alias(Prefix + "settopic")
                    .Alias(Prefix + "topic")
                    .Description($"Sets a topic on the current channel.\n**Usage**: `{Prefix}st My new topic`")
                    .AddCheck(SimpleCheckers.ManageChannels())
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var topic = e.GetArg("topic")?.Trim() ?? "";
                        await e.Channel.Edit(topic: topic).ConfigureAwait(false);
                        await e.Channel.SendMessage(":ok: **New channel topic set.**").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "schn").Alias(Prefix + "setchannelname")
                    .Alias(Prefix + "topic")
                    .Description("Changed the name of the current channel.")
                    .AddCheck(SimpleCheckers.ManageChannels())
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var name = e.GetArg("name");
                        if (string.IsNullOrWhiteSpace(name))
                            return;
                        await e.Channel.Edit(name: name).ConfigureAwait(false);
                        await e.Channel.SendMessage(":ok: **New channel name set.**").ConfigureAwait(false);
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
                        await e.Channel.SendMessage($"Id of the user { usr.Name } is { usr.Id }").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "cid").Alias(Prefix + "channelid")
                    .Description("Shows current channel ID.")
                    .Do(async e => await e.Channel.SendMessage("This channel's ID is " + e.Channel.Id).ConfigureAwait(false));

                cgb.CreateCommand(Prefix + "sid").Alias(Prefix + "serverid")
                    .Description("Shows current server ID.")
                    .Do(async e => await e.Channel.SendMessage("This server's ID is " + e.Server.Id).ConfigureAwait(false));

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
                        await e.Channel.SendMessage((await NadekoStats.Instance.GetStats()).Matrix().TrimTo(1990)).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "heap")
                  .Description("Shows allocated memory - **Owner Only!**")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .Do(async e =>
                  {
                      var heap = await Task.Run(() => NadekoStats.Instance.Heap()).ConfigureAwait(false);
                      await e.Channel.SendMessage($"`Heap Size:` {heap}").ConfigureAwait(false);
                  });

                cgb.CreateCommand(Prefix + "prune")
                    .Alias(".clr")
                    .Description(
    "`.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel.\n**Usage**: `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X`")
                    .Parameter("user_or_num", ParameterType.Optional)
                    .Parameter("num", ParameterType.Optional)
                    .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("user_or_num"))) // if nothing is set, clear nadeko's messages, no permissions required
                        {
                            await Task.Run(async () =>
                            {
                                var msgs = (await e.Channel.DownloadMessages(100).ConfigureAwait(false)).Where(m => m.User.Id == e.Server.CurrentUser.Id);
                                foreach (var m in msgs)
                                {
                                    try
                                    {
                                        await m.Delete().ConfigureAwait(false);
                                    }
                                    catch { }
                                    await Task.Delay(100).ConfigureAwait(false);
                                }

                            }).ConfigureAwait(false);
                            return;
                        }
                        if (!e.User.GetPermissions(e.Channel).ManageMessages)
                            return;
                        else if (!e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await e.Channel.SendMessage("💢I don't have the permission to manage messages.");
                            return;
                        }
                        int val;
                        if (int.TryParse(e.GetArg("user_or_num"), out val)) // if num is set in the first argument, 
                                                                            //delete that number of messages.
                        {
                            if (val <= 0)
                                return;
                            val++;
                            foreach (var msg in await e.Channel.DownloadMessages(val).ConfigureAwait(false))
                            {
                                await msg.Delete().ConfigureAwait(false);
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                            return;
                        }
                        //else if first argument is user
                        var usr = e.Server.FindUsers(e.GetArg("user_or_num")).FirstOrDefault();
                        if (usr == null)
                            return;
                        val = 100;
                        if (!int.TryParse(e.GetArg("num"), out val))
                            val = 100;
                        await Task.Run(async () =>
                        {
                            var msgs = (await e.Channel.DownloadMessages(100).ConfigureAwait(false)).Where(m => m.User.Id == usr.Id).Take(val);
                            foreach (var m in msgs)
                            {
                                try
                                {
                                    await m.Delete().ConfigureAwait(false);
                                }
                                catch { }
                                await Task.Delay(100).ConfigureAwait(false);
                            }

                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "die")
                    .Alias(Prefix + "graceful")
                    .Description("Shuts the bot down and notifies users about the restart. **Owner Only!**")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("`Shutting down.`").ConfigureAwait(false);
                        await Task.Delay(2000).ConfigureAwait(false);
                        Environment.Exit(0);
                    });

                //cgb.CreateCommand(Prefix + "newnick")
                //    .Alias(Prefix + "setnick")
                //    .Description("Give the bot a new nickname. You need manage server permissions.")
                //    .Parameter("new_nick", ParameterType.Unparsed)
                //    .AddCheck(SimpleCheckers.ManageServer())
                //    .Do(async e =>
                //    {
                //        if (e.GetArg("new_nick") == null) return;

                //        await client.CurrentUser.Edit(NadekoBot.Creds.Password, e.GetArg("new_nick")).ConfigureAwait(false);
                //    });

                cgb.CreateCommand(Prefix + "newname")
                    .Alias(Prefix + "setname")
                    .Description("Give the bot a new name. **Owner Only!**")
                    .Parameter("new_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        if (e.GetArg("new_name") == null) return;

                        await client.CurrentUser.Edit(NadekoBot.Creds.Password, e.GetArg("new_name")).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "newavatar")
                    .Alias(Prefix + "setavatar")
                    .Description("Sets a new avatar image for the NadekoBot. **Owner Only!**")
                    .Parameter("img", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("img")))
                            return;
                        // Gather user provided URL.
                        var avatarAddress = e.GetArg("img");
                        var imageStream = await SearchHelper.GetResponseStreamAsync(avatarAddress).ConfigureAwait(false);
                        var image = System.Drawing.Image.FromStream(imageStream);
                        // Save the image to disk.
                        image.Save("data/avatar.png", System.Drawing.Imaging.ImageFormat.Png);
                        await client.CurrentUser.Edit(NadekoBot.Creds.Password, avatar: image.ToStream()).ConfigureAwait(false);
                        // Send confirm.
                        await e.Channel.SendMessage("New avatar set.").ConfigureAwait(false);
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
                        await e.User.SendMessage(output).ConfigureAwait(false);
                    });

                Server commsServer = null;
                User commsUser = null;
                Channel commsChannel = null;

                cgb.CreateCommand(Prefix + "commsuser")
                            .Description("Sets a user for through-bot communication. Only works if server is set. Resets commschannel. **Owner Only!**")
                            .Parameter("name", ParameterType.Unparsed)
                            .AddCheck(SimpleCheckers.OwnerOnly())
                            .Do(async e =>
                            {
                                commsUser = commsServer?.FindUsers(e.GetArg("name")).FirstOrDefault();
                                if (commsUser != null)
                                {
                                    commsChannel = null;
                                    await e.Channel.SendMessage("User for comms set.").ConfigureAwait(false);
                                }
                                else
                                    await e.Channel.SendMessage("No server specified or user.").ConfigureAwait(false);
                            });

                cgb.CreateCommand(Prefix + "commsserver")
                    .Description("Sets a server for through-bot communication. **Owner Only!**")
                    .Parameter("server", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        commsServer = client.FindServers(e.GetArg("server")).FirstOrDefault();
                        if (commsServer != null)
                            await e.Channel.SendMessage("Server for comms set.").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("No such server.").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "commschannel")
                    .Description("Sets a channel for through-bot communication. Only works if server is set. Resets commsuser. **Owner Only!**")
                    .Parameter("ch", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        commsChannel = commsServer?.FindChannels(e.GetArg("ch"), ChannelType.Text).FirstOrDefault();
                        if (commsChannel != null)
                        {
                            commsUser = null;
                            await e.Channel.SendMessage("Server for comms set.").ConfigureAwait(false);
                        }
                        else
                            await e.Channel.SendMessage("No server specified or channel is invalid.").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "send")
                    .Description("Send a message to someone on a different server through the bot. **Owner Only!**\n **Usage**: .send Message text multi word!")
                    .Parameter("msg", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        if (commsUser != null)
                            await commsUser.SendMessage(e.GetArg("msg")).ConfigureAwait(false);
                        else if (commsChannel != null)
                            await commsChannel.SendMessage(e.GetArg("msg")).ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("Failed. Make sure you've specified server and [channel or user]").ConfigureAwait(false);
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
                                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                                       send.Substring(2000);
                            }
                            await e.Channel.Send(send).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "parsetosql")
                  .Description("Loads exported parsedata from /data/parsedata/ into sqlite database.")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .Do(async e =>
                  {
                      await Task.Run(() =>
                      {
                          SaveParseToDb<Announcement>("data/parsedata/Announcements.json");
                          SaveParseToDb<DataModels.Command>("data/parsedata/CommandsRan.json");
                          SaveParseToDb<Request>("data/parsedata/Requests.json");
                          SaveParseToDb<Stats>("data/parsedata/Stats.json");
                          SaveParseToDb<TypingArticle>("data/parsedata/TypingArticles.json");
                      }).ConfigureAwait(false);
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

                            await e.Channel.SendMessage(str + string.Join("⭐", donatorsOrdered.Select(d => d.UserName))).ConfigureAwait(false);
                        }).ConfigureAwait(false);
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
                            var donator = e.Server.FindUsers(e.GetArg("donator")).FirstOrDefault();
                            var amount = int.Parse(e.GetArg("amount"));
                            if (donator == null) return;
                            try
                            {
                                DbHandler.Instance.InsertData(new Donator
                                {
                                    Amount = amount,
                                    UserName = donator.Name,
                                    UserId = (long)donator.Id
                                });
                                e.Channel.SendMessage("Successfuly added a new donator. 👑");
                            }
                            catch { }
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "videocall")
                  .Description("Creates a private <http://www.appear.in> video call link for you and other mentioned people. The link is sent to mentioned people via a private message.")
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
                              await usr.SendMessage(str).ConfigureAwait(false);
                          }
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine(ex);
                      }
                  });

                cgb.CreateCommand(Prefix + "announce")
                    .Description($"Sends a message to all servers' general channel bot is connected to.**Owner Only!**\n**Usage**: {Prefix}announce Useless spam")
                    .Parameter("msg", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        foreach (var ch in NadekoBot.Client.Servers.Select(s => s.DefaultChannel))
                        {
                            await ch.SendMessage(e.GetArg("msg"));
                        }

                        await e.Channel.SendMessage(":ok:");
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
