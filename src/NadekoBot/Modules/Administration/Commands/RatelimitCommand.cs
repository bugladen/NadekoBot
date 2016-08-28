using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

//todo rewrite to accept msg/sec (for example 1/5 - 1 message every 5 seconds)
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class RatelimitCommand
        {
            public static ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, DateTime>> RatelimitingChannels = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, DateTime>>();

            private static readonly TimeSpan ratelimitTime = new TimeSpan(0, 0, 0, 5);
            private DiscordSocketClient _client { get; }

            public RatelimitCommand()
            {

                this._client = NadekoBot.Client;

               _client.MessageReceived += async (umsg) =>
                {
                    var usrMsg = umsg as IUserMessage;
                    var channel = usrMsg.Channel as ITextChannel;

                    if (channel == null || await usrMsg.IsAuthor())
                        return;
                    ConcurrentDictionary<ulong, DateTime> userTimePair;
                    if (!RatelimitingChannels.TryGetValue(channel.Id, out userTimePair)) return;
                    DateTime lastMessageTime;
                    if (userTimePair.TryGetValue(usrMsg.Author.Id, out lastMessageTime))
                    {
                        if (DateTime.Now - lastMessageTime < ratelimitTime)
                        {
                            try
                            {
                                await usrMsg.DeleteAsync().ConfigureAwait(false);
                            }
                            catch { }
                            return;
                        }
                    }
                    userTimePair.AddOrUpdate(usrMsg.Author.Id, id => DateTime.Now, (id, dt) => DateTime.Now);
                };
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Slowmode(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                ConcurrentDictionary<ulong, DateTime> throwaway;
                if (RatelimitingChannels.TryRemove(channel.Id, out throwaway))
                {
                    await channel.SendMessageAsync("Slow mode disabled.").ConfigureAwait(false);
                    return;
                }
                if (RatelimitingChannels.TryAdd(channel.Id, new ConcurrentDictionary<ulong, DateTime>()))
                {
                    await channel.SendMessageAsync("Slow mode initiated. " +
                                                "Users can't send more than 1 message every 5 seconds.")
                                                .ConfigureAwait(false);
                }
            }
        }
    }
}