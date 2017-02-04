using Discord;
using Discord.Commands;
using ImageSharp;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Image = ImageSharp.Image;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class FlipCoinCommands : ModuleBase
        {
            private readonly IImagesService _images;

            private static NadekoRandom rng { get; } = new NadekoRandom();

            public FlipCoinCommands()
            {
                //todo DI in the future, can't atm
                this._images = NadekoBot.Images;
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Flip(int count = 1)
            {
                if (count == 1)
                {
                    if (rng.Next(0, 2) == 1)
                    {
                        using (var heads = _images.Heads.ToStream())
                        {
                            await Context.Channel.SendFileAsync(heads, "heads.jpg", $"{Context.User.Mention} flipped " + Format.Code("Heads") + ".").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        using (var tails = _images.Tails.ToStream())
                        {
                            await Context.Channel.SendFileAsync(tails, "tails.jpg", $"{Context.User.Mention} flipped " + Format.Code("Tails") + ".").ConfigureAwait(false);
                        }
                    }
                    return;
                }
                if (count > 10 || count < 1)
                {
                    await Context.Channel.SendErrorAsync("`Invalid number specified. You can flip 1 to 10 coins.`").ConfigureAwait(false);
                    return;
                }
                var imgs = new Image[count];
                using (var heads = _images.Heads.ToStream())
                using(var tails = _images.Tails.ToStream())
                {
                    for (var i = 0; i < count; i++)
                    {
                        imgs[i] = rng.Next(0, 10) < 5 ?
                                    new Image(heads) :
                                    new Image(tails);
                    }
                    await Context.Channel.SendFileAsync(imgs.Merge().ToStream(), $"{count} coins.png").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Betflip(int amount, string guess)
            {
                var guessStr = guess.Trim().ToUpperInvariant();
                if (guessStr != "H" && guessStr != "T" && guessStr != "HEADS" && guessStr != "TAILS")
                    return;

                if (amount < NadekoBot.BotConfig.MinimumBetAmount)
                {
                    await Context.Channel.SendErrorAsync($"You can't bet less than {NadekoBot.BotConfig.MinimumBetAmount}{CurrencySign}.")
                                 .ConfigureAwait(false);
                    return;
                }
                var removed = await CurrencyHandler.RemoveCurrencyAsync(Context.User, "Betflip Gamble", amount, false).ConfigureAwait(false);
                if (!removed)
                {
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} You don't have enough {CurrencyPluralName}.").ConfigureAwait(false);
                    return;
                }
                //heads = true
                //tails = false

                //todo this seems stinky, no time to look at it right now
                var isHeads = guessStr == "HEADS" || guessStr == "H";
                bool result = false;
                IEnumerable<byte> imageToSend;
                if (rng.Next(0, 2) == 1)
                {
                    imageToSend = _images.Heads;
                    result = true;
                }
                else
                {
                    imageToSend = _images.Tails;
                }

                string str;
                if (isHeads == result)
                { 
                    var toWin = (int)Math.Round(amount * NadekoBot.BotConfig.BetflipMultiplier);
                    str = $"{Context.User.Mention}`You guessed it!` You won {toWin}{CurrencySign}";
                    await CurrencyHandler.AddCurrencyAsync(Context.User, "Betflip Gamble", toWin, false).ConfigureAwait(false);
                }
                else
                {
                    str = $"{Context.User.Mention}`Better luck next time.`";
                }
                using (var toSend = imageToSend.ToStream())
                {
                    await Context.Channel.SendFileAsync(toSend, "result.png", str).ConfigureAwait(false);
                }
            }
        }
    }
}