using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Common.WheelOfFortune;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using Wof = NadekoBot.Modules.Gambling.Common.WheelOfFortune.WheelOfFortune;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class WheelOfFortuneCommands : NadekoSubmodule
        {
            private readonly CurrencyService _cs;
            private readonly IBotConfigProvider _bc;
            private readonly DbService _db;

            public WheelOfFortuneCommands(CurrencyService cs, IBotConfigProvider bc,
                DbService db)
            {
                _cs = cs;
                _bc = bc;
                _db = db;
            }

            public enum Allin { Allin = int.MinValue, All = int.MinValue }

            [NadekoCommand, Usage, Description, Aliases]
            public Task WheelOfFortune(Allin _)
            {
                long cur;
                using (var uow = _db.UnitOfWork)
                {
                    cur = uow.DiscordUsers.GetUserCurrency(Context.User.Id);
                }
                return WheelOfFortune(cur);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task WheelOfFortune(long bet)
            {
                const int minBet = 10;
                if (bet < minBet)
                {
                    await ReplyErrorLocalized("min_bet_limit", minBet + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                if (!_cs.Remove(Context.User.Id, "Wheel Of Fortune - bet", bet, gamble: true))
                {
                    await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                var wof = new WheelOfFortune();

                var amount = (int)(bet * wof.Multiplier);

                if (amount > 0)
                    await _cs.AddAsync(Context.User.Id, "Wheel Of Fortune - won", amount, gamble: true).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync(
Format.Bold($@"{Context.User.ToString()} won: {amount + _bc.BotConfig.CurrencySign}

   『{Wof.Multipliers[1]}』   『{Wof.Multipliers[0]}』   『{Wof.Multipliers[7]}』

『{Wof.Multipliers[2]}』      {wof.Emoji}      『{Wof.Multipliers[6]}』

     『{Wof.Multipliers[3]}』   『{Wof.Multipliers[4]}』   『{Wof.Multipliers[5]}』")).ConfigureAwait(false);
            }

            //[NadekoCommand, Usage, Description, Aliases]
            //[RequireContext(ContextType.Guild)]
            //public async Task WofTest(int length = 1000)
            //{
            //    var mults = new Dictionary<float, int>();
            //    for (int i = 0; i < length; i++)
            //    {
            //        var x = new Wof();
            //        if (mults.ContainsKey(x.Multiplier))
            //            ++mults[x.Multiplier];
            //        else
            //            mults.Add(x.Multiplier, 1);
            //    }

            //    var payout = mults.Sum(x => x.Key * x.Value);
            //    await Context.Channel.SendMessageAsync($"Total bet: {length}\n" +
            //        $"Paid out: {payout}\n" +
            //        $"Total Payout: {payout / length:F3}x")
            //        .ConfigureAwait(false);
            //}
        }
    }
}