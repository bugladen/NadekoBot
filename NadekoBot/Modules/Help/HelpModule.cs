using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.Help.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System.Linq;

namespace NadekoBot.Modules.Help
{
    internal class HelpModule : DiscordModule
    {

        public HelpModule()
        {
            commands.Add(new HelpCommand(this));
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Help;



        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);
                commands.ForEach(com => com.Init(cgb));

                cgb.CreateCommand(Prefix + "modules")
                    .Alias(".modules")
                    .Description("List all bot modules. | `{Prefix}modules` or `.modules`")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("`List of modules:` \n• " + string.Join("\n• ", NadekoBot.Client.GetService<ModuleService>().Modules.Select(m => m.Name)) + $"\n`Type \"{Prefix}commands module_name\" to get a list of commands in that module.`")
                                       .ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "commands")
                    .Alias(".commands")
                    .Description("List all of the bot's commands from a certain module. | `{Prefix}commands` or `.commands`")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var module = e.GetArg("module")?.Trim().ToLower();
                        if (string.IsNullOrWhiteSpace(module))
                            return;
                        var cmds = NadekoBot.Client.GetService<CommandService>().AllCommands
                                                    .Where(c => c.Category.ToLower() == module)
                                                    .OrderBy(c=>c.Text)
                                                    .AsEnumerable();
                        var cmdsArray = cmds as Command[] ?? cmds.ToArray();
                        if (!cmdsArray.Any())
                        {
                            await e.Channel.SendMessage("That module does not exist.").ConfigureAwait(false);
                            return;
                        }
                        if (module != "customreactions" && module != "conversations")
                        {
                            await e.Channel.SendMessage("`List Of Commands:`\n" + SearchHelper.ShowInPrettyCode<Command>(cmdsArray,
                                el => $"{el.Text,-15}{"[" + el.Aliases.FirstOrDefault() + "]",-8}"))
                                            .ConfigureAwait(false);
                        }
                        else
                        {
                            await e.Channel.SendMessage("`List Of Commands:`\n• " + string.Join("\n• ", cmdsArray.Select(c => $"{c.Text}")));
                        }
                        await e.Channel.SendMessage($"`You can type \"{Prefix}h command_name\" to see the help about that specific command.`").ConfigureAwait(false);
                    });
            });
        }
    }
}
