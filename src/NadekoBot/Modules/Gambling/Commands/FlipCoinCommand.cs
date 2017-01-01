using Discord;
using Discord.Commands;
using ImageSharp;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
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
            private static NadekoRandom rng { get; } = new NadekoRandom();
            private const string headsPath = "data/images/coins/heads.png";
            private const string tailsPath = "data/images/coins/tails.png";
            
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Flip(int count = 1)
            {
                if (count == 1)
                {
                    if (rng.Next(0, 2) == 1)
                        await Context.Channel.SendFileAsync(headsPath, $"{Context.User.Mention} flipped " + Format.Code("Heads") + ".").ConfigureAwait(false);
                    else
                        await Context.Channel.SendFileAsync(tailsPath, $"{Context.User.Mention} flipped " + Format.Code("Tails") + ".").ConfigureAwait(false);
                    return;
                }
                if (count > 10 || count < 1)
                {
                    await Context.Channel.SendErrorAsync("`Invalid number specified. You can flip 1 to 10 coins.`");
                    return;
                }
                var imgs = new Image[count];
                for (var i = 0; i < count; i++)
                {
                    imgs[i] = rng.Next(0, 10) < 5 ?
                                new Image(File.OpenRead(headsPath)) :
                                new Image(File.OpenRead(tailsPath));
                }
                await Context.Channel.SendFileAsync(imgs.Merge().ToStream(), $"{count} coins.png").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Betflip(int amount, string guess)
            {
                var guessStr = guess.Trim().ToUpperInvariant();
                if (guessStr != "H" && guessStr != "T" && guessStr != "HEADS" && guessStr != "TAILS")
                    return;

                if (amount < 3)
                {
                    await Context.Channel.SendErrorAsync($"You can't bet less than 3{Gambling.CurrencySign}.")
                                 .ConfigureAwait(false);
                    return;
                }
                var removed = await CurrencyHandler.RemoveCurrencyAsync(Context.User, "Betflip Gamble", amount, false).ConfigureAwait(false);
                if (!removed)
                {
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} You don't have enough {Gambling.CurrencyPluralName}.").ConfigureAwait(false);
                    return;
                }
                //heads = true
                //tails = false

                var isHeads = guessStr == "HEADS" || guessStr == "H";
                bool result = false;
                string imgPathToSend;
                if (rng.Next(0, 2) == 1)
                {
                    imgPathToSend = headsPath;
                    result = true;
                }
                else
                {
                    imgPathToSend = tailsPath;
                }

                string str;
                if (isHeads == result)
                { 
                    var toWin = (int)Math.Round(amount * 1.8);
                    str = $"{Context.User.Mention}`You guessed it!` You won {toWin}{Gambling.CurrencySign}";
                    await CurrencyHandler.AddCurrencyAsync(Context.User, "Betflip Gamble", toWin, false).ConfigureAwait(false);
                }
                else
                {
                    str = $"{Context.User.Mention}`Better luck next time.`";
                }

                await Context.Channel.SendFileAsync(imgPathToSend, str).ConfigureAwait(false);
            }
        }
    }
}