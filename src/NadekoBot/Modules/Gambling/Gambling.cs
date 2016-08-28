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
using NadekoBot.Services.Database;

//todo DB
namespace NadekoBot.Modules.Gambling
{
    [Module("$", AppendSpace = false)]
    public partial class Gambling : DiscordModule
    {
        public static string CurrencyName { get; set; }
        public static string CurrencyPluralName { get; set; }
        public static string CurrencySign { get; set; }
        
        public Gambling(ILocalization loc, CommandService cmds, DiscordSocketClient client) : base(loc, cmds, client)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var conf = uow.BotConfig.GetOrCreate();

                CurrencyName = conf.CurrencyName;
                CurrencySign = conf.CurrencySign;
                CurrencyPluralName = conf.CurrencyPluralName;
            }
            
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Raffle(IUserMessage umsg, [Remainder] IRole role = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            role = role ?? channel.Guild.EveryoneRole;

            var members = role.Members().Where(u => u.Status == UserStatus.Online);
            var membersArray = members as IUser[] ?? members.ToArray();
            var usr = membersArray[new Random().Next(0, membersArray.Length)];
            await channel.SendMessageAsync($"**Raffled user:** {usr.Username} (id: {usr.Id})").ConfigureAwait(false);

        }

        ////todo DB
        //[LocalizedCommand("$$$"), LocalizedDescription("$$$"), LocalizedSummary("$$$")]
        //[RequireContext(ContextType.Guild)]
        //public async Task Cash(IUserMessage umsg, [Remainder] string arg)
        //{
        //    var channel = (ITextChannel)umsg.Channel;

        //    var usr = e.Message.MentionedUsers.FirstOrDefault() ?? umsg.Author;
        //    var pts = GetUserFlowers(usr.Id);
        //    var str = $"{usr.Name} has {pts} {NadekoBot.Config.CurrencySign}";
        //    await channel.SendMessageAsync(str).ConfigureAwait(false);
        //}

        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Give(IUserMessage umsg, long amount, [Remainder] IUser receiver)
        //{
        //    var channel = (ITextChannel)umsg.Channel;
        //    if (amount <= 0)
        //        return;
        //    var userFlowers = GetUserFlowers(umsg.Author.Id);

        //    if (userFlowers < amount)
        //    {
        //        await channel.SendMessageAsync($"{umsg.Author.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
        //        return;
        //    }

        //    await FlowersHandler.RemoveFlowers(umsg.Author, "Gift", (int)amount, true).ConfigureAwait(false);
        //    await FlowersHandler.AddFlowersAsync(receiver, "Gift", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{umsg.Author.Mention} successfully sent {amount} {NadekoBot.Config.CurrencyName}s to {receiver.Mention}!").ConfigureAwait(false);

        //}

        ////todo DB
        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public Task Award(IUserMessage umsg, long amount, [Remainder] IGuildUser usr) =>
        //    Award(umsg, amount, usr.Id);

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Award(IUserMessage umsg, long amount, [Remainder] ulong usrId)
        //{
        //    var channel = (ITextChannel)umsg.Channel;

        //    if (amount <= 0)
        //        return;

        //    await FlowersHandler.AddFlowersAsync(usrId, $"Awarded by bot owner. ({umsg.Author.Username}/{umsg.Author.Id})", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{umsg.Author.Mention} successfully awarded {amount} {NadekoBot.Config.CurrencyName}s to <@{usrId}>!").ConfigureAwait(false);
        //}

        ////todo owner only
        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public Task Take(IUserMessage umsg, long amount, [Remainder] IGuildUser user) =>
        //    Take(umsg, amount, user.Id);

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Take(IUserMessage umsg, long amount, [Remainder] ulong usrId)
        //{
        //    var channel = (ITextChannel)umsg.Channel;
        //    if (amount <= 0)
        //        return;

        //    await FlowersHandler.RemoveFlowers(usrId, $"Taken by bot owner.({umsg.Author.Username}/{umsg.Author.Id})", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{umsg.Author.Mention} successfully took {amount} {NadekoBot.Config.CurrencyName}s from <@{usrId}>!").ConfigureAwait(false);
        //}

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task BetRoll(IUserMessage umsg, int amount)
        //{
        //    var channel = (ITextChannel)umsg.Channel;

        //    if (amount < 1)
        //        return;

        //    var userFlowers = GetUserFlowers(umsg.Author.Id);

        //    if (userFlowers < amount)
        //    {
        //        await channel.SendMessageAsync($"{umsg.Author.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
        //        return;
        //    }

        //    await FlowersHandler.RemoveFlowers(umsg.Author, "Betroll Gamble", (int)amount, true).ConfigureAwait(false);

        //    var rng = new Random().Next(0, 101);
        //    var str = $"{umsg.Author.Mention} `You rolled {rng}.` ";
        //    if (rng < 67)
        //    {
        //        str += "Better luck next time.";
        //    }
        //    else if (rng < 90)
        //    {
        //        str += $"Congratulations! You won {amount * 2}{NadekoBot.Config.CurrencySign} for rolling above 66";
        //        await FlowersHandler.AddFlowersAsync(umsg.Author, "Betroll Gamble", amount * 2, true).ConfigureAwait(false);
        //    }
        //    else if (rng < 100)
        //    {
        //        str += $"Congratulations! You won {amount * 3}{NadekoBot.Config.CurrencySign} for rolling above 90.";
        //        await FlowersHandler.AddFlowersAsync(umsg.Author, "Betroll Gamble", amount * 3, true).ConfigureAwait(false);
        //    }
        //    else
        //    {
        //        str += $"👑 Congratulations! You won {amount * 10}{NadekoBot.Config.CurrencySign} for rolling **100**. 👑";
        //        await FlowersHandler.AddFlowersAsync(umsg.Author, "Betroll Gamble", amount * 10, true).ConfigureAwait(false);
        //    }

        //    await channel.SendMessageAsync(str).ConfigureAwait(false);
        //}

        ////todo DB
//        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
//        [RequireContext(ContextType.Guild)]
//        public async Task Leaderboard(IUserMessage umsg)
//        {
//            var channel = (ITextChannel)umsg.Channel;

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
