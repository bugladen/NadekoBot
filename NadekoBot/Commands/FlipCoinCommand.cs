using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot
{
    class FlipCoinCommand : DiscordCommand
    {

        private Random _r;
        public FlipCoinCommand() : base()
        {
            _r = new Random();
        }

        public override Func<CommandEventArgs, Task> DoFunc()
        {
            return async e => {
                int num = _r.Next(0, 2);
                if (num == 1)
                {
                    await client.SendFile(e.Channel, @"images/coins/heads.png");
                }
                else
                {
                    await client.SendFile(e.Channel, @"images/coins/tails.png");
                }
            };
        }

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("$flip")
                .Description("Flips a coin, heads or tails, and shows an image of it.")
                .Do(DoFunc());
        }
    }
}
