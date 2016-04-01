using Discord.Commands;
using NadekoBot.Commands;
using System;

namespace NadekoBot.Modules.Games.Commands
{
    class Bomberman : DiscordCommand
    {
        public Bomberman(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            throw new NotImplementedException();
        }
    }
}
