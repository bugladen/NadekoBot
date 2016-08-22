//using Discord;
//using Discord.Commands;
//using Discord.WebSocket;
//using NadekoBot.Attributes;
//using System.Linq;
//using System.Threading.Tasks;

////todo owner only
//namespace NadekoBot.Modules.Administration
//{
//    public partial class Administration
//    {
//        [Group]
//        class SelfCommands
//        {
//            private DiscordSocketClient _client;

//            public SelfCommands(DiscordSocketClient client)
//            {
//                this._client = client;
//            }

//            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
//            [RequireContext(ContextType.Guild)]
//            public async Task Leave(IMessage imsg, [Remainder] string guildStr)
//            {
//                var channel = (ITextChannel)imsg.Channel;

//                guildStr = guildStr.ToUpperInvariant();
//                var server = _client.GetGuilds().FirstOrDefault(g => g.Id.ToString() == guildStr) ?? _client.GetGuilds().FirstOrDefault(g => g.Name.ToUpperInvariant() == guildStr);

//                if (server == null)
//                {
//                    await channel.SendMessageAsync("Cannot find that server").ConfigureAwait(false);
//                    return;
//                }
//                if (server.OwnerId != _client.GetCurrentUser().Id)
//                {
//                    await server.LeaveAsync().ConfigureAwait(false);
//                    await channel.SendMessageAsync("Left server " + server.Name).ConfigureAwait(false);
//                }
//                else
//                {
//                    await server.DeleteAsync().ConfigureAwait(false);
//                    await channel.SendMessageAsync("Deleted server " + server.Name).ConfigureAwait(false);
//                }
//            }
//        }
//    }
//}