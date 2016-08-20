using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Classes;
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

               _client.MessageReceived += async (imsg) =>
                {
                    var channel = imsg.Channel as ITextChannel;

                    if (channel == null || await imsg.IsAuthor(_client))
                        return;
                    ConcurrentDictionary<ulong, DateTime> userTimePair;
                    if (!RatelimitingChannels.TryGetValue(channel.Id, out userTimePair)) return;
                    DateTime lastMessageTime;
                    if (userTimePair.TryGetValue(imsg.Author.Id, out lastMessageTime))
                    {
                        if (DateTime.Now - lastMessageTime < ratelimitTime)
                        {
                            try
                            {
                                await imsg.DeleteAsync().ConfigureAwait(false);
                            }
                            catch { }
                            return;
                        }
                    }
                    userTimePair.AddOrUpdate(imsg.Author.Id, id => DateTime.Now, (id, dt) => DateTime.Now);
                };
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Slowmode(IMessage imsg)
            {
                var channel = imsg.Channel as ITextChannel;

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