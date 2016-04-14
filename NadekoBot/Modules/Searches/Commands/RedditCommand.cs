using Discord.Commands;
using NadekoBot.Commands;
using System;

namespace NadekoBot.Modules.Searches.Commands
{
    class RedditCommand : DiscordCommand
    {
        public RedditCommand(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            throw new NotImplementedException();
        }
    }
}
