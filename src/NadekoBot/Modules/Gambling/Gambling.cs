using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Services;
using Discord.WebSocket;

//todo DB
namespace NadekoBot.Modules.Gambling
{
    [Module("$", AppendSpace = false)]
    public partial class Gambling : DiscordModule
    {
        public Gambling(ILocalization loc, CommandService cmds, IBotConfiguration config, DiscordSocketClient client) : base(loc, cmds, config, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Raffle(IMessage imsg, [Remainder] IRole role = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            role = role ?? channel.Guild.EveryoneRole;

            var members = role.Members().Where(u => u.Status == UserStatus.Online);
            var membersArray = members as IUser[] ?? members.ToArray();
            var usr = membersArray[new Random().Next(0, membersArray.Length)];
            await channel.SendMessageAsync($"**Raffled user:** {usr.Username} (id: {usr.Id})").ConfigureAwait(false);

        }

        ////todo DB
        //[LocalizedCommand("$$$"), LocalizedDescription("$$$"), LocalizedSummary("$$$")]
        //[RequireContext(ContextType.Guild)]
        //public async Task Cash(IMessage imsg, [Remainder] string arg)
        //{
        //    var channel = (ITextChannel)imsg.Channel;

        //    var usr = e.Message.MentionedUsers.FirstOrDefault() ?? imsg.Author;
        //    var pts = GetUserFlowers(usr.Id);
        //    var str = $"{usr.Name} has {pts} {NadekoBot.Config.CurrencySign}";
        //    await channel.SendMessageAsync(str).ConfigureAwait(false);
        //}

        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Give(IMessage imsg, long amount, [Remainder] IUser receiver)
        //{
        //    var channel = (ITextChannel)imsg.Channel;
        //    if (amount <= 0)
        //        return;
        //    var userFlowers = GetUserFlowers(imsg.Author.Id);

        //    if (userFlowers < amount)
        //    {
        //        await channel.SendMessageAsync($"{imsg.Author.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
        //        return;
        //    }

        //    await FlowersHandler.RemoveFlowers(imsg.Author, "Gift", (int)amount, true).ConfigureAwait(false);
        //    await FlowersHandler.AddFlowersAsync(receiver, "Gift", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{imsg.Author.Mention} successfully sent {amount} {NadekoBot.Config.CurrencyName}s to {receiver.Mention}!").ConfigureAwait(false);

        //}

        ////todo DB
        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public Task Award(IMessage imsg, long amount, [Remainder] IGuildUser usr) =>
        //    Award(imsg, amount, usr.Id);

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Award(IMessage imsg, long amount, [Remainder] ulong usrId)
        //{
        //    var channel = (ITextChannel)imsg.Channel;

        //    if (amount <= 0)
        //        return;

        //    await FlowersHandler.AddFlowersAsync(usrId, $"Awarded by bot owner. ({imsg.Author.Username}/{imsg.Author.Id})", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{imsg.Author.Mention} successfully awarded {amount} {NadekoBot.Config.CurrencyName}s to <@{usrId}>!").ConfigureAwait(false);
        //}

        ////todo owner only
        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public Task Take(IMessage imsg, long amount, [Remainder] IGuildUser user) =>
        //    Take(imsg, amount, user.Id);

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Take(IMessage imsg, long amount, [Remainder] ulong usrId)
        //{
        //    var channel = (ITextChannel)imsg.Channel;
        //    if (amount <= 0)
        //        return;

        //    await FlowersHandler.RemoveFlowers(usrId, $"Taken by bot owner.({imsg.Author.Username}/{imsg.Author.Id})", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{imsg.Author.Mention} successfully took {amount} {NadekoBot.Config.CurrencyName}s from <@{usrId}>!").ConfigureAwait(false);
        //}

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task BetRoll(IMessage imsg, int amount)
        //{
        //    var channel = (ITextChannel)imsg.Channel;

        //    if (amount < 1)
        //        return;

        //    var userFlowers = GetUserFlowers(imsg.Author.Id);

        //    if (userFlowers < amount)
        //    {
        //        await channel.SendMessageAsync($"{imsg.Author.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
        //        return;
        //    }

        //    await FlowersHandler.RemoveFlowers(imsg.Author, "Betroll Gamble", (int)amount, true).ConfigureAwait(false);

        //    var rng = new Random().Next(0, 101);
        //    var str = $"{imsg.Author.Mention} `You rolled {rng}.` ";
        //    if (rng < 67)
        //    {
        //        str += "Better luck next time.";
        //    }
        //    else if (rng < 90)
        //    {
        //        str += $"Congratulations! You won {amount * 2}{NadekoBot.Config.CurrencySign} for rolling above 66";
        //        await FlowersHandler.AddFlowersAsync(imsg.Author, "Betroll Gamble", amount * 2, true).ConfigureAwait(false);
        //    }
        //    else if (rng < 100)
        //    {
        //        str += $"Congratulations! You won {amount * 3}{NadekoBot.Config.CurrencySign} for rolling above 90.";
        //        await FlowersHandler.AddFlowersAsync(imsg.Author, "Betroll Gamble", amount * 3, true).ConfigureAwait(false);
        //    }
        //    else
        //    {
        //        str += $"👑 Congratulations! You won {amount * 10}{NadekoBot.Config.CurrencySign} for rolling **100**. 👑";
        //        await FlowersHandler.AddFlowersAsync(imsg.Author, "Betroll Gamble", amount * 10, true).ConfigureAwait(false);
        //    }

        //    await channel.SendMessageAsync(str).ConfigureAwait(false);
        //}

        ////todo DB
//        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
//        [RequireContext(ContextType.Guild)]
//        public async Task Leaderboard(IMessage imsg)
//        {
//            var channel = (ITextChannel)imsg.Channel;

//            var richestTemp = DbHandler.Instance.GetTopRichest();
//            var richest = richestTemp as CurrencyState[] ?? richestTemp.ToArray();
//            if (richest.Length == 0)
//                return;
//            await channel.SendMessageAsync(
//                richest.Aggregate(new StringBuilder(
//$@"```xl
//┏━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━┓
//┃        Id           ┃  $$$  ┃
//"),
//                (cur, cs) => cur.AppendLine(
//$@"┣━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━┫
//┃{(e.Server.Users.Where(u => u.Id == (ulong)cs.UserId).FirstOrDefault()?.Name.TrimTo(18, true) ?? cs.UserId.ToString()),-20} ┃ {cs.Value,5} ┃")
//                        ).ToString() + "┗━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━┛```").ConfigureAwait(false);
        //}
    }
}
