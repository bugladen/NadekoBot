using Discord.Modules;
using System.Collections.Generic;
using NadekoBot.Commands;

namespace NadekoBot.Modules {
    internal abstract class DiscordModule : IModule {
        protected readonly HashSet<IDiscordCommand> commands = new HashSet<IDiscordCommand>();

        public abstract void Install(ModuleManager manager);
    }
}
