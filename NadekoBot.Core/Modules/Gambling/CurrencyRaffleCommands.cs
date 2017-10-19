using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Gambling.Services;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class CurrencyRaffleCommands : NadekoSubmodule<CurrencyRaffleService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            public async Task RaffleCur(int amount)
            {
                if (_service.Games.TryAdd(Context.Channel.Id,
                    ))
                {

                }
            }
        }
    }
}
