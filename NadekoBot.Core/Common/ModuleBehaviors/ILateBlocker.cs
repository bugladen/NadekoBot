using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace NadekoBot.Common.ModuleBehaviors
{
    public interface ILateBlocker
    {
        Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage msg, IGuild guild, 
            IMessageChannel channel, IUser user, string moduleName, string commandName);
    }
}
