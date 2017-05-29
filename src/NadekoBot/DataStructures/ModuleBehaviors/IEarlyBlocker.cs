using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ModuleBehaviors
{
    /// <summary>
    /// Implemented by modules which block execution before anything is executed
    /// </summary>
    public interface IEarlyBlocker
    {
        Task<bool> TryBlockEarly(IGuild guild, IUserMessage msg);
    }
}
