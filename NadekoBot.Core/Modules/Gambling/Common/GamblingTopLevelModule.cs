using Discord;
using NadekoBot.Core.Services;
using NadekoBot.Modules;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Gambling.Common
{
    public abstract class GamblingTopLevelModule<TService> : NadekoTopLevelModule<TService> where TService: INService
    {
        public GamblingTopLevelModule(bool isTopLevel = true) : base(isTopLevel)
        {
        }

        private async Task<bool> InternalCheckBet(long amount)
        {
            if (amount < 1)
            {
                return false;
            }
            if (amount < _bc.BotConfig.MinBet)
            {
                await ReplyErrorLocalized("min_bet_limit", Format.Bold(_bc.BotConfig.MinBet.ToString()) + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                return false;
            }
            if (_bc.BotConfig.MaxBet > 0 && amount > _bc.BotConfig.MaxBet)
            {
                await ReplyErrorLocalized("max_bet_limit", Format.Bold(_bc.BotConfig.MaxBet.ToString()) + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                return false;
            }
            return true;
        }

        protected Task<bool> CheckBetMandatory(long amount)
        {
            if (amount < 1)
            {
                return Task.FromResult(false);
            }
            return InternalCheckBet(amount);
        }

        protected Task<bool> CheckBetOptional(long amount)
        {
            if (amount == 0)
            {
                return Task.FromResult(true);
            }
            return InternalCheckBet(amount);
        }
    }

    public abstract class GamblingSubmodule<TService> : GamblingTopLevelModule<TService> where TService : INService
    {
        public GamblingSubmodule() : base(false)
        {
        }
    }
}
