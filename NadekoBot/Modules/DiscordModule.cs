using Discord.Modules;
using System.Collections.Generic;
using NadekoBot.Commands;

namespace NadekoBot.Modules {
    internal abstract class DiscordModule : IModule {
        protected List<DiscordCommand> commands = new List<DiscordCommand>();

        protected DiscordModule() {
        }

        public abstract void Install(ModuleManager manager);
    }
}
