using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Services.Permissions
{
    public class FilterService
    {
        public ConcurrentHashSet<ulong> InviteFilteringChannels { get; }
        public ConcurrentHashSet<ulong> InviteFilteringServers { get; }

        //serverid, filteredwords
        public ConcurrentDictionary<ulong, ConcurrentHashSet<string>> ServerFilteredWords { get; }

        public ConcurrentHashSet<ulong> WordFilteringChannels { get; }
        public ConcurrentHashSet<ulong> WordFilteringServers { get; }

        public ConcurrentHashSet<string> FilteredWordsForChannel(ulong channelId, ulong guildId)
        {
            ConcurrentHashSet<string> words = new ConcurrentHashSet<string>();
            if (WordFilteringChannels.Contains(channelId))
                ServerFilteredWords.TryGetValue(guildId, out words);
            return words;
        }

        public ConcurrentHashSet<string> FilteredWordsForServer(ulong guildId)
        {
            var words = new ConcurrentHashSet<string>();
            if (WordFilteringServers.Contains(guildId))
                ServerFilteredWords.TryGetValue(guildId, out words);
            return words;
        }

        public FilterService(IEnumerable<GuildConfig> gcs)
        {
            InviteFilteringServers = new ConcurrentHashSet<ulong>(gcs.Where(gc => gc.FilterInvites).Select(gc => gc.GuildId));
            InviteFilteringChannels = new ConcurrentHashSet<ulong>(gcs.SelectMany(gc => gc.FilterInvitesChannelIds.Select(fci => fci.ChannelId)));

            var dict = gcs.ToDictionary(gc => gc.GuildId, gc => new ConcurrentHashSet<string>(gc.FilteredWords.Select(fw => fw.Word)));

            ServerFilteredWords = new ConcurrentDictionary<ulong, ConcurrentHashSet<string>>(dict);

            var serverFiltering = gcs.Where(gc => gc.FilterWords);
            WordFilteringServers = new ConcurrentHashSet<ulong>(serverFiltering.Select(gc => gc.GuildId));

            WordFilteringChannels = new ConcurrentHashSet<ulong>(gcs.SelectMany(gc => gc.FilterWordsChannelIds.Select(fwci => fwci.ChannelId)));
        }
    }
}
