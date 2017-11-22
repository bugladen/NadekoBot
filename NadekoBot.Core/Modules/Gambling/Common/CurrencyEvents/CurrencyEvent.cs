using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling.Common
{
    public abstract class CurrencyEvent
    {
        public abstract Task Stop();
        public abstract Task Start(IUserMessage msg, ICommandContext channel);
    }
}
