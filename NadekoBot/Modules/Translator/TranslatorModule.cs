using Discord.Modules;
using NadekoBot.Extensions;

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
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);
                commands.ForEach(cmd => cmd.Init(cgb));
            });
        }

    }
}
