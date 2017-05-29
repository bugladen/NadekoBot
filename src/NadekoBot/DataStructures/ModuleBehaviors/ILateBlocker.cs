using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ModuleBehaviors
{
    public interface ILateBlocker
    {
        Task<bool> TryBlockLate(DiscordShardedClient client, IUserMessage msg, IGuild guild, 
            IMessageChannel channel, IUser user, string moduleName, string commandName);
    }
}
