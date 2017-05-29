using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ModuleBehaviors
{
    /// <summary>
    /// Last thing to be executed, won't stop further executions
    /// </summary>
    public interface ILateExecutor
    {
        Task LateExecute(DiscordShardedClient client, IGuild guild, IUserMessage msg);
    }
}
