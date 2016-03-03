using System;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Classes;
using PermsHandler = NadekoBot.Classes.Permissions.PermissionsHandler;
using System.Linq;

namespace NadekoBot.Modules {
    internal class PermissionModule : DiscordModule {
        private const string prefix = ";";

        public PermissionModule()  {
            //Empty for now
        }
        //todo word filtering/invite bans (?:discord(?:\.gg|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))
        public override void Install(ModuleManager manager) {
            var client = NadekoBot.Client;
            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(prefix + "permrole")
                    .Alias(prefix + "pr")
                    .Description("Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'.")
                    .Parameter("role", ParameterType.Unparsed)
                     .Do(async e => {
                         if (string.IsNullOrWhiteSpace(e.GetArg("role"))) {
                             await e.Channel.SendMessage($"Current permissions role is `{PermsHandler.GetServerPermissionsRoleName(e.Server)}`");
                             return;
                         }

                         var arg = e.GetArg("role");
                         Discord.Role role = null;
                         try {
                             role = PermissionHelper.ValidateRole(e.Server, arg);
                         }
                         catch (Exception ex) {
                             Console.WriteLine(ex.Message);
                             await e.Channel.SendMessage($"Role `{arg}` probably doesn't exist. Create the role with that name first.");
                             return;
                         }
                         PermsHandler.SetPermissionsRole(e.Server, role.Name);
                         await e.Channel.SendMessage($"Role `{role.Name}` is now required in order to change permissions.");
                     });

                cgb.CreateCommand(prefix + "verbose")
                    .Alias(prefix + "v")
                    .Description("Sets whether to show when a command/module is blocked.\n**Usage**: ;verbose true")
                    .Parameter("arg", ParameterType.Required)
                    .Do(async e => {
                        var arg = e.GetArg("arg");
                        var val = PermissionHelper.ValidateBool(arg);
                        PermsHandler.SetVerbosity(e.Server, val);
                        await e.Channel.SendMessage($"Verbosity set to {val}.");
                    });

                cgb.CreateCommand(prefix + "serverperms")
                    .Alias(prefix + "sp")
                    .Description("Shows banned permissions for this server.")
                    .Do(async e => {
                        var perms = PermsHandler.GetServerPermissions(e.Server);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage("No permissions set for this server.");
                        await e.Channel.SendMessage(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "roleperms")
                    .Alias(prefix + "rp")
                    .Description("Shows banned permissions for a certain role. No argument means for everyone.\n**Usage**: ;rp AwesomeRole")
                    .Parameter("role", ParameterType.Unparsed)
                    .Do(async e => {
                        var arg = e.GetArg("role");
                        var role = e.Server.EveryoneRole;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try {
                                role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));
                            }
                            catch (Exception ex) {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message);
                                return;
                            }

                        var perms = PermsHandler.GetRolePermissionsById(e.Server, role.Id);

                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for **{role.Name}** role.");
                        await e.Channel.SendMessage(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "channelperms")
                    .Alias(prefix + "cp")
                    .Description("Shows banned permissions for a certain channel. No argument means for this channel.\n**Usage**: ;cp #dev")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e => {
                        var arg = e.GetArg("channel");
                        var channel = e.Channel;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try {
                                channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            }
                            catch (Exception ex) {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message);
                                return;
                            }

                        var perms = PermsHandler.GetChannelPermissionsById(e.Server, channel.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for **{channel.Name}** channel.");
                        await e.Channel.SendMessage(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "userperms")
                    .Alias(prefix + "up")
                    .Description("Shows banned permissions for a certain user. No argument means for yourself.\n**Usage**: ;up Kwoth")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e => {
                        var arg = e.GetArg("user");
                        var user = e.User;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
                            try {
                                user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));
                            }
                            catch (Exception ex) {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message);
                                return;
                            }

                        var perms = PermsHandler.GetUserPermissionsById(e.Server, user.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for user **{user.Name}**.");
                        await e.Channel.SendMessage(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "sm").Alias(prefix + "servermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a module's permission at the server level.\n**Usage**: ;sm [module_name] enable")
                    .Do(async e => {
                        try {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerModulePermission(e.Server, module, state);
                            await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on this server.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "sc").Alias(prefix + "servercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a command's permission at the server level.\n**Usage**: ;sc [command_name] disable")
                    .Do(async e => {
                        try {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerCommandPermission(e.Server, command, state);
                            await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on this server.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "rm").Alias(prefix + "rolemodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the role level.\n**Usage**: ;rm [module_name] enable [role_name]")
                    .Do(async e => {
                        try {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("role")?.ToLower() == "all") {
                                foreach (var role in e.Server.Roles) {
                                    PermsHandler.SetRoleModulePermission(role, module, state);
                                }
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **ALL** roles.");
                            }
                            else {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                PermsHandler.SetRoleModulePermission(role, module, state);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                            }
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "rc").Alias(prefix + "rolecommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the role level.\n**Usage**: ;rc [command_name] disable [role_name]")
                    .Do(async e => {
                        try {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("role")?.ToLower() == "all") {
                                foreach (var role in e.Server.Roles) {
                                    PermsHandler.SetRoleCommandPermission(role, command, state);
                                }
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **ALL** roles.");
                            }
                            else {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                PermsHandler.SetRoleCommandPermission(role, command, state);
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                            }
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "cm").Alias(prefix + "channelmodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the channel level.\n**Usage**: ;cm [module_name] enable [channel_name]")
                    .Do(async e => {
                        try {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("channel")?.ToLower() == "all") {
                                foreach (var channel in e.Server.TextChannels) {
                                    PermsHandler.SetChannelModulePermission(channel, module, state);
                                }
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on **ALL** channels.");
                            }
                            else {
                                var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                                PermsHandler.SetChannelModulePermission(channel, module, state);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                            }
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "cc").Alias(prefix + "channelcommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the channel level.\n**Usage**: ;cc [command_name] enable [channel_name]")
                    .Do(async e => {
                        try {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("channel")?.ToLower() == "all") {
                                foreach (var channel in e.Server.TextChannels) {
                                    PermsHandler.SetChannelCommandPermission(channel, command, state);
                                }
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on **ALL** channels.");
                            }
                            else {
                                var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                                PermsHandler.SetChannelCommandPermission(channel, command, state);
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                            }
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "um").Alias(prefix + "usermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the user level.\n**Usage**: ;um [module_name] enable [user_name]")
                    .Do(async e => {
                        try {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserModulePermission(user, module, state);
                            await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "uc").Alias(prefix + "usercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the user level.\n**Usage**: ;uc [command_name] enable [user_name]")
                    .Do(async e => {
                        try {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserCommandPermission(user, command, state);
                            await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "asm").Alias(prefix + "allservermodules")
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets permissions for all modules at the server level.\n**Usage**: ;asm [enable/disable]")
                    .Do(async e => {
                        try {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules) {
                                PermsHandler.SetServerModulePermission(e.Server, module.Name, state);
                            }
                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** on this server.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "asc").Alias(prefix + "allservercommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets permissions for all commands from a certain module at the server level.\n**Usage**: ;asc [module_name] [enable/disable]")
                    .Do(async e => {
                        try {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));

                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module)) {
                                PermsHandler.SetServerCommandPermission(e.Server, command.Text, state);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** on this server.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "acm").Alias(prefix + "allchannelmodules")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all modules at the channel level.\n**Usage**: ;acm [enable/disable] [channel_name]")
                    .Do(async e => {
                        try {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules) {
                                PermsHandler.SetChannelModulePermission(channel, module.Name, state);
                            }

                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "acc").Alias(prefix + "allchannelcommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all commands from a certain module at the channel level.\n**Usage**: ;acc [module_name] [enable/disable] [channel_name]")
                    .Do(async e => {
                        try {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module)) {
                                PermsHandler.SetChannelCommandPermission(channel, command.Text, state);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "arm").Alias(prefix + "allrolemodules")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets permissions for all modules at the role level.\n**Usage**: ;arm [enable/disable] [role_name]")
                    .Do(async e => {
                        try {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));
                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules) {
                                PermsHandler.SetRoleModulePermission(role, module.Name, state);
                            }

                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "arc").Alias(prefix + "allrolecommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all commands from a certain module at the role level.\n**Usage**: ;arc [module_name] [enable/disable] [channel_name]")
                    .Do(async e => {
                        try {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module)) {
                                PermsHandler.SetRoleCommandPermission(role, command.Text, state);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                        }
                        catch (ArgumentException exArg) {
                            await e.Channel.SendMessage(exArg.Message);
                        }
                        catch (Exception ex) {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message);
                        }
                    });
            });
        }
    }
}
