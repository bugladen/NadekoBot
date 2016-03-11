using System;
using System.Drawing;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules;

namespace NadekoBot.Commands {
    internal class FlipCoinCommand : DiscordCommand {

        private readonly Random rng = new Random();

        public Func<CommandEventArgs, Task> DoFunc() => async e => {

            if (e.GetArg("count") == "") {
                if (rng.Next(0, 2) == 1)
                    await e.Channel.SendFile("heads.png", Properties.Resources.heads.ToStream(System.Drawing.Imaging.ImageFormat.Png));
                else
                    await e.Channel.SendFile("tails.png", Properties.Resources.tails.ToStream(System.Drawing.Imaging.ImageFormat.Png));
            } else {
                int result;
                if (int.TryParse(e.GetArg("count"), out result)) {
                    if (result > 10)
                        result = 10;
                    var imgs = new Image[result];
                    for (var i = 0; i < result; i++) {
                        imgs[i] = rng.Next(0, 2) == 0 ?
                                    Properties.Resources.tails :
                                    Properties.Resources.heads;
                    }
                    await e.Channel.SendFile($"{result} coins.png", imgs.Merge().ToStream(System.Drawing.Imaging.ImageFormat.Png));
                    return;
                }
                await e.Channel.SendMessage("Invalid number");
            }
        };

        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "flip")
                .Description("Flips coin(s) - heads or tails, and shows an image.\n**Usage**: `$flip` or `$flip 3`")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());
        }

        public FlipCoinCommand(DiscordModule module) : base(module) {}
    }
}
