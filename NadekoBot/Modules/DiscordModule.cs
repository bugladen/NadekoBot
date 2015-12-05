using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
