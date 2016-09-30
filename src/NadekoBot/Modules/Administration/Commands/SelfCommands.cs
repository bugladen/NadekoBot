using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        class SelfCommands
        {
            private DiscordSocketClient _client;

            public SelfCommands(DiscordSocketClient client)
            {
                this._client = client;
            }

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Leave(IUserMessage umsg, [Remainder] string guildStr)
            {
                var channel = (ITextChannel)umsg.Channel;

                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = _client.GetGuilds().FirstOrDefault(g => g.Id.ToString().Trim().ToUpperInvariant() == guildStr) ?? 
                    _client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await channel.SendMessageAsync("Cannot find that server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != _client.GetCurrentUser().Id)
                {
                    await server.LeaveAsync().ConfigureAwait(false);
                    await channel.SendMessageAsync("Left server " + server.Name).ConfigureAwait(false);
                }
                else
                {
                    await server.DeleteAsync().ConfigureAwait(false);
                    await channel.SendMessageAsync("Deleted server " + server.Name).ConfigureAwait(false);
                }
            }
        }
    }
}