//using Discord.Commands;
//using NadekoBot.Classes;
//using NadekoBot.Extensions;
//using System;
//using System.Drawing;
//using System.Threading.Tasks;

////todo drawing
//namespace NadekoBot.Modules.Gambling
//{
//    internal class FlipCoinCommand : DiscordCommand
//    {

//        public FlipCoinCommand(DiscordModule module) : base(module) { }

//        internal override void Init(CommandGroupBuilder cgb)
//        {
//            cgb.CreateCommand(Module.Prefix + "flip")
//                .Description($"Flips coin(s) - heads or tails, and shows an image. | `{Prefix}flip` or `{Prefix}flip 3`")
//                .Parameter("count", ParameterType.Optional)
//                .Do(FlipCoinFunc());

//            cgb.CreateCommand(Module.Prefix + "betflip")
//                .Alias(Prefix+"bf")
//                .Description($"Bet to guess will the result be heads or tails. Guessing award you double flowers you've bet. | `{Prefix}bf 5 heads` or `{Prefix}bf 3 t`")
//                .Parameter("amount", ParameterType.Required)
//                .Parameter("guess", ParameterType.Required)
//                .Do(BetFlipCoinFunc());
//        }



//        private readonly Random rng = new Random();
//        public Func<CommandEventArgs, Task> BetFlipCoinFunc() => async e =>
//        {

//            var amountstr = amount.Trim();

//            var guessStr = guess.Trim().ToUpperInvariant();
//            if (guessStr != "H" && guessStr != "T" && guessStr != "HEADS" && guessStr != "TAILS")
//                return;

//            int amount;
//            if (!int.TryParse(amountstr, out amount) || amount < 1)
//                return;

//            var userFlowers = Gambling.GetUserFlowers(umsg.Author.Id);

//            if (userFlowers < amount)
//            {
//                await channel.SendMessageAsync($"{umsg.Author.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
//                return;
//            }

//            await FlowersHandler.RemoveFlowers(umsg.Author, "Betflip Gamble", (int)amount, true).ConfigureAwait(false);
//            //heads = true
//            //tails = false

//            var guess = guessStr == "HEADS" || guessStr == "H";
//            bool result = false;
//            if (rng.Next(0, 2) == 1) {
//                await e.Channel.SendFile("heads.png", Properties.Resources.heads.ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
//                result = true;
//            }
//            else {
//                await e.Channel.SendFile("tails.png", Properties.Resources.tails.ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
//            }

//            string str;
//            if (guess == result)
//            {
//                str = $"{umsg.Author.Mention}`You guessed it!` You won {amount * 2}{NadekoBot.Config.CurrencySign}";
//                await FlowersHandler.AddFlowersAsync(umsg.Author, "Betflip Gamble", amount * 2, true).ConfigureAwait(false);

//            }
//            else
//                str = $"{umsg.Author.Mention}`More luck next time.`";

//            await channel.SendMessageAsync(str).ConfigureAwait(false);
//        };

//        public Func<CommandEventArgs, Task> FlipCoinFunc() => async e =>
//        {

//            if (count == "")
//            {
//                if (rng.Next(0, 2) == 1)
//                    await e.Channel.SendFile("heads.png", Properties.Resources.heads.ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
//                else
//                    await e.Channel.SendFile("tails.png", Properties.Resources.tails.ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
//            }
//            else
//            {
//                int result;
//                if (int.TryParse(count, out result))
//                {
//                    if (result > 10)
//                        result = 10;
//                    var imgs = new Image[result];
//                    for (var i = 0; i < result; i++)
//                    {
//                        imgs[i] = rng.Next(0, 2) == 0 ?
//                                    Properties.Resources.tails :
//                                    Properties.Resources.heads;
//                    }
//                    await e.Channel.SendFile($"{result} coins.png", imgs.Merge().ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
//                    return;
//                }
//                await channel.SendMessageAsync("Invalid number").ConfigureAwait(false);
//            }
//        };
//    }
//}
