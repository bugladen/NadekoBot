using System;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Classes;
using PermsHandler = NadekoBot.Classes.Permissions.PermissionsHandler;
using System.Linq;

namespace NadekoBot.Modules {
    class PermissionModule : DiscordModule {
        string prefix = ";";
        public PermissionModule() : base() {
            //Empty for now
        }

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;
            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(prefix + "verbose")
                  .Description("Sets whether to show when a command/module is blocked.\n**Usage**: ;verbose true")
                  .Parameter("arg", ParameterType.Required)
                  .Do(async e => {
                      var arg = e.GetArg("arg");
                      bool val = PermissionHelper.ValidateBool(arg);
                      PermsHandler.SetVerbosity(e.Server, val);
                      await e.Send($"Verbosity set to {val}.");
                  });

                cgb.CreateCommand(prefix + "serverperms")
                    .Alias(prefix + "sp")
                    .Description("Shows banned permissions for this server.")
                    .Do(async e => {
                        var perms = PermsHandler.GetServerPermissions(e.Server);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Send("No permissions set for this server.");
                        await e.Send(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "roleperms")
                    .Alias(prefix + "rp")
                    .Description("Shows banned permissions for a certain role. No argument means for everyone.\n**Usage**: ;rp AwesomeRole")
                    .Parameter("role", ParameterType.Unparsed)
                    .Do(async e => {
                        var arg = e.GetArg("role");
                        Discord.Role role = e.Server.EveryoneRole;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try {
                                role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));
                            } catch (Exception ex) {
                                await e.Send("💢 Error: " + ex.Message);
                                return;
                            }

                        var perms = PermsHandler.GetRolePermissionsById(e.Server, role.Id);

                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Send($"No permissions set for **{role.Name}** role.");
                        await e.Send(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "channelperms")
                    .Alias(prefix + "cp")
                    .Description("Shows banned permissions for a certain channel. No argument means for this channel.\n**Usage**: ;cp #dev")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e => {
                        var arg = e.GetArg("channel");
                        Discord.Channel channel = e.Channel;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try {
                                channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            } catch (Exception ex) {
                                await e.Send("💢 Error: " + ex.Message);
                                return;
                            }

                        var perms = PermsHandler.GetChannelPermissionsById(e.Server, channel.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Send($"No permissions set for **{channel.Name}** channel.");
                        await e.Send(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "userperms")
                    .Alias(prefix + "up")
                    .Description("Shows banned permissions for a certain user. No argument means for yourself.\n**Usage**: ;up Kwoth")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e => {
                        var arg = e.GetArg("user");
                        Discord.User user = e.User;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
                            try {
                                user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));
                            } catch (Exception ex) {
                                await e.Send("💢 Error: " + ex.Message);
                                return;
                            }

                        var perms = PermsHandler.GetUserPermissionsById(e.Server, user.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Send($"No permissions set for user **{user.Name}**.");
                        await e.Send(perms.ToString());
                    });

                cgb.CreateCommand(prefix + "sm").Alias(prefix + "servermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a module's permission at the server level.\n**Usage**: ;sm <module_name> enable")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerModulePermission(e.Server, module, state);
                            await e.Send($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on this server.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "sc").Alias(prefix + "servercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a command's permission at the server level.\n**Usage**: ;sc <command_name> disable")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerCommandPermission(e.Server, command, state);
                            await e.Send($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on this server.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "rm").Alias(prefix + "rolemodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the role level.\n**Usage**: ;rm <module_name> enable <role_name>")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                            PermsHandler.SetRoleModulePermission(role, module, state);
                            await e.Send($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "rc").Alias(prefix + "rolecommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the role level.\n**Usage**: ;rc <command_name> disable <role_name>")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                            PermsHandler.SetRoleCommandPermission(role, command, state);
                            await e.Send($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "cm").Alias(prefix + "channelmodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the channel level.\n**Usage**: ;cm <module_name> enable <channel_name>")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                            PermsHandler.SetChannelModulePermission(channel, module, state);
                            await e.Send($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "cc").Alias(prefix + "channelcommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the channel level.\n**Usage**: ;cm enable <channel_name>")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                            PermsHandler.SetChannelCommandPermission(channel, command, state);
                            await e.Send($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "um").Alias(prefix + "usermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the user level.\n**Usage**: ;um <module_name> enable <user_name>")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.User user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserModulePermission(user, module, state);
                            await e.Send($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "uc").Alias(prefix + "usercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the user level.\n**Usage**: ;uc <module_command> enable <user_name>")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.User user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserCommandPermission(user, command, state);
                            await e.Send($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "asm").Alias(prefix + "allservermodules")
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets permissions for all modules at the server level.\n**Usage**: ;asm <enable/disable>")
                    .Do(async e => {
                        try {
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            foreach (var module in NadekoBot.client.Modules().Modules) {
                                PermsHandler.SetServerModulePermission(e.Server, module.Name, state);
                            }
                            await e.Send($"All modules have been **{(state ? "enabled" : "disabled")}** on this server.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "asc").Alias(prefix + "allservercommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets permissions for all commands from a certain module at the server level.\n**Usage**: ;asc <module_name> <enable/disable>")
                    .Do(async e => {
                        try {
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));

                            foreach (var command in NadekoBot.client.Commands().AllCommands.Where(c => c.Category == module)) {
                                PermsHandler.SetServerCommandPermission(e.Server, command.Text, state);
                            }
                            await e.Send($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** on this server.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "acm").Alias(prefix + "allchannelmodules")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all modules at the server level.\n**Usage**: ;acm <enable/disable> <channel_name>")
                    .Do(async e => {
                        try {
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            foreach (var module in NadekoBot.client.Modules().Modules) {
                                PermsHandler.SetChannelModulePermission(channel, module.Name, state);
                            }

                            await e.Send($"All modules have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "acc").Alias(prefix + "allchannelcommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all commands from a certain module at the server level.\n**Usage**: ;acc <module_name> <enable/disable> <channel_name>")
                    .Do(async e => {
                        try {
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.client.Commands().AllCommands.Where(c => c.Category == module)) {
                                PermsHandler.SetChannelCommandPermission(channel, command.Text, state);
                            }
                            await e.Send($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "arm").Alias(prefix + "allrolemodules")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets permissions for all modules at the role level.\n**Usage**: ;arm <enable/disable> <role_name>")
                    .Do(async e => {
                        try {
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));
                            foreach (var module in NadekoBot.client.Modules().Modules) {
                                PermsHandler.SetRoleModulePermission(role, module.Name, state);
                            }

                            await e.Send($"All modules have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "arc").Alias(prefix + "allrolecommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all commands from a certain module at the role level.\n**Usage**: ;arc <module_name> <enable/disable> <channel_name>")
                    .Do(async e => {
                        try {
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.client.Commands().AllCommands.Where(c => c.Category == module)) {
                                PermsHandler.SetRoleCommandPermission(role, command.Text, state);
                            }
                            await e.Send($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.");
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });
            });
        }
    }
}
