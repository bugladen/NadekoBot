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
    public class UtilityService
    {
        public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; } = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>();

        public UtilityService(IEnumerable<GuildConfig> guildConfigs, DiscordShardedClient client)
        {
            //commandmap
            AliasMaps = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>(
                    guildConfigs.ToDictionary(
                        x => x.GuildId,
                        x => new ConcurrentDictionary<string, string>(x.CommandAliases
                            .Distinct(new CommandAliasEqualityComparer())
                            .ToDictionary(ca => ca.Trigger, ca => ca.Mapping))));

            //cross server
            _client = client;
            _client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage imsg)
        {
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
        }

        private string GetMessage(ITextChannel channel, IGuildUser user, IUserMessage message) =>
            $"**{channel.Guild.Name} | {channel.Name}** `{user.Username}`: " + message.Content.SanitizeMentions();

        public readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers =
            new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();
        private DiscordShardedClient _client;
    }

    public class CommandAliasEqualityComparer : IEqualityComparer<CommandAlias>
    {
        public bool Equals(CommandAlias x, CommandAlias y) => x.Trigger == y.Trigger;

        public int GetHashCode(CommandAlias obj) => obj.Trigger.GetHashCode();
    }
}
