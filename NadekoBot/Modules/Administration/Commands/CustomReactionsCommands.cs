using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            cgb.CreateCommand(Prefix + "addcustreact")
                .Alias(Prefix + "acr")
                .Description($"Add a custom reaction. Guide here: <https://github.com/Kwoth/NadekoBot/wiki/Custom-Reactions> **Bot Owner Only!**   | `{Prefix}acr \"hello\" I love saying hello to %user%`")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Parameter("name", ParameterType.Required)
                .Parameter("message", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var name = e.GetArg("name");
                    var message = e.GetArg("message")?.Trim();
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        await e.Channel.SendMessage($"Incorrect command usage. See -h {Prefix}acr for correct formatting").ConfigureAwait(false);
                        return;
                    }
                    if (NadekoBot.Config.CustomReactions.ContainsKey(name))
                        NadekoBot.Config.CustomReactions[name].Add(message);
                    else
                        NadekoBot.Config.CustomReactions.Add(name, new System.Collections.Generic.List<string>() { message });
                    await Classes.JSONModels.ConfigHandler.SaveConfig().ConfigureAwait(false);
                    await e.Channel.SendMessage($"Added {name} : {message}").ConfigureAwait(false);

                });

            cgb.CreateCommand(Prefix + "listcustreact")
                .Alias(Prefix + "lcr")
                .Description($"Lists custom reactions (paginated with 30 commands per page). Use 'all' instead of page number to get all custom reactions DM-ed to you.  |`{Prefix}lcr 1`")
                .Parameter("num", ParameterType.Required)
                .Do(async e =>
                {
                    var numStr = e.GetArg("num");

                    if (numStr.ToUpperInvariant() == "ALL")
                    {
                        var fullstr = String.Join("\n", NadekoBot.Config.CustomReactions.Select(kvp => kvp.Key));
                        do
                        {
                            var str = string.Concat(fullstr.Take(1900));
                            fullstr = new string(fullstr.Skip(1900).ToArray());
                            await e.User.SendMessage("```xl\n" + str + "```");
                        } while (fullstr.Length != 0);
                        return;
                    }
                    int num;
                    if (!int.TryParse(numStr, out num) || num <= 0) num = 1;
                    var cmds = GetCustomsOnPage(num - 1);
                    if (!cmds.Any())
                    {
                        await e.Channel.SendMessage("`There are no custom reactions.`");
                    }
                    else
                    {
                        string result = SearchHelper.ShowInPrettyCode<string>(cmds, s => $"{s,-25}"); //People prefer starting with 1
                        await e.Channel.SendMessage($"`Showing page {num}:`\n" + result).ConfigureAwait(false);
                    }
                });

            cgb.CreateCommand(Prefix + "showcustreact")
                .Alias(Prefix + "scr")
                .Description($"Shows all possible responses from a single custom reaction. |`{Prefix}scr %mention% bb`")
                .Parameter("name", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var name = e.GetArg("name")?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        return;
                    if (!NadekoBot.Config.CustomReactions.ContainsKey(name))
                    {
                        await e.Channel.SendMessage("`Can't find that custom reaction.`").ConfigureAwait(false);
                        return;
                    }
                    var items = NadekoBot.Config.CustomReactions[name];
                    var message = new StringBuilder($"Responses for {Format.Bold(name)}:\n");
                    var last = items.Last();

                    int i = 1;
                    foreach (var reaction in items)
                    {
                        message.AppendLine($"[{i++}] " + Format.Code(Format.Escape(reaction)));
                    }
                    await e.Channel.SendMessage(message.ToString());
                });

            cgb.CreateCommand(Prefix + "editcustreact")
                .Alias(Prefix + "ecr")
                .Description($"Edits a custom reaction, arguments are custom reactions name, index to change, and a (multiword) message **Bot Owner Only** | `{Prefix}ecr \"%mention% disguise\" 2 Test 123`")
                .Parameter("name", ParameterType.Required)
                .Parameter("index", ParameterType.Required)
                .Parameter("message", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var name = e.GetArg("name")?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        return;
                    var indexstr = e.GetArg("index")?.Trim();
                    if (string.IsNullOrWhiteSpace(indexstr))
                        return;
                    var msg = e.GetArg("message")?.Trim();
                    if (string.IsNullOrWhiteSpace(msg))
                        return;



                    if (!NadekoBot.Config.CustomReactions.ContainsKey(name))
                    {
                        await e.Channel.SendMessage("`Could not find given commandname`").ConfigureAwait(false);
                        return;
                    }

                    int index;
                    if (!int.TryParse(indexstr, out index) || index < 1 || index > NadekoBot.Config.CustomReactions[name].Count)
                    {
                        await e.Channel.SendMessage("`Invalid index.`").ConfigureAwait(false);
                        return;
                    }
                    index = index - 1;
                    NadekoBot.Config.CustomReactions[name][index] = msg;

                    await Classes.JSONModels.ConfigHandler.SaveConfig().ConfigureAwait(false);
                    await e.Channel.SendMessage($"Edited response #{index + 1} from `{name}`").ConfigureAwait(false);
                });

            cgb.CreateCommand(Prefix + "delcustreact")
                .Alias(Prefix + "dcr")
                .Description($"Deletes a custom reaction with given name (and index). | `{Prefix}dcr index`")
                .Parameter("name", ParameterType.Required)
                .Parameter("index", ParameterType.Optional)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var name = e.GetArg("name")?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        return;
                    if (!NadekoBot.Config.CustomReactions.ContainsKey(name))
                    {
                        await e.Channel.SendMessage("Could not find given commandname").ConfigureAwait(false);
                        return;
                    }
                    string message = "";
                    int index;
                    if (int.TryParse(e.GetArg("index")?.Trim() ?? "", out index))
                    {
                        index = index - 1;
                        if (index < 0 || index > NadekoBot.Config.CustomReactions[name].Count)
                        {
                            await e.Channel.SendMessage("Given index was out of range").ConfigureAwait(false);
                            return;

                        }
                        NadekoBot.Config.CustomReactions[name].RemoveAt(index);
                        if (!NadekoBot.Config.CustomReactions[name].Any())
                        {
                            NadekoBot.Config.CustomReactions.Remove(name);
                        }
                        message = $"Deleted response #{index + 1} from `{name}`";
                    }
                    else
                    {
                        NadekoBot.Config.CustomReactions.Remove(name);
                        message = $"Deleted custom reaction: `{name}`";
                    }
                    await Classes.JSONModels.ConfigHandler.SaveConfig().ConfigureAwait(false);
                    await e.Channel.SendMessage(message).ConfigureAwait(false);
                });
        }

        private readonly int ItemsPerPage = 30;

        private IEnumerable<string> GetCustomsOnPage(int page)
        {
            var items = NadekoBot.Config.CustomReactions.Skip(page * ItemsPerPage).Take(ItemsPerPage);
            if (!items.Any())
            {
                return Enumerable.Empty<string>();
            }
            return items.Select(kvp => kvp.Key);
            /*
            var message = new StringBuilder($"--- Custom reactions - page {page + 1} ---\n");
            foreach (var cr in items)
            {
                message.Append($"{Format.Code(cr.Key)}\n");
                int i = 1;
                var last = cr.Value.Last();
                foreach (var reaction in cr.Value)
                {
                    if (last != reaction)
                        message.AppendLine("  `├" + i++ + "─`" + Format.Bold(reaction));
                    else
                        message.AppendLine("  `└" + i++ + "─`" + Format.Bold(reaction));
                }
            }
            return message.ToString() + "\n";
            */
        }
    }
}
// zeta is a god
//├
//─
//│
//└
