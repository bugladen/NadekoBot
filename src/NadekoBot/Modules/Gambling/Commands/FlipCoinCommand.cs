using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Image = ImageSharp.Image;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class FlipCoinCommands : NadekoSubmodule
        {
            private readonly IImagesService _images;

            private static NadekoRandom rng { get; } = new NadekoRandom();

            public FlipCoinCommands()
            {
                //todo DI in the future, can't atm
                _images = NadekoBot.Images;
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
                            await Context.Channel.SendFileAsync(heads, "heads.jpg", Context.User.Mention + " " + GetText("flipped", Format.Bold(GetText("heads"))) + ".").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        using (var tails = _images.Tails.ToStream())
                        {
                            await Context.Channel.SendFileAsync(tails, "tails.jpg", Context.User.Mention + " " + GetText("flipped", Format.Bold(GetText("tails"))) + ".").ConfigureAwait(false);
                        }
                    }
                    return;
                }
                if (count > 10 || count < 1)
                {
                    await ReplyErrorLocalized("flip_invalid", 10).ConfigureAwait(false);
                    return;
                }
                var imgs = new Image[count];
                for (var i = 0; i < count; i++)
                {
                    using (var heads = _images.Heads.ToStream())
                    using (var tails = _images.Tails.ToStream())
                    {
                        if (rng.Next(0, 10) < 5)
                        {
                            imgs[i] = new Image(heads);
                        }
                        else
                        {
                            imgs[i] = new Image(tails);
                        }
                    }
                }
                await Context.Channel.SendFileAsync(imgs.Merge().ToStream(), $"{count} coins.png").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Betflip(int amount, string guess)
            {
                var guessStr = guess.Trim().ToUpperInvariant();
                if (guessStr != "H" && guessStr != "T" && guessStr != "HEADS" && guessStr != "TAILS")
                    return;

                if (amount < NadekoBot.BotConfig.MinimumBetAmount)
                {
                    await ReplyErrorLocalized("min_bet_limit", NadekoBot.BotConfig.MinimumBetAmount + CurrencySign).ConfigureAwait(false);
                    return;
                }
                var removed = await CurrencyHandler.RemoveCurrencyAsync(Context.User, "Betflip Gamble", amount, false).ConfigureAwait(false);
                if (!removed)
                {
                    await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
                    return;
                }
                //heads = true
                //tails = false

                //todo this seems stinky, no time to look at it right now
                var isHeads = guessStr == "HEADS" || guessStr == "H";
                var result = false;
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
                    str = Context.User.Mention + " " + GetText("flip_guess", toWin + CurrencySign);
                    await CurrencyHandler.AddCurrencyAsync(Context.User, GetText("betflip_gamble"), toWin, false).ConfigureAwait(false);
                }
                else
                {
                    str = Context.User.Mention + " " + GetText("better_luck");
                }
                using (var toSend = imageToSend.ToStream())
                {
                    await Context.Channel.SendFileAsync(toSend, "result.png", str).ConfigureAwait(false);
                }
            }
        }
    }
}