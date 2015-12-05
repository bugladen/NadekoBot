using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Modules;

namespace NadekoBot.Modules
{
    class Games : DiscordModule
    {
        public Games() : base() {
            commands.Add(new Trivia());
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                commands.ForEach(cmd => cmd.Init(cgb));
            });
        }
    }
}
