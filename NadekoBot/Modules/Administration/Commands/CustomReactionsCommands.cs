using NadekoBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Modules.Permissions.Classes;
using Discord;

namespace NadekoBot.Modules.Administration.Commands
{
    class CustomReactionsCommands : DiscordCommand
    {
        public CustomReactionsCommands(DiscordModule module) : base(module)
        {

        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            var Prefix = Module.Prefix;

            cgb.CreateCommand(Prefix + "addcustomreaction")
                .Alias(Prefix + "acr")
                .Description($"Add a custom reaction. **Owner Only!**\n**Usage**: {Prefix}acr \"hello\" I love saying hello to %user%")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Parameter("name", ParameterType.Required)
                .Parameter("message", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var name = e.GetArg("name");
                    var message = e.GetArg("message").Trim();
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        await e.Channel.SendMessage($"Incorrect command usage. See -h {Prefix}acr for correct formatting").ConfigureAwait(false);
                        return;
                    }
                    try
                    {
                        NadekoBot.Config.CustomReactions[name].Add(message);
                    }
                    catch (KeyNotFoundException)
                    {
                        NadekoBot.Config.CustomReactions.Add(name, new System.Collections.Generic.List<string>() { message });
                    }
                    finally
                    {
                        Classes.JSONModels.ConfigHandler.SaveConfig();
                    }
                    await e.Channel.SendMessage($"Added {name} : {message}").ConfigureAwait(false);

                });

            cgb.CreateCommand(Prefix + "listcustomreactions")
            .Alias(Prefix + "lcr")
            .Description("Lists all current custom reactions. **Owner Only!**")
            .AddCheck(SimpleCheckers.OwnerOnly())
            .Do(async e =>
            {

                string message = $"Custom reactions:";
                foreach (var cr in NadekoBot.Config.CustomReactions)
                {
                    if (message.Length > 1500)
                    {
                        await e.Channel.SendMessage(message).ConfigureAwait(false);
                        message = "";
                    }
                    message += $"\n**\"{Format.Escape(cr.Key)}\"**:";
                    int i = 1;
                    foreach (var reaction in cr.Value)
                    {
                        message += "\n     " + i++ + "." + Format.Code(reaction);
                    }

                }
                await e.Channel.SendMessage(message);
            });

            cgb.CreateCommand(Prefix + "deletecustomreaction")
            .Alias(Prefix + "dcr")
            .Description("Deletes a custome reaction with given name (and index)")
            .Parameter("name", ParameterType.Required)
            .Parameter("index", ParameterType.Optional)
            .Do(async e =>
            {
                var name = e.GetArg("name");
                if (!NadekoBot.Config.CustomReactions.ContainsKey(name))
                {
                    await e.Channel.SendMessage("Could not find given key");
                    return;
                }
                string message = "";
                int index;
                if (int.TryParse(e.GetArg("index") ?? "", out index))
                {
                    try
                    {
                        NadekoBot.Config.CustomReactions[name].RemoveAt(index - 1);
                        if (!NadekoBot.Config.CustomReactions[name].Any())
                        {
                            NadekoBot.Config.CustomReactions.Remove(name);
                        }
                        message = $"Deleted response #{index} from {name}";
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        await e.Channel.SendMessage("Index given was out of range").ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    NadekoBot.Config.CustomReactions.Remove(name);
                    message = $"Deleted custom reaction \"{name}\"";
                }
                Classes.JSONModels.ConfigHandler.SaveConfig();
                await e.Channel.SendMessage(message);
            });
        }
    }
}
