using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
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

            cgb.CreateCommand(Prefix + "addcustomreaction")
                .Alias(Prefix + "acr")
                .Description($"Add a custom reaction. **Owner Only!**\n**Usage**: {Prefix}acr \"hello\" I love saying hello to %user%")
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
                    await Task.Run(() => Classes.JSONModels.ConfigHandler.SaveConfig());
                    await e.Channel.SendMessage($"Added {name} : {message}").ConfigureAwait(false);

                });

            cgb.CreateCommand(Prefix + "listcustomreactions")
            .Alias(Prefix + "lcr")
            .Description($"Lists all current custom reactions (paginated with 5 commands per page).\n**Usage**:{Prefix}lcr 1")
            .Parameter("num", ParameterType.Required)
            .Do(async e =>
            {
                int num;
                if (!int.TryParse(e.GetArg("num"), out num) || num <= 0) return;
                string result = GetCustomsOnPage(num - 1); //People prefer starting with 1
                await e.Channel.SendMessage(result);
            });

            cgb.CreateCommand(Prefix + "deletecustomreaction")
                .Alias(Prefix + "dcr")
                .Description("Deletes a custome reaction with given name (and index)")
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
                        await e.Channel.SendMessage("Could not find given commandname");
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
                    await Task.Run(() => Classes.JSONModels.ConfigHandler.SaveConfig());
                    await e.Channel.SendMessage(message);
                });
        }

        private readonly int ItemsPerPage = 5;

        private string GetCustomsOnPage(int page)
        {
            var items = NadekoBot.Config.CustomReactions.Skip(page * ItemsPerPage).Take(ItemsPerPage);
            if (!items.Any())
            {
                return $"No items on page {page + 1}.";
            }
            var message = new StringBuilder($"--- Custom reactions - page {page + 1} ---\n");
            foreach (var cr in items)
            {
                message.Append($"{ Format.Code(cr.Key)}\n");
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
        }
    }
}
// zeta is a god
//├
//─
//│
//└
