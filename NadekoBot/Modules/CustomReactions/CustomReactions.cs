using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Modules.Permissions.Classes;
using Discord;

namespace NadekoBot.Modules.CustomReactions
{
    class CustomReactionsModule : DiscordModule
    {
        public override string Prefix { get; } = "";

        public override void Install(ModuleManager manager)
        {

            manager.CreateCommands("",cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                foreach (var command in NadekoBot.Config.CustomReactions)
                {
                    var commandName = command.Key.Replace("%mention%", NadekoBot.BotMention);

                    var c = cgb.CreateCommand(commandName);
                    c.Description($"Custom reaction.\n**Usage**:{command.Key}");
                    c.Parameter("args", ParameterType.Unparsed);
                    c.Do(async e =>
                    {
                        Random range = new Random();
                        var ownerMentioned = e.Message.MentionedUsers.Where(x =>/* x != e.User &&*/ NadekoBot.IsOwner(x.Id));
                        var ownerReactions = command.Value.Where(x => x.Contains("%owner%")).ToList();
                        string str;

                        if (ownerMentioned.Any() && ownerReactions.Any())
                        {
                            str = ownerReactions[range.Next(0, ownerReactions.Count)];
                            str = str.Replace("%owner%", ownerMentioned.FirstOrDefault().Mention);
                        }
                        else if (ownerReactions.Any())
                        {
                            var others = command.Value.Except(ownerReactions).ToList();
                            str = others[range.Next(0, others.Count())];
                        }
                        else
                        {
                            str = command.Value[range.Next(0, command.Value.Count())];
                        }

                        str = str.Replace("%user%", e.User.Mention);
                        str = str.Replace("%rng%", "" + range.Next());
                        if (str.Contains("%target%"))
                        {
                            var args = e.GetArg("args");
                            if (string.IsNullOrWhiteSpace(args)) args = string.Empty;
                            str = str.Replace("%target%", e.GetArg("args"));
                        }

                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });
                }
                cgb.CreateCommand("addcustomreaction")
                .Alias("acr")
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
                    catch (System.Collections.Generic.KeyNotFoundException)
                    {
                        NadekoBot.Config.CustomReactions.Add(name, new System.Collections.Generic.List<string>() { message });
                    }
                    finally
                    {
                        Classes.JSONModels.ConfigHandler.SaveConfig();
                    }
                    await e.Channel.SendMessage($"Added {name} : {message}").ConfigureAwait(false);

                });

                cgb.CreateCommand("listcustomreactions")
                .Alias("lcr")
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

                cgb.CreateCommand("deletecustomreaction")
                .Alias("dcr")
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
            });
        }
    }
}
