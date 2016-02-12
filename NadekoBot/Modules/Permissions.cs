using System;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Classes;
using PermsHandler = NadekoBot.Classes.Permissions.PermissionsHandler;

namespace NadekoBot.Modules {
    class PermissionModule : DiscordModule {
        string prefix = "*";
        public PermissionModule() : base() {
            //Empty for now
        }

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;
            manager.CreateCommands("", cgb => {

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                cgb.CreateCommand(prefix + "serverperms")
                  .Description("Shows banned permissions for this server.")
                  .Do(async e => {

                      var perms = PermsHandler.GetServerPermissions(e.Server);
                      if (perms == null)
                          await e.Send("No permissions set.");
                      await e.Send(perms.ToString());
                  });

                cgb.CreateCommand(prefix + "roleperms")
                  .Description("Shows banned permissions for a certain role. No argument means for everyone.")
                  .Parameter("role", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("role");
                      var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                      var perms = PermsHandler.GetRolePermissionsById(e.Server, role.Id);
                      if (perms == null)
                          await e.Send("No permissions set.");
                      await e.Send(perms.ToString());
                  });

                cgb.CreateCommand(prefix + "channelperms")
                  .Description("Shows banned permissions for a certain channel. No argument means for this channel.")
                  .Parameter("channel", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("channel");
                      var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                      var perms = PermsHandler.GetChannelPermissionsById(e.Server, channel.Id);
                      if (perms == null)
                          await e.Send("No permissions set.");
                      await e.Send(perms.ToString());
                  });

                cgb.CreateCommand(prefix + "userperms")
                  .Description("Shows banned permissions for a certain user. No argument means for yourself.")
                  .Parameter("user", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("user");
                      Discord.User user;
                      if (string.IsNullOrWhiteSpace(e.GetArg("user")))
                          user = e.User;
                      else
                          user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                      var perms = PermsHandler.GetUserPermissionsById(e.Server, user.Id);
                      if (perms == null)
                          await e.Send("No permissions set.");
                      await e.Send(perms.ToString());
                  });

                cgb.CreateCommand(prefix + "sm").Alias(prefix + "servermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a module's permission at the server level.")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerModulePermission(e.Server, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
                        } catch (ArgumentException exArg) {
                            await e.Send(exArg.Message);
                        } catch (Exception ex) {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(prefix + "sc").Alias(prefix + "servercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a command's permission at the server level.")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerCommandPermission(e.Server, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
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
                    .Description("Sets a module's permission at the role level.")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                            PermsHandler.SetRoleModulePermission(role, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
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
                    .Description("Sets a command's permission at the role level.")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                            PermsHandler.SetRoleCommandPermission(role, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
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
                    .Description("Sets a module's permission at the channel level.")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                            PermsHandler.SetChannelModulePermission(channel, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
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
                    .Description("Sets a command's permission at the channel level.")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                            PermsHandler.SetChannelCommandPermission(channel, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
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
                    .Description("Sets a module's permission at the user level.")
                    .Do(async e => {
                        try {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.User user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserModulePermission(user, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
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
                    .Description("Sets a command's permission at the user level.")
                    .Do(async e => {
                        try {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.User user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserCommandPermission(user, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
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
