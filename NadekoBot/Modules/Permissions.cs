using System;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Classes;
using PermsHandler = NadekoBot.Classes.Permissions.PermissionsHandler;

namespace NadekoBot.Modules {
    class PermissionModule : DiscordModule
    {
        string trigger = "*";
        public PermissionModule() : base()
        {
            //Empty for now
        }

        public override void Install(ModuleManager manager)
        {
            var client = NadekoBot.client;
            manager.CreateCommands("", cgb => {

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(trigger + "ssm").Alias(trigger + "setservermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a module's permission at the server level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerModulePermission(e.Server, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "ssc").Alias(trigger + "setservercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a command's permission at the server level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermsHandler.SetServerCommandPermission(e.Server, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "srm").Alias(trigger + "setrolemodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the role level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                            PermsHandler.SetRoleModulePermission(role, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "src").Alias(trigger + "setrolecommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role",ParameterType.Unparsed)
                    .Description("Sets a command's permission at the role level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Role role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                            PermsHandler.SetRoleCommandPermission(role, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "scm").Alias(trigger + "setchannelmodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the channel level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                            PermsHandler.SetChannelModulePermission(channel, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "scc").Alias(trigger + "setchannelcommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the channel level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.Channel channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                            PermsHandler.SetChannelCommandPermission(channel, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "sum").Alias(trigger + "setusermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the user level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.User user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserModulePermission(user, module, state);
                            await e.Send("I'm setting " + e.GetArg("module") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });

                cgb.CreateCommand(trigger + "suc").Alias(trigger + "setusercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the user level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            string command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            Discord.User user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermsHandler.SetUserCommandPermission(user, command, state);
                            await e.Send("I'm setting " + e.GetArg("command") + " to " + state);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Send(exArg.Message);
                        }
                        catch (Exception ex)
                        {
                            await e.Send("Something went terribly wrong - " + ex.Message);
                        }
                    });
            });
        }
    }
}
