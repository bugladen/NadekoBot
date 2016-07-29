using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Commands;
using NadekoBot.Modules.Permissions.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            commands.Add(new CustomReactionsCommands(this));
            commands.Add(new AutoAssignRole(this));
            commands.Add(new SelfCommands(this));
            commands.Add(new IncidentsCommands(this));

            NadekoBot.Client.GetService<CommandService>().CommandExecuted += DeleteCommandMessage;
        }

        private void DeleteCommandMessage(object sender, CommandEventArgs e)
        {
            if (e.Server == null || e.Channel.IsPrivate)
                return;
            var conf = SpecificConfigurations.Default.Of(e.Server.Id);
            if (!conf.AutoDeleteMessagesOnCommand)
                return;
            try
            {
                e.Message.Delete();
            }
            catch { }
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Administration;

        public override void Install(ModuleManager manager)
        {

            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "delmsgoncmd")
                    .Description($"Toggles the automatic deletion of user's successful command message to prevent chat flood. Server Manager Only. | `{Prefix}delmsgoncmd`")
                    .AddCheck(SimpleCheckers.ManageServer())
                    .Do(async e =>
                    {
                        var conf = SpecificConfigurations.Default.Of(e.Server.Id);
                        conf.AutoDeleteMessagesOnCommand = !conf.AutoDeleteMessagesOnCommand;
                        await Classes.JSONModels.ConfigHandler.SaveConfig().ConfigureAwait(false);
                        if (conf.AutoDeleteMessagesOnCommand)
                            await e.Channel.SendMessage("❗`Now automatically deleting successfull command invokations.`");
                        else
                            await e.Channel.SendMessage("❗`Stopped automatic deletion of successfull command invokations.`");

                    });

                cgb.CreateCommand(Prefix + "restart")
                    .Description($"Restarts the bot. Might not work. **Bot Owner Only** | `{Prefix}restart`")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("`Restarting in 2 seconds...`");
                        await Task.Delay(2000);
                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        Environment.Exit(0);
                    });

                cgb.CreateCommand(Prefix + "setrole").Alias(Prefix + "sr")
                    .Description($"Sets a role for a given user. | `{Prefix}sr @User Guest`")
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

                cgb.CreateCommand(Prefix + "removerole").Alias(Prefix + "rr")
                    .Description($"Removes a role from a given user. | `{Prefix}rr @User Admin`")
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

                cgb.CreateCommand(Prefix + "renamerole")
                    .Alias(Prefix + "renr")
                    .Description($"Renames a role. Role you are renaming must be lower than bot's highest role. | `{Prefix}renr \"First role\" SecondRole`")
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
                            await e.Channel.SendMessage("Can't find that role.").ConfigureAwait(false);
                            return;
                        }

                        try
                        {
                            if (roleToEdit.Position > e.Server.CurrentUser.Roles.Max(r => r.Position))
                            {
                                await e.Channel.SendMessage("I can't edit roles higher than my highest role.").ConfigureAwait(false);
                                return;
                            }
                            await roleToEdit.Edit(r2);
                            await e.Channel.SendMessage("Role renamed.").ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage("Failed to rename role. Probably insufficient permissions.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "removeallroles").Alias(Prefix + "rar")
                    .Description($"Removes all roles from a mentioned user. | `{Prefix}rar @User`")
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

                cgb.CreateCommand(Prefix + "createrole").Alias(Prefix + "cr")
                    .Description($"Creates a role with a given name. | `{Prefix}cr Awesome Role`")
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
                    .Description($"Set a role's color to the hex or 0-255 rgb color value provided. | `{Prefix}rc Admin 255 200 100` or `{Prefix}rc Admin ffba55`")
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

                cgb.CreateCommand(Prefix + "ban").Alias(Prefix + "b")
                    .Parameter("user", ParameterType.Required)
                    .Parameter("msg", ParameterType.Unparsed)
                    .Description($"Bans a user by id or name with an optional message. | `{Prefix}b \"@some Guy\" Your behaviour is toxic.`")
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
                                    await e.Server.Ban(usr, 7).ConfigureAwait(false);

                                    await e.Channel.SendMessage("Banned user " + usr.Name + " Id: " + usr.Id).ConfigureAwait(false);
                                }
                                catch
                                {
                                    await e.Channel.SendMessage("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
                                }
                            }
                        });

                cgb.CreateCommand(Prefix + "softban").Alias(Prefix + "sb")
                    .Parameter("user", ParameterType.Required)
                    .Parameter("msg", ParameterType.Unparsed)
                    .Description($"Bans and then unbans a user by id or name with an optional message. | `{Prefix}sb \"@some Guy\" Your behaviour is toxic.`")
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
                                    await usr.SendMessage($"**You have been SOFT-BANNED from `{e.Server.Name}` server.**\n" +
                                                          $"Reason: {msg}").ConfigureAwait(false);
                                    await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
                                }
                                try
                                {
                                    await e.Server.Ban(usr, 7).ConfigureAwait(false);
                                    await e.Server.Unban(usr).ConfigureAwait(false);

                                    await e.Channel.SendMessage("Soft-Banned user " + usr.Name + " Id: " + usr.Id).ConfigureAwait(false);
                                }
                                catch
                                {
                                    await e.Channel.SendMessage("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
                                }
                            }
                        });

                cgb.CreateCommand(Prefix + "kick").Alias(Prefix + "k")
                    .Parameter("user")
                    .Parameter("msg", ParameterType.Unparsed)
                    .Description($"Kicks a mentioned user. | `{Prefix}k \"@some Guy\" Your behaviour is toxic.`")
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
                    .Description($"Mutes mentioned user or users. | `{Prefix}mute \"@Someguy\"` or `{Prefix}mute \"@Someguy\" \"@Someguy\"`")
                    .Parameter("throwaway", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.MuteMembers)
                        {
                            await e.Channel.SendMessage("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
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
                            await e.Channel.SendMessage("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "unmute")
                    .Description($"Unmutes mentioned user or users. | `{Prefix}unmute \"@Someguy\"` or `{Prefix}unmute \"@Someguy\" \"@Someguy\"`")
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
                            await e.Channel.SendMessage("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "deafen")
                    .Alias(Prefix + "deaf")
                    .Description($"Deafens mentioned user or users | `{Prefix}deaf \"@Someguy\"` or `{Prefix}deaf \"@Someguy\" \"@Someguy\"`")
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
                            await e.Channel.SendMessage("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "undeafen")
                    .Alias(Prefix + "undef")
                    .Description($"Undeafens mentioned user or users | `{Prefix}undef \"@Someguy\"` or `{Prefix}undef \"@Someguy\" \"@Someguy\"`")
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
                            await e.Channel.SendMessage("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "delvoichanl")
                    .Alias(Prefix + "dvch")
                    .Description($"Deletes a voice channel with a given name. | `{Prefix}dvch VoiceChannelName`")
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

                cgb.CreateCommand(Prefix + "creatvoichanl")
                    .Alias(Prefix + "cvch")
                    .Description($"Creates a new voice channel with a given name. | `{Prefix}cvch VoiceChannelName`")
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

                cgb.CreateCommand(Prefix + "deltxtchanl")
                    .Alias(Prefix + "dtch")
                    .Description($"Deletes a text channel with a given name. | `{Prefix}dtch TextChannelName`")
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

                cgb.CreateCommand(Prefix + "creatxtchanl")
                    .Alias(Prefix + "ctch")
                    .Description($"Creates a new text channel with a given name. | `{Prefix}ctch TextChannelName`")
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

                cgb.CreateCommand(Prefix + "settopic")
                    .Alias(Prefix + "st")
                    .Description($"Sets a topic on the current channel. | `{Prefix}st My new topic`")
                    .AddCheck(SimpleCheckers.ManageChannels())
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var topic = e.GetArg("topic")?.Trim() ?? "";
                        await e.Channel.Edit(topic: topic).ConfigureAwait(false);
                        await e.Channel.SendMessage(":ok: **New channel topic set.**").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "setchanlname")
                    .Alias(Prefix + "schn")
                    .Description($"Changed the name of the current channel.| `{Prefix}schn NewName`")
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

                cgb.CreateCommand(Prefix + "heap")
                  .Description($"Shows allocated memory - **Bot Owner Only!** | `{Prefix}heap`")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .Do(async e =>
                  {
                      var heap = await Task.Run(() => NadekoStats.Instance.Heap()).ConfigureAwait(false);
                      await e.Channel.SendMessage($"`Heap Size:` {heap}").ConfigureAwait(false);
                  });

                cgb.CreateCommand(Prefix + "prune")
                    .Alias(Prefix + "clr")
                    .Description(
                    "`.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel. " +
                    $"| `{Prefix}prune` or `{Prefix}prune 5` or `{Prefix}prune @Someone` or `{Prefix}prune @Someone X`")
                    .Parameter("user_or_num", ParameterType.Optional)
                    .Parameter("num", ParameterType.Optional)
                    .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("user_or_num"))) // if nothing is set, clear nadeko's messages, no permissions required
                        {
                            var msgs = (await e.Channel.DownloadMessages(100).ConfigureAwait(false)).Where(m => m.User?.Id == e.Server.CurrentUser.Id)?.ToArray();
                            if (msgs == null || !msgs.Any())
                                return;
                            var toDelete = msgs as Message[] ?? msgs.ToArray();
                            await e.Channel.DeleteMessages(toDelete).ConfigureAwait(false);
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
                            await e.Channel.DeleteMessages((await e.Channel.DownloadMessages(val).ConfigureAwait(false)).ToArray()).ConfigureAwait(false);
                            return;
                        }
                        //else if first argument is user
                        var usr = e.Server.FindUsers(e.GetArg("user_or_num")).FirstOrDefault();
                        if (usr == null)
                            return;
                        val = 100;
                        if (!int.TryParse(e.GetArg("num"), out val))
                            val = 100;
                        var mesgs = (await e.Channel.DownloadMessages(100).ConfigureAwait(false)).Where(m => m.User?.Id == usr.Id).Take(val);
                        if (mesgs == null || !mesgs.Any())
                            return;
                        await e.Channel.DeleteMessages(mesgs as Message[] ?? mesgs.ToArray()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "die")
                    .Description($"Shuts the bot down and notifies users about the restart. **Bot Owner Only!** | `{Prefix}die`")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("`Shutting down.`").ConfigureAwait(false);
                        await Task.Delay(2000).ConfigureAwait(false);
                        Environment.Exit(0);
                    });

                cgb.CreateCommand(Prefix + "setname")
                    .Alias(Prefix + "newnm")
                    .Description($"Give the bot a new name. **Bot Owner Only!** | {Prefix}newnm BotName")
                    .Parameter("new_name", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        if (e.GetArg("new_name") == null) return;

                        await client.CurrentUser.Edit("", e.GetArg("new_name")).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "newavatar")
                    .Alias(Prefix + "setavatar")
                    .Description($"Sets a new avatar image for the NadekoBot. Argument is a direct link to an image. **Bot Owner Only!** | `{Prefix}setavatar https://i.ytimg.com/vi/WDudkR1eTMM/maxresdefault.jpg`")
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
                        await client.CurrentUser.Edit("", avatar: image.ToStream()).ConfigureAwait(false);

                        // Send confirm.
                        await e.Channel.SendMessage("New avatar set.").ConfigureAwait(false);

                        // Save the image to disk.
                        image.Save("data/avatar.png", System.Drawing.Imaging.ImageFormat.Png);
                    });

                cgb.CreateCommand(Prefix + "setgame")
                  .Description($"Sets the bots game. **Bot Owner Only!** | `{Prefix}setgame Playing with kwoth`")
                  .Parameter("set_game", ParameterType.Unparsed)
                  .Do(e =>
                  {
                      if (!NadekoBot.IsOwner(e.User.Id) || e.GetArg("set_game") == null) return;

                      client.SetGame(e.GetArg("set_game"));
                  });

                cgb.CreateCommand(Prefix + "send")
                    .Description($"Send a message to someone on a different server through the bot. **Bot Owner Only!** | `{Prefix}send serverid|u:user_id Send this to a user!` or `{Prefix}send serverid|c:channel_id Send this to a channel!`")
                    .Parameter("ids", ParameterType.Required)
                    .Parameter("msg", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        var msg = e.GetArg("msg")?.Trim();

                        if (string.IsNullOrWhiteSpace(msg))
                            return;

                        var ids = e.GetArg("ids").Split('|');
                        if (ids.Length != 2)
                            return;
                        var sid = ulong.Parse(ids[0]);
                        var server = NadekoBot.Client.Servers.Where(s => s.Id == sid).FirstOrDefault();

                        if (server == null)
                            return;

                        if (ids[1].ToUpperInvariant().StartsWith("C:"))
                        {
                            var cid = ulong.Parse(ids[1].Substring(2));
                            var channel = server.TextChannels.Where(c => c.Id == cid).FirstOrDefault();
                            if (channel == null)
                            {
                                return;
                            }
                            await channel.SendMessage(msg);
                        }
                        else if (ids[1].ToUpperInvariant().StartsWith("U:"))
                        {
                            var uid = ulong.Parse(ids[1].Substring(2));
                            var user = server.Users.Where(u => u.Id == uid).FirstOrDefault();
                            if (user == null)
                            {
                                return;
                            }
                            await user.SendMessage(msg);
                        }
                        else
                        {
                            await e.Channel.SendMessage("`Invalid format.`");
                        }
                    });

                cgb.CreateCommand(Prefix + "mentionrole")
                    .Alias(Prefix + "menro")
                    .Description($"Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission. | `{Prefix}menro RoleName`")
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

                cgb.CreateCommand(Prefix + "unstuck")
                  .Description($"Clears the message queue. **Bot Owner Only!** | `{Prefix}unstuck`")
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

                cgb.CreateCommand(Prefix + "donadd")
                    .Description($"Add a donator to the database. | `{Prefix}donadd Donate Amount`")
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
                                DbHandler.Instance.Connection.Insert(new Donator
                                {
                                    Amount = amount,
                                    UserName = donator.Name,
                                    UserId = (long)donator.Id
                                });
                                e.Channel.SendMessage("Successfuly added a new donator. 👑").ConfigureAwait(false);
                            }
                            catch { }
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "announce")
                    .Description($"Sends a message to all servers' general channel bot is connected to.**Bot Owner Only!** | `{Prefix}announce Useless spam`")
                    .Parameter("msg", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        foreach (var ch in NadekoBot.Client.Servers.Select(s => s.DefaultChannel))
                        {
                            await ch.SendMessage(e.GetArg("msg")).ConfigureAwait(false);
                        }

                        await e.Channel.SendMessage(":ok:").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "savechat")
                    .Description($"Saves a number of messages to a text file and sends it to you. **Bot Owner Only** | `{Prefix}savechat 150`")
                    .Parameter("cnt", ParameterType.Required)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        var cntstr = e.GetArg("cnt")?.Trim();
                        int cnt;
                        if (!int.TryParse(cntstr, out cnt))
                            return;
                        ulong? lastmsgId = null;
                        var sb = new StringBuilder();
                        var msgs = new List<Message>(cnt);
                        while (cnt > 0)
                        {
                            var dlcnt = cnt < 100 ? cnt : 100;

                            var dledMsgs = await e.Channel.DownloadMessages(dlcnt, lastmsgId);
                            if (!dledMsgs.Any())
                                break;
                            msgs.AddRange(dledMsgs);
                            lastmsgId = msgs[msgs.Count - 1].Id;
                            cnt -= 100;
                        }
                        await e.User.SendFile($"Chatlog-{e.Server.Name}/#{e.Channel.Name}-{DateTime.Now}.txt", JsonConvert.SerializeObject(new { Messages = msgs.Select(s => s.ToString()) }, Formatting.Indented).ToStream()).ConfigureAwait(false);
                    });

            });
        }
    }
}
