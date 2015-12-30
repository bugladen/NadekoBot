using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Legacy;

namespace NadekoBot
{
    class FlipCoinCommand : DiscordCommand
    {

        private Random _r;
        public FlipCoinCommand() : base()
        {
            _r = new Random();
        }

        public override Func<CommandEventArgs, Task> DoFunc() => async e =>
        {
            int num = _r.Next(0, 2);
            if (num == 1)
            {
                await e.Channel.SendFile(@"images/coins/heads.png");
            }
            else
            {
                await e.Channel.SendFile(@"images/coins/tails.png");
            }
        };

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("$flip")
                .Description("Flips a coin, heads or tails, and shows an image of it.")
                .Do(DoFunc());
        }
    }
}
