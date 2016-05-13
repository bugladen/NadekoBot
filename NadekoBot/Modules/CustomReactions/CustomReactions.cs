using Discord.Commands;
using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Modules.CustomReactions
{
    class CustomReactionsModule : DiscordModule
    {
        public override string Prefix { get; } = "";

        Random rng = new Random();

        private Dictionary<string, Func<CommandEventArgs, string>> commandFuncs;

        public CustomReactionsModule()
        {
            commandFuncs = new Dictionary<string, Func<CommandEventArgs, string>>
                 {
                    {"%rng%", (e) =>  rng.Next().ToString()},
                    {"%mention%", (e) => NadekoBot.BotMention },
                    {"%user%", e => e.User.Mention },
                    {"%target%", e => e.GetArg("args")?.Trim() ?? "" },
                 };
        }

        public override void Install(ModuleManager manager)
        {

            manager.CreateCommands("", cgb =>
             {

                 cgb.AddCheck(PermissionChecker.Instance);

                 foreach (var command in NadekoBot.Config.CustomReactions)
                 {
                     var commandName = command.Key.Replace("%mention%", NadekoBot.BotMention);

                     var c = cgb.CreateCommand(commandName);
                     if (commandName.Contains(NadekoBot.BotMention))
                         c.Alias(commandName.Replace("<@", "<@!"));
                     c.Description($"Custom reaction.\n**Usage**:{command.Key}")
                         .Parameter("args", ParameterType.Unparsed)
                         .Do(async e =>
                          {
                              string str = command.Value[rng.Next(0, command.Value.Count())];
                              commandFuncs.Keys.ForEach(k => str = str.Replace(k, commandFuncs[k](e)));
                              await e.Channel.SendMessage(str).ConfigureAwait(false);
                          });
                 }

             });
        }
    }
}