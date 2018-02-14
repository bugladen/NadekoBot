using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling.Common
{
    public interface ICurrencyEvent
    {
        Task Stop();
        Task Start();
    }
}
