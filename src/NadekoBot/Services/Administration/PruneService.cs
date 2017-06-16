using Discord;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Administration
{
    public class PruneService
    {
        //channelids where prunes are currently occuring
        private ConcurrentHashSet<ulong> _pruningChannels = new ConcurrentHashSet<ulong>();
        private readonly TimeSpan twoWeeks = TimeSpan.FromDays(14);

        public async Task PruneWhere(ITextChannel channel, int amount, Func<IMessage, bool> predicate)
        {
            channel.ThrowIfNull(nameof(channel));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (!_pruningChannels.Add(channel.Id))
                return;

            try
            {
                IMessage[] msgs;
                IMessage lastMessage = null;
                msgs = (await channel.GetMessagesAsync(amount > 100 ? 100 : amount).Flatten()).Where(predicate).ToArray();
                while (amount > 0 && msgs.Any())
                {
                    lastMessage = msgs[msgs.Length - 1];

                    var bulkDeletable = new List<IMessage>();
                    var singleDeletable = new List<IMessage>();
                    foreach (var x in msgs)
                    {
                        if (DateTime.UtcNow - x.CreatedAt < twoWeeks)
                            bulkDeletable.Add(x);
                        else
                            singleDeletable.Add(x);
                    }

                    if (bulkDeletable.Count > 0)
                        await Task.WhenAll(Task.Delay(1000), channel.DeleteMessagesAsync(bulkDeletable)).ConfigureAwait(false);

                    var i = 0;
                    foreach (var group in singleDeletable.GroupBy(x => ++i / (singleDeletable.Count / 5)))
                        await Task.WhenAll(Task.Delay(1000), Task.WhenAll(group.Select(x => x.DeleteAsync()))).ConfigureAwait(false);

                    amount -= 100;
                    if(amount > 0)
                        msgs = (await channel.GetMessagesAsync(lastMessage, Direction.Before, amount > 100 ? 100 : amount).Flatten()).Where(predicate).ToArray();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _pruningChannels.TryRemove(channel.Id);
            }
        }
    }
}
