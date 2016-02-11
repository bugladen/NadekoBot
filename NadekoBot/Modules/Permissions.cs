using System;
using System.Threading.Tasks;
using Discord.Modules;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Collections.Generic;
using NadekoBot.Classes.PermissionCheckers;
using NadekoBot.Classes;

namespace NadekoBot.Modules
{
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
                            PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                            PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                    .Description("Sets a module's permission at the role level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                    .Description("Sets a command's permission at the role level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                    .Description("Sets a module's permission at the channel level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                    .Description("Sets a command's permission at the channel level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            PermissionHelper.ValidateCommand(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                    .Description("Sets a module's permission at the user level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            PermissionHelper.ValidateModule(e.GetArg("module"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
                    .Description("Sets a command's permission at the user level.")
                    // .AddCheck() -> fix this
                    .Do(async e =>
                    {
                        try
                        {
                            PermissionHelper.ValidateModule(e.GetArg("command"));
                            bool state = PermissionHelper.ValidateBool(e.GetArg("bool"));

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
