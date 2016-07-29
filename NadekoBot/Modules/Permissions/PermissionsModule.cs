using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Commands;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Permissions.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    internal class PermissionModule : DiscordModule
    {
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Permissions;

        public PermissionModule()
        {
            commands.Add(new FilterInvitesCommand(this));
            commands.Add(new FilterWords(this));
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "permrole")
                    .Alias(Prefix + "pr")
                    .Description($"Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'. | `{Prefix}pr role`")
                    .Parameter("role", ParameterType.Unparsed)
                     .Do(async e =>
                     {
                         if (string.IsNullOrWhiteSpace(e.GetArg("role")))
                         {
                             await e.Channel.SendMessage($"Current permissions role is `{PermissionsHandler.GetServerPermissionsRoleName(e.Server)}`").ConfigureAwait(false);
                             return;
                         }

                         var arg = e.GetArg("role");
                         Discord.Role role = null;
                         try
                         {
                             role = PermissionHelper.ValidateRole(e.Server, arg);
                         }
                         catch (Exception ex)
                         {
                             Console.WriteLine(ex.Message);
                             await e.Channel.SendMessage($"Role `{arg}` probably doesn't exist. Create the role with that name first.").ConfigureAwait(false);
                             return;
                         }
                         await PermissionsHandler.SetPermissionsRole(e.Server, role.Name).ConfigureAwait(false);
                         await e.Channel.SendMessage($"Role `{role.Name}` is now required in order to change permissions.").ConfigureAwait(false);
                     });

                cgb.CreateCommand(Prefix + "rolepermscopy")
                    .Alias(Prefix + "rpc")
                    .Description($"Copies BOT PERMISSIONS (not discord permissions) from one role to another. |`{Prefix}rpc Some Role ~ Some other role`")
                    .Parameter("from_to", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("from_to")?.Trim();
                        if (string.IsNullOrWhiteSpace(arg) || !arg.Contains('~'))
                            return;
                        var args = arg.Split('~').Select(a => a.Trim()).ToArray();
                        if (args.Length > 2)
                        {
                            await e.Channel.SendMessage("💢Invalid number of '~'s in the argument.").ConfigureAwait(false);
                            return;
                        }
                        try
                        {
                            var fromRole = PermissionHelper.ValidateRole(e.Server, args[0]);
                            var toRole = PermissionHelper.ValidateRole(e.Server, args[1]);

                            await PermissionsHandler.CopyRolePermissions(fromRole, toRole).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Copied permission settings from **{fromRole.Name}** to **{toRole.Name}**.").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢{ex.Message}").ConfigureAwait(false);
                        }
                    });
                cgb.CreateCommand(Prefix + "chnlpermscopy")
                    .Alias(Prefix + "cpc")
                    .Description($"Copies BOT PERMISSIONS (not discord permissions) from one channel to another. |`{Prefix}cpc Some Channel ~ Some other channel`")
                    .Parameter("from_to", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("from_to")?.Trim();
                        if (string.IsNullOrWhiteSpace(arg) || !arg.Contains('~'))
                            return;
                        var args = arg.Split('~').Select(a => a.Trim()).ToArray();
                        if (args.Length > 2)
                        {
                            await e.Channel.SendMessage("💢Invalid number of '~'s in the argument.");
                            return;
                        }
                        try
                        {
                            var fromChannel = PermissionHelper.ValidateChannel(e.Server, args[0]);
                            var toChannel = PermissionHelper.ValidateChannel(e.Server, args[1]);

                            await PermissionsHandler.CopyChannelPermissions(fromChannel, toChannel).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Copied permission settings from **{fromChannel.Name}** to **{toChannel.Name}**.").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢{ex.Message}");
                        }
                    });
                cgb.CreateCommand(Prefix + "usrpermscopy")
                    .Alias(Prefix + "upc")
                    .Description($"Copies BOT PERMISSIONS (not discord permissions) from one role to another. |`{Prefix}upc @SomeUser ~ @SomeOtherUser`")
                    .Parameter("from_to", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("from_to")?.Trim();
                        if (string.IsNullOrWhiteSpace(arg) || !arg.Contains('~'))
                            return;
                        var args = arg.Split('~').Select(a => a.Trim()).ToArray();
                        if (args.Length > 2)
                        {
                            await e.Channel.SendMessage("💢Invalid number of '~'s in the argument.").ConfigureAwait(false);
                            return;
                        }
                        try
                        {
                            var fromUser = PermissionHelper.ValidateUser(e.Server, args[0]);
                            var toUser = PermissionHelper.ValidateUser(e.Server, args[1]);

                            await PermissionsHandler.CopyUserPermissions(fromUser, toUser).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Copied permission settings from **{fromUser.ToString()}**to * *{toUser.ToString()}**.").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢{ex.Message}");
                        }
                    });

                cgb.CreateCommand(Prefix + "verbose")
                    .Alias(Prefix + "v")
                    .Description($"Sets whether to show when a command/module is blocked. | `{Prefix}verbose true`")
                    .Parameter("arg", ParameterType.Required)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("arg");
                        var val = PermissionHelper.ValidateBool(arg);
                        await PermissionsHandler.SetVerbosity(e.Server, val).ConfigureAwait(false);
                        await e.Channel.SendMessage($"Verbosity set to {val}.").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "srvrperms")
                    .Alias(Prefix + "sp")
                    .Description($"Shows banned permissions for this server. | `{Prefix}sp`")
                    .Do(async e =>
                    {
                        var perms = PermissionsHandler.GetServerPermissions(e.Server);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage("No permissions set for this server.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "roleperms")
                    .Alias(Prefix + "rp")
                    .Description($"Shows banned permissions for a certain role. No argument means for everyone. | `{Prefix}rp AwesomeRole`")
                    .Parameter("role", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("role");
                        var role = e.Server.EveryoneRole;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try
                            {
                                role = PermissionHelper.ValidateRole(e.Server, arg);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message).ConfigureAwait(false);
                                return;
                            }

                        var perms = PermissionsHandler.GetRolePermissionsById(e.Server, role.Id);

                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for **{role.Name}** role.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "chnlperms")
                    .Alias(Prefix + "cp")
                    .Description($"Shows banned permissions for a certain channel. No argument means for this channel. | `{Prefix}cp #dev`")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("channel");
                        var channel = e.Channel;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try
                            {
                                channel = PermissionHelper.ValidateChannel(e.Server, arg);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message).ConfigureAwait(false);
                                return;
                            }

                        var perms = PermissionsHandler.GetChannelPermissionsById(e.Server, channel.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for **{channel.Name}** channel.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "userperms")
                    .Alias(Prefix + "up")
                    .Description($"Shows banned permissions for a certain user. No argument means for yourself. | `{Prefix}up Kwoth`")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var user = e.User;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
                            try
                            {
                                user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message).ConfigureAwait(false);
                                return;
                            }

                        var perms = PermissionsHandler.GetUserPermissionsById(e.Server, user.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for user **{user.Name}**.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "srvrmdl")
                    .Alias(Prefix + "sm")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description($"Sets a module's permission at the server level. | `{Prefix}sm \"module name\" enable`")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            await PermissionsHandler.SetServerModulePermission(e.Server, module, state).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "srvrcmd").Alias(Prefix + "sc")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description($"Sets a command's permission at the server level. | `{Prefix}sc \"command name\" disable`")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            await PermissionsHandler.SetServerCommandPermission(e.Server, command, state).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "rolemdl").Alias(Prefix + "rm")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description($"Sets a module's permission at the role level. | `{Prefix}rm \"module name\" enable MyRole`")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("role")?.ToLower() == "all")
                            {
                                foreach (var role in e.Server.Roles)
                                {
                                    await PermissionsHandler.SetRoleModulePermission(role, module, state).ConfigureAwait(false);
                                }
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **ALL** roles.").ConfigureAwait(false);
                            }
                            else
                            {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                await PermissionsHandler.SetRoleModulePermission(role, module, state).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "rolecmd").Alias(Prefix + "rc")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description($"Sets a command's permission at the role level. | `{Prefix}rc \"command name\" disable MyRole`")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("role")?.ToLower() == "all")
                            {
                                foreach (var role in e.Server.Roles)
                                {
                                    await PermissionsHandler.SetRoleCommandPermission(role, command, state).ConfigureAwait(false);
                                }
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **ALL** roles.").ConfigureAwait(false);
                            }
                            else
                            {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                await PermissionsHandler.SetRoleCommandPermission(role, command, state).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "chnlmdl").Alias(Prefix + "cm")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description($"Sets a module's permission at the channel level. | `{Prefix}cm \"module name\" enable SomeChannel`")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var channelArg = e.GetArg("channel");
                            if (channelArg?.ToLower() == "all")
                            {
                                foreach (var channel in e.Server.TextChannels)
                                {
                                    await PermissionsHandler.SetChannelModulePermission(channel, module, state).ConfigureAwait(false);
                                }
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on **ALL** channels.").ConfigureAwait(false);
                            }
                            else if (string.IsNullOrWhiteSpace(channelArg))
                            {
                                await PermissionsHandler.SetChannelModulePermission(e.Channel, module, state).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{e.Channel.Name}** channel.").ConfigureAwait(false);
                            }
                            else
                            {
                                var channel = PermissionHelper.ValidateChannel(e.Server, channelArg);

                                await PermissionsHandler.SetChannelModulePermission(channel, module, state).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "chnlcmd").Alias(Prefix + "cc")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description($"Sets a command's permission at the channel level. | `{Prefix}cc \"command name\" enable SomeChannel`")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("channel")?.ToLower() == "all")
                            {
                                foreach (var channel in e.Server.TextChannels)
                                {
                                    await PermissionsHandler.SetChannelCommandPermission(channel, command, state).ConfigureAwait(false);
                                }
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on **ALL** channels.").ConfigureAwait(false);
                            }
                            else
                            {
                                var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                                await PermissionsHandler.SetChannelCommandPermission(channel, command, state).ConfigureAwait(false);
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "usrmdl").Alias(Prefix + "um")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description($"Sets a module's permission at the user level. | `{Prefix}um \"module name\" enable SomeUsername`")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            await PermissionsHandler.SetUserModulePermission(user, module, state).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "usrcmd").Alias(Prefix + "uc")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description($"Sets a command's permission at the user level. | `{Prefix}uc \"command name\" enable SomeUsername`")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            await PermissionsHandler.SetUserCommandPermission(user, command, state).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allsrvrmdls").Alias(Prefix + "asm")
                    .Parameter("bool", ParameterType.Required)
                    .Description($"Sets permissions for all modules at the server level. | `{Prefix}asm [enable/disable]`")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules)
                            {
                                await PermissionsHandler.SetServerModulePermission(e.Server, module.Name, state).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allsrvrcmds").Alias(Prefix + "asc")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description($"Sets permissions for all commands from a certain module at the server level. | `{Prefix}asc \"module name\" [enable/disable]`")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));

                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                            {
                                await PermissionsHandler.SetServerCommandPermission(e.Server, command.Text, state).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allchnlmdls").Alias(Prefix + "acm")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description($"Sets permissions for all modules at the channel level. | `{Prefix}acm [enable/disable] SomeChannel`")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var chArg = e.GetArg("channel");
                            var channel = string.IsNullOrWhiteSpace(chArg) ? e.Channel : PermissionHelper.ValidateChannel(e.Server, chArg);
                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules)
                            {
                                await PermissionsHandler.SetChannelModulePermission(channel, module.Name, state).ConfigureAwait(false);
                            }

                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allchnlcmds").Alias(Prefix + "acc")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description($"Sets permissions for all commands from a certain module at the channel level. | `{Prefix}acc \"module name\" [enable/disable] SomeChannel`")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                            {
                                await PermissionsHandler.SetChannelCommandPermission(channel, command.Text, state).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allrolemdls").Alias(Prefix + "arm")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description($"Sets permissions for all modules at the role level. | `{Prefix}arm [enable/disable] MyRole`")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));
                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules)
                            {
                                await PermissionsHandler.SetRoleModulePermission(role, module.Name, state).ConfigureAwait(false);
                            }

                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allrolecmds").Alias(Prefix + "arc")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description($"Sets permissions for all commands from a certain module at the role level. | `{Prefix}arc \"module name\" [enable/disable] MyRole`")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            if (e.GetArg("role")?.ToLower() == "all")
                            {
                                foreach (var role in e.Server.Roles)
                                {
                                    foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                                    {
                                        await PermissionsHandler.SetRoleCommandPermission(role, command.Text, state).ConfigureAwait(false);
                                    }
                                }
                                await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **all roles** role.").ConfigureAwait(false);
                            }
                            else
                            {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                                {
                                    await PermissionsHandler.SetRoleCommandPermission(role, command.Text, state).ConfigureAwait(false);
                                }
                                await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "ubl")
                    .Description($"Blacklists a mentioned user. | `{Prefix}ubl [user_mention]`")
                    .Parameter("user", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.Message.MentionedUsers.Any()) return;
                            var usr = e.Message.MentionedUsers.First();
                            NadekoBot.Config.UserBlacklist.Add(usr.Id);
                            await ConfigHandler.SaveConfig().ConfigureAwait(false);
                            await e.Channel.SendMessage($"`Sucessfully blacklisted user {usr.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "uubl")
                   .Description($"Unblacklists a mentioned user. | `{Prefix}uubl [user_mention]`")
                   .Parameter("user", ParameterType.Unparsed)
                   .AddCheck(SimpleCheckers.OwnerOnly())
                   .Do(async e =>
                   {
                       await Task.Run(async () =>
                       {
                           if (!e.Message.MentionedUsers.Any()) return;
                           var usr = e.Message.MentionedUsers.First();
                           if (NadekoBot.Config.UserBlacklist.Contains(usr.Id))
                           {
                               NadekoBot.Config.UserBlacklist.Remove(usr.Id);
                               await ConfigHandler.SaveConfig().ConfigureAwait(false);
                               await e.Channel.SendMessage($"`Sucessfully unblacklisted user {usr.Name}`").ConfigureAwait(false);
                           }
                           else
                           {
                               await e.Channel.SendMessage($"`{usr.Name} was not in blacklist`").ConfigureAwait(false);
                           }
                       }).ConfigureAwait(false);
                   });

                cgb.CreateCommand(Prefix + "cbl")
                    .Description($"Blacklists a mentioned channel (#general for example). | `{Prefix}cbl #some_channel`")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.Message.MentionedChannels.Any()) return;
                            var ch = e.Message.MentionedChannels.First();
                            NadekoBot.Config.UserBlacklist.Add(ch.Id);
                            await ConfigHandler.SaveConfig().ConfigureAwait(false);
                            await e.Channel.SendMessage($"`Sucessfully blacklisted channel {ch.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "cubl")
                    .Description($"Unblacklists a mentioned channel (#general for example). | `{Prefix}cubl #some_channel`")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.Message.MentionedChannels.Any()) return;
                            var ch = e.Message.MentionedChannels.First();
                            NadekoBot.Config.UserBlacklist.Remove(ch.Id);
                            await ConfigHandler.SaveConfig().ConfigureAwait(false);
                            await e.Channel.SendMessage($"`Sucessfully blacklisted channel {ch.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "sbl")
                    .Description($"Blacklists a server by a name or id (#general for example). **BOT OWNER ONLY** | `{Prefix}sbl [servername/serverid]`")
                    .Parameter("server", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            var arg = e.GetArg("server")?.Trim();
                            if (string.IsNullOrWhiteSpace(arg))
                                return;
                            var server = NadekoBot.Client.Servers.FirstOrDefault(s => s.Id.ToString() == arg) ??
                                         NadekoBot.Client.FindServers(arg.Trim()).FirstOrDefault();
                            if (server == null)
                            {
                                await e.Channel.SendMessage("Cannot find that server").ConfigureAwait(false);
                                return;
                            }
                            var serverId = server.Id;
                            NadekoBot.Config.ServerBlacklist.Add(serverId);
                            await ConfigHandler.SaveConfig().ConfigureAwait(false);
                            //cleanup trivias and typeracing
                            Modules.Games.Commands.Trivia.TriviaGame trivia;
                            TriviaCommands.RunningTrivias.TryRemove(serverId, out trivia);
                            TypingGame typeracer;
                            SpeedTyping.RunningContests.TryRemove(serverId, out typeracer);

                            await e.Channel.SendMessage($"`Sucessfully blacklisted server {server.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "cmdcooldown")
                    .Alias(Prefix+ "cmdcd")
                    .Description($"Sets a cooldown per user for a command. Set 0 to clear. | `{Prefix}cmdcd \"some cmd\" 5`")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("secs",ParameterType.Required)
                    .AddCheck(SimpleCheckers.ManageMessages())
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var secsStr = e.GetArg("secs").Trim();
                            int secs;
                            if (!int.TryParse(secsStr, out secs) || secs < 0 || secs > 3600)
                                throw new ArgumentOutOfRangeException("secs", "Invalid second parameter. (Must be a number between 0 and 3600)");


                            await PermissionsHandler.SetCommandCooldown(e.Server, command, secs).ConfigureAwait(false);
                            if(secs == 0)
                                await e.Channel.SendMessage($"Command **{command}** has no coooldown now.").ConfigureAwait(false);
                            else
                                await e.Channel.SendMessage($"Command **{command}** now has a **{secs} {(secs==1 ? "second" : "seconds")}** cooldown.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "allcmdcooldowns")
                    .Alias(Prefix + "acmdcds")
                    .Description("Shows a list of all commands and their respective cooldowns.")
                    .Do(async e =>
                    {
                        ServerPermissions perms;
                        PermissionsHandler.PermissionsDict.TryGetValue(e.Server.Id, out perms);
                        if (perms == null)
                            return;

                        if (!perms.CommandCooldowns.Any())
                        {
                            await e.Channel.SendMessage("`No command cooldowns set.`").ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage(SearchHelper.ShowInPrettyCode(perms.CommandCooldowns.Select(c=>c.Key+ ": "+c.Value+" secs"),s=>$"{s,-30}",2)).ConfigureAwait(false);
                    });
            });
        }
    }
}
