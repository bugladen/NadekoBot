using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Extensions;

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
                Random range = new Random();
                Dictionary<string, Func<CommandEventArgs, string>> MyFuncs = new Dictionary<string, Func<CommandEventArgs, string>>
                {
                    {"%rng%", (e) =>  range.Next().ToString()},
                    {"%mention%", (e) => NadekoBot.BotMention },
                    {"%user%", e => e.User.Mention },
                    {"%target%", e =>
                    {
                        var arg = e.GetArg("args");
                        return string.IsNullOrWhiteSpace(arg) ? "" : arg;
                    } }

                };

                foreach (var command in NadekoBot.Config.CustomReactions)
                {
                    var commandName = command.Key.Replace("%mention%", NadekoBot.BotMention);

                    var c = cgb.CreateCommand(commandName);
                    c.Description($"Custom reaction.\n**Usage**:{command.Key}");
                    c.Parameter("args", ParameterType.Unparsed);
                    c.Do(async e =>
                    {
                        
                        var ownerMentioned = e.Message.MentionedUsers.Where(x =>NadekoBot.IsOwner(x.Id));
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
                        MyFuncs.Keys.ForEach(k => str = str.Replace(k, MyFuncs[k](e)));
                        
                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });
                }
                
            });
        }
    }
}