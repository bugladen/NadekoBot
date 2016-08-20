using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Services;

//todo DB
namespace NadekoBot.Modules.Gambling
{
    [Module("$", AppendSpace = false)]
    public partial class Gambling : DiscordModule
    {
        public Gambling(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Raffle(IMessage imsg, [Remainder] IRole role = null)
        {
            var channel = imsg.Channel as ITextChannel;

            role = role ?? channel.Guild.EveryoneRole;

            var members = (await role.Members()).Where(u => u.Status == UserStatus.Online);
            var membersArray = members as IUser[] ?? members.ToArray();
            var usr = membersArray[new Random().Next(0, membersArray.Length)];
            await channel.SendMessageAsync($"**Raffled user:** {usr.Username} (id: {usr.Id})").ConfigureAwait(false);

        }

        ////todo DB
        //[LocalizedCommand("$$$"), LocalizedDescription("$$$"), LocalizedSummary("$$$")]
        //[RequireContext(ContextType.Guild)]
        //public async Task Cash(IMessage imsg, [Remainder] string arg)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //    var usr = e.Message.MentionedUsers.FirstOrDefault() ?? e.User;
        //    var pts = GetUserFlowers(usr.Id);
        //    var str = $"{usr.Name} has {pts} {NadekoBot.Config.CurrencySign}";
        //    await channel.SendMessageAsync(str).ConfigureAwait(false);
        //}

        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Give(IMessage imsg, long amount, [Remainder] IUser receiver)
        //{
        //    var channel = imsg.Channel as ITextChannel;
        //    if (amount <= 0)
        //        return;
        //    var userFlowers = GetUserFlowers(imsg.Author.Id);

        //    if (userFlowers < amount)
        //    {
        //        await channel.SendMessageAsync($"{e.User.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
        //        return;
        //    }

        //    await FlowersHandler.RemoveFlowers(e.User, "Gift", (int)amount, true).ConfigureAwait(false);
        //    await FlowersHandler.AddFlowersAsync(receiver, "Gift", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{e.User.Mention} successfully sent {amount} {NadekoBot.Config.CurrencyName}s to {receiver.Mention}!").ConfigureAwait(false);

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
        //    var channel = imsg.Channel as ITextChannel;

        //    if (amount <= 0)
        //        return;

        //    await FlowersHandler.AddFlowersAsync(usrId, $"Awarded by bot owner. ({imsg.Author.Username}/{imsg.Author.Id})", (int)amount).ConfigureAwait(false);

        //    await channel.SendMessageAsync($"{e.User.Mention} successfully awarded {amount} {NadekoBot.Config.CurrencyName}s to <@{usrId}>!").ConfigureAwait(false);
        //}


        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Take(IMessage imsg, long amount, [Remainder] IGuildUser ownedPerson)
        {
            var channel = imsg.Channel as ITextChannel;

            if (amount < 0)
                return;

            var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                u.Id != NadekoBot.Client.CurrentUser.Id);
            if (mentionedUser == null)
                return;

            await FlowersHandler.RemoveFlowers(ownedPerson.Id, $"Taken by bot owner.({e.User.Name}/{e.User.Id})", (int)amount).ConfigureAwait(false);

            await channel.SendMessageAsync($"{e.User.Mention} successfully took {amount} {NadekoBot.Config.CurrencyName}s from {mentionedUser.Mention}!").ConfigureAwait(false);
        }

        public override void Install(ModuleManager manager)
        {
            cgb.CreateCommand(Prefix + "award")
                .Description($"Gives someone a certain amount of flowers. **Bot Owner Only!** | `{Prefix}award 100 @person`")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Parameter("amount", ParameterType.Required)
                .Parameter("receiver", ParameterType.Unparsed)
                .Do(async e =>
                {

                });

            cgb.CreateCommand(Prefix + "take")
                .Description($"Takes a certain amount of flowers from someone. **Bot Owner Only!** | `{Prefix}take 1 \"@someguy\"`")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Parameter("amount", ParameterType.Required)
                .Parameter("rektperson", ParameterType.Unparsed)
                .Do(async e =>
                {
                });

            cgb.CreateCommand(Prefix + "betroll")
                .Alias(Prefix + "br")
                .Description($"Bets a certain amount of {NadekoBot.Config.CurrencyName}s and rolls a dice. Rolling over 66 yields x2 flowers, over 90 - x3 and 100 x10. | `{Prefix}br 5`")
                .Parameter("amount", ParameterType.Required)
                .Do(async e =>
                {
                    var amountstr = e.GetArg("amount").Trim();
                    int amount;

                    if (!int.TryParse(amountstr, out amount) || amount < 1)
                        return;

                    var userFlowers = GetUserFlowers(e.User.Id);

                    if (userFlowers < amount)
                    {
                        await channel.SendMessageAsync($"{e.User.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
                        return;
                    }

                    await FlowersHandler.RemoveFlowers(e.User, "Betroll Gamble", (int)amount, true).ConfigureAwait(false);

                    var rng = new Random().Next(0, 101);
                    var str = $"{e.User.Mention} `You rolled {rng}.` ";
                    if (rng < 67)
                    {
                        str += "Better luck next time.";
                    }
                    else if (rng < 90)
                    {
                        str += $"Congratulations! You won {amount * 2}{NadekoBot.Config.CurrencySign} for rolling above 66";
                        await FlowersHandler.AddFlowersAsync(e.User, "Betroll Gamble", amount * 2, true).ConfigureAwait(false);
                    }
                    else if (rng < 100)
                    {
                        str += $"Congratulations! You won {amount * 3}{NadekoBot.Config.CurrencySign} for rolling above 90.";
                        await FlowersHandler.AddFlowersAsync(e.User, "Betroll Gamble", amount * 3, true).ConfigureAwait(false);
                    }
                    else
                    {
                        str += $"👑 Congratulations! You won {amount * 10}{NadekoBot.Config.CurrencySign} for rolling **100**. 👑";
                        await FlowersHandler.AddFlowersAsync(e.User, "Betroll Gamble", amount * 10, true).ConfigureAwait(false);
                    }

                    await channel.SendMessageAsync(str).ConfigureAwait(false);

                });

            cgb.CreateCommand(Prefix + "leaderboard")
                .Alias(Prefix + "lb")
                .Description($"Displays bot currency leaderboard | `{Prefix}lb`")
                .Do(async e =>
                {
                    var richestTemp = DbHandler.Instance.GetTopRichest();
                    var richest = richestTemp as CurrencyState[] ?? richestTemp.ToArray();
                    if (richest.Length == 0)
                        return;
                    await channel.SendMessageAsync(
                        richest.Aggregate(new StringBuilder(
$@"```xl
┏━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━┓
┃        Id           ┃  $$$  ┃
"),
                        (cur, cs) => cur.AppendLine(
$@"┣━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━┫
┃{(e.Server.Users.Where(u => u.Id == (ulong)cs.UserId).FirstOrDefault()?.Name.TrimTo(18, true) ?? cs.UserId.ToString()),-20} ┃ {cs.Value,5} ┃")
                                ).ToString() + "┗━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━┛```").ConfigureAwait(false);
                });
        });
        }

    public static long GetUserFlowers(ulong userId) =>
        Classes.DbHandler.Instance.GetStateByUserId((long)userId)?.Value ?? 0;
}
}
