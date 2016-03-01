using System.Linq;
using Discord.Modules;
using Discord.Commands;

namespace NadekoBot.Modules {
    internal class Help : DiscordModule {

        public Help()  {
            commands.Add(new HelpCommand());
        }

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);
                commands.ForEach(com => com.Init(cgb));

                cgb.CreateCommand(".modules")
                    .Alias("-modules")
                    .Description("List all bot modules.")
                    .Do(async e => {
                        await e.Channel.SendMessage("`List of modules:` \n• " + string.Join("\n• ", NadekoBot.Client.GetService<ModuleService>().Modules.Select(m => m.Name)));
                    });

                cgb.CreateCommand(".commands")
                    .Alias("-commands")
                    .Description("List all of the bot's commands from a certain module.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e => {
                        var commands = NadekoBot.Client.GetService<CommandService>().AllCommands
                                                    .Where(c => c.Category.ToLower() == e.GetArg("module").Trim().ToLower());
                        if (commands == null || commands.Count() == 0) {
                            await e.Channel.SendMessage("That module does not exist.");
                            return;
                        }
                        await e.Channel.SendMessage("`List of commands:` \n• " + string.Join("\n• ", commands.Select(c => c.Text)));
                    });
            });
        }
    }
}
