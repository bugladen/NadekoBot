using Discord;
using Discord.Commands;

//todo drawing
namespace NadekoBot.Modules.Gambling
{ 
    [Group]
    public class FlipCoinCommands
    {

        public FlipCoinCommands() { }


        ////todo drawing
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        //[RequireContext(ContextType.Guild)]
        //public async Task Flip(IUserMessage imsg, int count = 0)
        //{
        //    var channel = (ITextChannel)imsg.Channel;
        //    if (count == 0)
        //    {
        //        if (rng.Next(0, 2) == 1)
        //            await channel.SendFileAsync("heads.png", ).ConfigureAwait(false);
        //        else
        //            await channel.SendFileAsync("tails.png", ).ConfigureAwait(false);
        //        return;
        //    }
        //    if (result > 10)
        //        result = 10;
        //    var imgs = new Image[result];
        //    for (var i = 0; i < result; i++)
        //    {
        //        imgs[i] = rng.Next(0, 2) == 0 ?
        //                    Properties.Resources.tails :
        //                    Properties.Resources.heads;
        //    }
        //    await channel.SendFile($"{result} coins.png", imgs.Merge().ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
        //    return;
        //    await channel.SendMessageAsync("Invalid number").ConfigureAwait(false);
        //}

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        //[RequireContext(ContextType.Guild)]
        //public async Task Betflip(IUserMessage umsg, int amount, string guess)
        //{
        //    var channel = (ITextChannel)umsg.Channel;
        //    var guildUser = (IGuildUser)umsg.Author;
        //    var guessStr = guess.Trim().ToUpperInvariant();
        //    if (guessStr != "H" && guessStr != "T" && guessStr != "HEADS" && guessStr != "TAILS")
        //        return;
            
        //    if (amount < 1)
        //        return;

        //    var userFlowers = Gambling.GetUserFlowers(umsg.Author.Id);

        //    if (userFlowers < amount)
        //    {
        //        await channel.SendMessageAsync($"{umsg.Author.Mention} You don't have enough {Gambling.CurrencyName}s. You only have {userFlowers}{Gambling.CurrencySign}.").ConfigureAwait(false);
        //        return;
        //    }

        //    await CurrencyHandler.RemoveCurrencyAsync(guildUser, "Betflip Gamble", amount, false).ConfigureAwait(false);
        //    //heads = true
        //    //tails = false

        //    var isHeads = guessStr == "HEADS" || guessStr == "H";
        //    bool result = false;
        //    var rng = new Random();
        //    if (rng.Next(0, 2) == 1)
        //    {
        //        await channel.SendFileAsync("heads.png", Properties.Resources.heads.ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
        //        result = true;
        //    }
        //    else
        //    {
        //        await channel.SendFileAsync("tails.png", Properties.Resources.tails.ToStream(System.Drawing.Imaging.ImageFormat.Png)).ConfigureAwait(false);
        //    }

        //    string str;
        //    if (isHeads == result)
        //    {
        //        str = $"{umsg.Author.Mention}`You guessed it!` You won {amount * 2}{Gambling.CurrencySign}";
        //        await CurrencyHandler.AddCurrencyAsync((IGuildUser)umsg.Author, "Betflip Gamble", amount * 2, false).ConfigureAwait(false);

        //    }
        //    else
        //        str = $"{umsg.Author.Mention}`More luck next time.`";

        //    await channel.SendMessageAsync(str).ConfigureAwait(false);
        //}
    }
}
