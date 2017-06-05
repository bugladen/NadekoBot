using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Services.Utility
{
    public class CrossServerTextService
    {
        public readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers =
            new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();
        private DiscordShardedClient _client;

        public CrossServerTextService(IEnumerable<GuildConfig> guildConfigs, DiscordShardedClient client)
        {
            _client = client;
            _client.MessageReceived += Client_MessageReceived;
        }

        private Task Client_MessageReceived(SocketMessage imsg)
        {
            var _ = Task.Run(async () => {
                try
                {
                    if (imsg.Author.IsBot)
                        return;
                    var msg = imsg as IUserMessage;
                    if (msg == null)
                        return;
                    var channel = imsg.Channel as ITextChannel;
                    if (channel == null)
                        return;
                    if (msg.Author.Id == _client.CurrentUser.Id) return;
                    foreach (var subscriber in Subscribers)
                    {
                        var set = subscriber.Value;
                        if (!set.Contains(channel))
                            continue;
                        foreach (var chan in set.Except(new[] { channel }))
                        {
                            try
                            {
                                await chan.SendMessageAsync(GetMessage(channel, (IGuildUser)msg.Author,
                                    msg)).ConfigureAwait(false);
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            });

            return Task.CompletedTask;
        }

        private string GetMessage(ITextChannel channel, IGuildUser user, IUserMessage message) =>
            $"**{channel.Guild.Name} | {channel.Name}** `{user.Username}`: " + message.Content.SanitizeMentions();
    }
}
