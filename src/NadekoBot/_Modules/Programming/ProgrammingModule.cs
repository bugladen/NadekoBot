using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Programming.Commands;

namespace NadekoBot.Modules.Programming
{
    class ProgrammingModule : DiscordModule
    {
        public override string Prefix => NadekoBot.Config.CommandPrefixes.Programming;

        public ProgrammingModule()
        {
            commands.Add(new HaskellRepl(this));
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);
                commands.ForEach(c => c.Init(cgb));
            });
        }
    }
}
