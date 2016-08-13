using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;

namespace NadekoBot.Modules.Translator
{
    internal class TranslatorModule : DiscordModule
    {
        public TranslatorModule()
        {
            commands.Add(new TranslateCommand(this));
            commands.Add(new ValidLanguagesCommand(this));
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Searches;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);
                commands.ForEach(cmd => cmd.Init(cgb));
            });
        }

    }
}
