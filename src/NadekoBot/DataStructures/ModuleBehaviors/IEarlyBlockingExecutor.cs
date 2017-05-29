using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ModuleBehaviors
{
    /// <summary>
    /// Implemented by modules which can execute something and prevent further commands from being executed.
    /// </summary>
    public interface IEarlyBlockingExecutor
    {
        /// <summary>
        /// Try to execute some logic within some module's service.
        /// </summary>
        /// <returns>Whether it should block other command executions after it.</returns>
        Task<bool> TryExecuteEarly(DiscordShardedClient client, IGuild guild, IUserMessage msg);
    }
}
