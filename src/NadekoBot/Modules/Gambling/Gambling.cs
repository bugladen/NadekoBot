using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Services;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using NadekoBot.Services.Database;

namespace NadekoBot.Modules.Gambling
{
    [NadekoModule("Gambling", "$")]
    public partial class Gambling : DiscordModule
    {
        public static string CurrencyName { get; set; }
        public static string CurrencyPluralName { get; set; }
        public static string CurrencySign { get; set; }
        
        public Gambling() : base()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var conf = uow.BotConfig.GetOrCreate();

                CurrencyName = conf.CurrencyName;
                CurrencySign = conf.CurrencySign;
                CurrencyPluralName = conf.CurrencyPluralName;
            }
        }

        public static long GetCurrency(ulong id)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                return uow.Currency.GetUserCurrency(id);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Raffle(IUserMessage umsg, [Remainder] IRole role = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            role = role ?? channel.Guild.EveryoneRole;

            var members = role.Members().Where(u => u.Status != UserStatus.Offline && u.Status != UserStatus.Unknown);
            var membersArray = members as IUser[] ?? members.ToArray();
            var usr = membersArray[new NadekoRandom().Next(0, membersArray.Length)];
            await channel.SendConfirmAsync("🎟 Raffled user", $"**{usr.Username}#{usr.Discriminator}** ID: `{usr.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task Cash(IUserMessage umsg, [Remainder] IUser user = null)
        {
            var channel = umsg.Channel;

            user = user ?? umsg.Author;

            await channel.SendConfirmAsync($"{user.Username} has {GetCurrency(user.Id)} {CurrencySign}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task Cash(IUserMessage umsg, ulong userId)
        {
            var channel = umsg.Channel;

            await channel.SendConfirmAsync($"`{userId}` has {GetCurrency(userId)} {CurrencySign}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Give(IUserMessage umsg, long amount, [Remainder] IGuildUser receiver)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (amount <= 0 || umsg.Author.Id == receiver.Id)
                return;
            var success = await CurrencyHandler.RemoveCurrencyAsync((IGuildUser)umsg.Author, $"Gift to {receiver.Username} ({receiver.Id}).", amount, true).ConfigureAwait(false);
            if (!success)
            {
                await channel.SendErrorAsync($"{umsg.Author.Mention} You don't have enough {Gambling.CurrencyPluralName}.").ConfigureAwait(false);
                return;
            }
            await CurrencyHandler.AddCurrencyAsync(receiver, $"Gift from {umsg.Author.Username} ({umsg.Author.Id}).", amount, true).ConfigureAwait(false);
            await channel.SendConfirmAsync($"{umsg.Author.Mention} successfully sent {amount} {(amount == 1 ? Gambling.CurrencyName : Gambling.CurrencyPluralName)} to {receiver.Mention}!").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(2)]
        public Task Award(IUserMessage umsg, int amount, [Remainder] IGuildUser usr) =>
            Award(umsg, amount, usr.Id);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(1)]
        public async Task Award(IUserMessage umsg, int amount, ulong usrId)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (amount <= 0)
                return;

            await CurrencyHandler.AddCurrencyAsync(usrId, $"Awarded by bot owner. ({umsg.Author.Username}/{umsg.Author.Id})", amount).ConfigureAwait(false);

            await channel.SendConfirmAsync($"{umsg.Author.Mention} successfully awarded {amount} {(amount == 1 ? Gambling.CurrencyName : Gambling.CurrencyPluralName)} to <@{usrId}>!").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(0)]
        public async Task Award(IUserMessage umsg, int amount, [Remainder] IRole role)
        {
            var channel = (ITextChannel)umsg.Channel;
            var users = channel.Guild.GetUsers()
                               .Where(u => u.Roles.Contains(role))
                               .ToList();
            await Task.WhenAll(users.Select(u => CurrencyHandler.AddCurrencyAsync(u.Id,
                                                      $"Awarded by bot owner to **{role.Name}** role. ({umsg.Author.Username}/{umsg.Author.Id})",
                                                      amount)))
                         .ConfigureAwait(false);

            await channel.SendConfirmAsync($"Awarded `{amount}` {Gambling.CurrencyPluralName} to `{users.Count}` users from `{role.Name}` role.")
                         .ConfigureAwait(false);

        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Take(IUserMessage umsg, long amount, [Remainder] IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (amount <= 0)
                return;

            if(await CurrencyHandler.RemoveCurrencyAsync(user, $"Taken by bot owner.({umsg.Author.Username}/{umsg.Author.Id})", amount, true).ConfigureAwait(false))
                await channel.SendConfirmAsync($"{umsg.Author.Mention} successfully took {amount} {(amount == 1? Gambling.CurrencyName : Gambling.CurrencyPluralName)} from {user}!").ConfigureAwait(false);
            else
                await channel.SendErrorAsync($"{umsg.Author.Mention} was unable to take {amount} {(amount == 1 ? Gambling.CurrencyName : Gambling.CurrencyPluralName)} from {user} because the user doesn't have that much {Gambling.CurrencyPluralName}!").ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Take(IUserMessage umsg, long amount, [Remainder] ulong usrId)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (amount <= 0)
                return;

            if(await CurrencyHandler.RemoveCurrencyAsync(usrId, $"Taken by bot owner.({umsg.Author.Username}/{umsg.Author.Id})", amount).ConfigureAwait(false))
                await channel.SendConfirmAsync($"{umsg.Author.Mention} successfully took {amount} {(amount == 1 ? Gambling.CurrencyName : Gambling.CurrencyPluralName)} from <@{usrId}>!").ConfigureAwait(false);
            else
                await channel.SendErrorAsync($"{umsg.Author.Mention} was unable to take {amount} {(amount == 1 ? Gambling.CurrencyName : Gambling.CurrencyPluralName)} from `{usrId}` because the user doesn't have that much {Gambling.CurrencyPluralName}!").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task BetRoll(IUserMessage umsg, long amount)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (amount < 1)
                return;

            var guildUser = (IGuildUser)umsg.Author;

            long userFlowers;
            using (var uow = DbHandler.UnitOfWork())
            {
                userFlowers = uow.Currency.GetOrCreate(umsg.Author.Id).Amount;
            }

            if (userFlowers < amount)
            {
                await channel.SendErrorAsync($"{guildUser.Mention} You don't have enough {Gambling.CurrencyPluralName}. You only have {userFlowers}{Gambling.CurrencySign}.").ConfigureAwait(false);
                return;
            }

            await CurrencyHandler.RemoveCurrencyAsync(guildUser, "Betroll Gamble", amount, false).ConfigureAwait(false);

            var rng = new NadekoRandom().Next(0, 101);
            var str = $"{guildUser.Mention} `You rolled {rng}.` ";
            if (rng < 67)
            {
                str += "Better luck next time.";
            }
            else if (rng < 91)
            {
                str += $"Congratulations! You won {amount * 2}{Gambling.CurrencySign} for rolling above 66";
                await CurrencyHandler.AddCurrencyAsync(guildUser, "Betroll Gamble", amount * 2, false).ConfigureAwait(false);
            }
            else if (rng < 100)
            {
                str += $"Congratulations! You won {amount * 3}{Gambling.CurrencySign} for rolling above 90.";
                await CurrencyHandler.AddCurrencyAsync(guildUser, "Betroll Gamble", amount * 3, false).ConfigureAwait(false);
            }
            else
            {
                str += $"👑 Congratulations! You won {amount * 10}{Gambling.CurrencySign} for rolling **100**. 👑";
                await CurrencyHandler.AddCurrencyAsync(guildUser, "Betroll Gamble", amount * 10, false).ConfigureAwait(false);
            }

            await channel.SendConfirmAsync(str).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Leaderboard(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            IEnumerable<Currency> richest;
            using (var uow = DbHandler.UnitOfWork())
            {
                richest = uow.Currency.GetTopRichest(10);
            }
            if (!richest.Any())
                return;
            await channel.SendMessageAsync(
                richest.Aggregate(new StringBuilder(
$@"```xl
┏━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━┓
┃        Id           ┃  $$$   ┃
"),
                (cur, cs) => cur.AppendLine($@"┣━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━┫
┃{(channel.Guild.GetUser(cs.UserId)?.Username.TrimTo(18, true) ?? cs.UserId.ToString()),-20} ┃ {cs.Amount,6} ┃")
                        ).ToString() + "┗━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━┛```").ConfigureAwait(false);
        }
    }
}
