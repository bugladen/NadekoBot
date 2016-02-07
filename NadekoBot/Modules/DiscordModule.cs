using Discord.Modules;
using System.Collections.Generic;

namespace NadekoBot.Modules
{
    abstract class DiscordModule : IModule
    {
        public List<DiscordCommand> commands;

        protected DiscordModule() {
            commands = new List<DiscordCommand>();
        }

        public abstract void Install(ModuleManager manager);
    }
}
