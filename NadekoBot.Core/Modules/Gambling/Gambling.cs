using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using System;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Modules.Gambling.Common;
using Discord.WebSocket;
using NadekoBot.Core.Common;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling : GamblingTopLevelModule<GamblingService>
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly IDataCache _cache;
        private readonly DiscordSocketClient _client;

        private string CurrencyName => _bc.BotConfig.CurrencyName;
        private string CurrencyPluralName => _bc.BotConfig.CurrencyPluralName;
        private string CurrencySign => _bc.BotConfig.CurrencySign;

        public Gambling(DbService db, ICurrencyService currency,
            IDataCache cache, DiscordSocketClient client)
        {
            _db = db;
            _cs = currency;
            _cache = cache;
            _client = client;
        }

        public long GetCurrency(ulong id)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.DiscordUsers.GetUserCurrency(id);
            }
        }

        public long GetCurrency(IUser user)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.DiscordUsers.GetOrCreate(user).CurrencyAmount;
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Timely()
        {
            var val = _bc.BotConfig.TimelyCurrency;
            var period = _bc.BotConfig.TimelyCurrencyPeriod;
            if (val <= 0 || period <= 0)
            {
                await ReplyErrorLocalized("timely_none").ConfigureAwait(false);
                return;
            }

            TimeSpan? rem;
            if ((rem = _cache.AddTimelyClaim(Context.User.Id, period)) != null)
            {
                await ReplyErrorLocalized("timely_already_claimed", rem?.ToString(@"dd\d\ hh\h\ mm\m\ ss\s")).ConfigureAwait(false);
                return;
            }

            await _cs.AddAsync(Context.User.Id, "Timely claim", val).ConfigureAwait(false);

            await ReplyConfirmLocalized("timely", val + _bc.BotConfig.CurrencySign, period).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task TimelyReset()
        {
            _cache.RemoveAllTimelyClaims();
            await ReplyConfirmLocalized("timely_reset").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task TimelySet(int num, int period = 24)
        {
            if (num < 0 || period < 0)
                return;
            using (var uow = _db.UnitOfWork)
            {
                var bc = uow.BotConfig.GetOrCreate(set => set);
                bc.TimelyCurrency = num;
                bc.TimelyCurrencyPeriod = period;
                uow.Complete();
            }
            _bc.Reload();
            if (num == 0)
                await ReplyConfirmLocalized("timely_set_none").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("timely_set", Format.Bold(num + _bc.BotConfig.CurrencySign), Format.Bold(period.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Raffle([Remainder] IRole role = null)
        {
            role = role ?? Context.Guild.EveryoneRole;

            var members = (await role.GetMembersAsync()).Where(u => u.Status != UserStatus.Offline);
            var membersArray = members as IUser[] ?? members.ToArray();
            if (membersArray.Length == 0)
            {
                return;
            }
            var usr = membersArray[new NadekoRandom().Next(0, membersArray.Length)];
            await Context.Channel.SendConfirmAsync("🎟 " + GetText("raffled_user"), $"**{usr.Username}#{usr.Discriminator}**", footer: $"ID: {usr.Id}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RaffleAny([Remainder] IRole role = null)
        {
            role = role ?? Context.Guild.EveryoneRole;

            var members = (await role.GetMembersAsync());
            var membersArray = members as IUser[] ?? members.ToArray();
            if (membersArray.Length == 0)
            {
                return;
            }
            var usr = membersArray[new NadekoRandom().Next(0, membersArray.Length)];
            await Context.Channel.SendConfirmAsync("🎟 " + GetText("raffled_user"), $"**{usr.Username}#{usr.Discriminator}**", footer: $"ID: {usr.Id}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task Cash([Remainder] IUser user = null)
        {
            if (user == null)
                await ConfirmLocalized("has", Format.Bold(Context.User.ToString()), $"{GetCurrency(Context.User)} {CurrencySign}").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("has", Format.Bold(user.ToString()), $"{GetCurrency(user)} {CurrencySign}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(2)]
        public Task CurrencyTransactions(int page = 1) =>
            InternalCurrencyTransactions(Context.User.Id, page);

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        [Priority(0)]
        public Task CurrencyTransactions([Remainder] IUser usr) =>
            InternalCurrencyTransactions(usr.Id, 1);

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        [Priority(1)]
        public Task CurrencyTransactions(IUser usr, int page) =>
            InternalCurrencyTransactions(usr.Id, page);

        private async Task InternalCurrencyTransactions(ulong userId, int page)
        {
            if (--page < 0)
                return;

            var trs = new List<CurrencyTransaction>();
            using (var uow = _db.UnitOfWork)
            {
                trs = uow.CurrencyTransactions.GetPageFor(userId, page);
            }

            var embed = new EmbedBuilder()
                .WithTitle(GetText("transactions",
                    ((SocketGuild)Context.Guild)?.GetUser(userId)?.ToString() ?? $"{userId}"))
                .WithOkColor();

            var desc = "";
            foreach (var tr in trs)
            {
                var type = tr.Amount > 0 ? "🔵" : "🔴";
                var date = Format.Code($"〖{tr.DateAdded:HH:mm yyyy-MM-dd}〗");
                desc += $"\\{type} {date} {Format.Bold(tr.Amount.ToString())}\n\t{tr.Reason?.Trim()}\n";
            }

            embed.WithDescription(desc);
            embed.WithFooter(GetText("page", page + 1));
            await Context.Channel.EmbedAsync(embed);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task Cash(ulong userId)
        {
            await ReplyConfirmLocalized("has", Format.Code(userId.ToString()), $"{GetCurrency(userId)} {CurrencySign}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Give(ShmartNumber amount, IGuildUser receiver, [Remainder] string msg = null)
        {
            if (amount <= 0 || Context.User.Id == receiver.Id || receiver.IsBot)
                return;
            var success = await _cs.RemoveAsync((IGuildUser)Context.User, $"Gift to {receiver.Username} ({receiver.Id}).", amount, false).ConfigureAwait(false);
            if (!success)
            {
                await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
                return;
            }
            await _cs.AddAsync(receiver, $"Gift from {Context.User.Username} ({Context.User.Id}) - {msg}.", amount, true).ConfigureAwait(false);
            await ReplyConfirmLocalized("gifted", amount + CurrencySign, Format.Bold(receiver.ToString()), msg)
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task Give(ShmartNumber amount, [Remainder] IGuildUser receiver)
            => Give(amount, receiver, null);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(0)]
        public Task Award(ShmartNumber amount, IGuildUser usr, [Remainder] string msg) =>
            Award(amount, usr.Id, msg);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(1)]
        public Task Award(ShmartNumber amount, [Remainder] IGuildUser usr) =>
            Award(amount, usr.Id);

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        [Priority(2)]
        public async Task Award(ShmartNumber amount, ulong usrId, [Remainder] string msg = null)
        {
            if (amount <= 0)
                return;

            await _cs.AddAsync(usrId,
                $"Awarded by bot owner. ({Context.User.Username}/{Context.User.Id}) {(msg ?? "")}",
                amount,
                gamble: (Context.Client.CurrentUser.Id != usrId)).ConfigureAwait(false);
            await ReplyConfirmLocalized("awarded", amount + CurrencySign, $"<@{usrId}>").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(2)]
        public async Task Award(ShmartNumber amount, [Remainder] IRole role)
        {
            var users = (await Context.Guild.GetUsersAsync())
                               .Where(u => u.GetRoles().Contains(role))
                               .ToList();

            await _cs.AddBulkAsync(users.Select(x => x.Id),
                users.Select(x => $"Awarded by bot owner to **{role.Name}** role. ({Context.User.Username}/{Context.User.Id})"),
                users.Select(x => amount.Value),
                gamble: true)
                .ConfigureAwait(false);

            await ReplyConfirmLocalized("mass_award",
                amount + CurrencySign,
                Format.Bold(users.Count.ToString()),
                Format.Bold(role.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Take(ShmartNumber amount, [Remainder] IGuildUser user)
        {
            if (amount <= 0)
                return;

            if (await _cs.RemoveAsync(user, $"Taken by bot owner.({Context.User.Username}/{Context.User.Id})", amount,
                gamble: (Context.Client.CurrentUser.Id != user.Id)).ConfigureAwait(false))
                await ReplyConfirmLocalized("take", amount + CurrencySign, Format.Bold(user.ToString())).ConfigureAwait(false);
            else
                await ReplyErrorLocalized("take_fail", amount + CurrencySign, Format.Bold(user.ToString()), CurrencyPluralName).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task Take(ShmartNumber amount, [Remainder] ulong usrId)
        {
            if (amount <= 0)
                return;

            if (await _cs.RemoveAsync(usrId, $"Taken by bot owner.({Context.User.Username}/{Context.User.Id})", amount,
                gamble: (Context.Client.CurrentUser.Id != usrId)))
                await ReplyConfirmLocalized("take", amount + CurrencySign, $"<@{usrId}>").ConfigureAwait(false);
            else
                await ReplyErrorLocalized("take_fail", amount + CurrencySign, Format.Code(usrId.ToString()), CurrencyPluralName).ConfigureAwait(false);
        }

        IUserMessage rdMsg = null;

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RollDuel(IUser u)
        {
            if (Context.User.Id == u.Id)
                return;

            //since the challenge is created by another user, we need to reverse the ids
            //if it gets removed, means challenge is accepted
            if (_service.Duels.TryRemove((Context.User.Id, u.Id), out var game))
            {
                await game.StartGame().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RollDuel(ShmartNumber amount, IUser u)
        {
            if (Context.User.Id == u.Id)
                return;

            if (amount <= 0)
                return;

            var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("roll_duel"));

            var game = new RollDuelGame(_cs, _client.CurrentUser.Id, Context.User.Id, u.Id, amount);
            //means challenge is just created
            if (_service.Duels.TryGetValue((Context.User.Id, u.Id), out var other))
            {
                if (other.Amount != amount)
                {
                    await ReplyErrorLocalized("roll_duel_already_challenged").ConfigureAwait(false);
                }
                else
                {
                    await RollDuel(u).ConfigureAwait(false);
                }
                return;
            }
            if (_service.Duels.TryAdd((u.Id, Context.User.Id), game))
            {
                game.OnGameTick += Game_OnGameTick;
                game.OnEnded += Game_OnEnded;

                await ReplyConfirmLocalized("roll_duel_challenge",
                    Format.Bold(Context.User.ToString()),
                    Format.Bold(u.ToString()),
                    Format.Bold(amount + CurrencySign))
                        .ConfigureAwait(false);
            }

            async Task Game_OnGameTick(RollDuelGame arg)
            {
                var rolls = arg.Rolls.Last();
                embed.Description += $@"{Format.Bold(Context.User.ToString())} rolled **{rolls.Item1}**
{Format.Bold(u.ToString())} rolled **{rolls.Item2}**
--
";

                if (rdMsg == null)
                {
                    rdMsg = await Context.Channel.EmbedAsync(embed)
                        .ConfigureAwait(false);
                }
                else
                {
                    await rdMsg.ModifyAsync(x =>
                    {
                        x.Embed = embed.Build();
                    }).ConfigureAwait(false);
                }
            }

            async Task Game_OnEnded(RollDuelGame rdGame, RollDuelGame.Reason reason)
            {
                try
                {
                    if (reason == RollDuelGame.Reason.Normal)
                    {
                        var winner = rdGame.Winner == rdGame.P1
                            ? Context.User
                            : u;
                        embed.Description += $"\n**{winner}** Won {((long)(rdGame.Amount * 2 * 0.98)) + CurrencySign}";
                        await rdMsg.ModifyAsync(x => x.Embed = embed.Build())
                            .ConfigureAwait(false);
                    }
                    else if (reason == RollDuelGame.Reason.Timeout)
                    {
                        await ReplyErrorLocalized("roll_duel_timeout").ConfigureAwait(false);
                    }
                    else if (reason == RollDuelGame.Reason.NoFunds)
                    {
                        await ReplyErrorLocalized("roll_duel_no_funds").ConfigureAwait(false);
                    }
                }
                finally
                {
                    _service.Duels.TryRemove((u.Id, Context.User.Id), out var _);
                }
            }
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[OwnerOnly]
        //public Task BrTest(int tests = 1000)
        //{
        //    var t = Task.Run(async () =>
        //    {
        //        if (tests <= 0)
        //            return;
        //        //multi vs how many times it occured
        //        var dict = new Dictionary<int, int>();
        //        var generator = new NadekoRandom();
        //        for (int i = 0; i < tests; i++)
        //        {
        //            var rng = generator.Next(0, 101);
        //            var mult = 0;
        //            if (rng < 67)
        //            {
        //                mult = 0;
        //            }
        //            else if (rng < 91)
        //            {
        //                mult = 2;
        //            }
        //            else if (rng < 100)
        //            {
        //                mult = 4;
        //            }
        //            else
        //                mult = 10;

        //            if (dict.ContainsKey(mult))
        //                dict[mult] += 1;
        //            else
        //                dict.Add(mult, 1);
        //        }

        //        var sb = new StringBuilder();
        //        const int bet = 1;
        //        int payout = 0;
        //        foreach (var key in dict.Keys.OrderByDescending(x => x))
        //        {
        //            sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
        //            payout += key * dict[key];
        //        }
        //        try
        //        {
        //            await Context.Channel.SendConfirmAsync("BetRoll Test Results", sb.ToString(),
        //                footer: $"Total Bet: {tests * bet} | Payout: {payout * bet} | {payout * 1.0f / tests * 100}%");
        //        }
        //        catch { }

        //    });
        //    return Task.CompletedTask;
        //}

        private async Task InternallBetroll(long amount)
        {
            if (!await CheckBetMandatory(amount))
                return;

            if (!await _cs.RemoveAsync(Context.User, "Betroll Gamble", amount, false, gamble: true).ConfigureAwait(false))
            {
                await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
                return;
            }

            var rnd = new NadekoRandom().Next(0, 101);
            var str = Context.User.Mention + Format.Code(GetText("roll", rnd));
            if (rnd < 67)
            {
                str += GetText("better_luck");
            }
            else
            {
                if (rnd < 91)
                {
                    str += GetText("br_win", (amount * _bc.BotConfig.Betroll67Multiplier) + CurrencySign, 66);
                    await _cs.AddAsync(Context.User, "Betroll Gamble",
                        (int)(amount * _bc.BotConfig.Betroll67Multiplier), false, gamble: true).ConfigureAwait(false);
                }
                else if (rnd < 100)
                {
                    str += GetText("br_win", (amount * _bc.BotConfig.Betroll91Multiplier) + CurrencySign, 90);
                    await _cs.AddAsync(Context.User, "Betroll Gamble",
                        (int)(amount * _bc.BotConfig.Betroll91Multiplier), false, gamble: true).ConfigureAwait(false);
                }
                else
                {
                    str += GetText("br_win", (amount * _bc.BotConfig.Betroll100Multiplier) + CurrencySign, 99) + " 👑";
                    await _cs.AddAsync(Context.User, "Betroll Gamble",
                        (int)(amount * _bc.BotConfig.Betroll100Multiplier), false, gamble: true).ConfigureAwait(false);
                }
            }
            await Context.Channel.SendConfirmAsync(str).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public Task BetRoll(ShmartNumber amount)
            => InternallBetroll(amount);

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Leaderboard(int page = 1)
        {
            if (page < 1)
                return;

            List<DiscordUser> richest;
            using (var uow = _db.UnitOfWork)
            {
                richest = uow.DiscordUsers.GetTopRichest(_client.CurrentUser.Id, 9, 9 * (page - 1)).ToList();
            }

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(CurrencySign +" " + GetText("leaderboard"))
                .WithFooter(efb => efb.WithText(GetText("page", page)));

            if (!richest.Any())
            {
                embed.WithDescription(GetText("no_users_found"));
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                return;
            }

            for (var i = 0; i < richest.Count; i++)
            {
                var x = richest[i];
                var usrStr = x.ToString().TrimTo(20, true);

                var j = i;
                embed.AddField(efb => efb.WithName("#" + (9 * (page - 1) + j + 1) + " " + usrStr)
                                         .WithValue(x.CurrencyAmount.ToString() + " " + CurrencySign)
                                         .WithIsInline(true));
            }

            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }


        public enum RpsPick
        {
            R = 0,
            Rock = 0,
            Rocket = 0,
            P = 1,
            Paper = 1,
            Paperclip = 1,
            S = 2,
            Scissors = 2
        }

        public enum RpsResult
        {
            Win,
            Loss,
            Draw,
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rps(RpsPick pick, ShmartNumber amount = default)
        {
            long oldAmount = amount;
            if (!await CheckBetOptional(amount) || (amount == 1))
                return;

            string getRpsPick(RpsPick p)
            {
                switch (p)
                {
                    case RpsPick.R:
                        return "🚀";
                    case RpsPick.P:
                        return "📎";
                    default:
                        return "✂️";
                }
            }
            var embed = new EmbedBuilder();

            var nadekoPick = (RpsPick)new NadekoRandom().Next(0, 3);

            if (amount > 0)
            {
                if (!await _cs.RemoveAsync(Context.User.Id,
                    "Rps-bet", amount, gamble: true))
                {
                    await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
            }

            string msg;
            if (pick == nadekoPick)
            {
                await _cs.AddAsync(Context.User.Id,
                    "Rps-draw", amount, gamble: true);
                embed.WithOkColor();
                msg = GetText("rps_draw", getRpsPick(pick));
            }
            else if ((pick == RpsPick.Paper && nadekoPick == RpsPick.Rock) ||
                     (pick == RpsPick.Rock && nadekoPick == RpsPick.Scissors) ||
                     (pick == RpsPick.Scissors && nadekoPick == RpsPick.Paper))
            {
                amount = (long)(amount * _bc.BotConfig.BetflipMultiplier);
                await _cs.AddAsync(Context.User.Id,
                    "Rps-win", amount, gamble: true);
                embed.WithOkColor();
                embed.AddField(GetText("won"), amount);
                msg = GetText("rps_win", Context.User.Mention,
                    getRpsPick(pick), getRpsPick(nadekoPick));
            }
            else
            {
                embed.WithErrorColor();
                amount = 0;
                msg = GetText("rps_win", Context.Client.CurrentUser.Mention, getRpsPick(nadekoPick),
                    getRpsPick(pick));
            }

            embed
                .WithDescription(msg);

            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }
    }
}
