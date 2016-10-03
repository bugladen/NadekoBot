using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class FilterCommands
        {
            public static HashSet<ulong> InviteFilteringChannels { get; set; }
            public static HashSet<ulong> InviteFilteringServers { get; set; }

            //serverid, filteredwords
            private static ConcurrentDictionary<ulong, HashSet<string>> ServerFilteredWords { get; set; }

            public static HashSet<ulong> WordFilteringChannels { get; set; }
            public static HashSet<ulong> WordFilteringServers { get; set; }

            public static HashSet<string> FilteredWordsForChannel(ulong channelId, ulong guildId)
            {
                var words = FilteredWordsForServer(guildId);

                if (!words.Any() || WordFilteringChannels.Contains(channelId))
                    return words;
                return new HashSet<string>();
            }

            public static HashSet<string> FilteredWordsForServer(ulong guildId)
            {
                var words = new HashSet<string>();
                ServerFilteredWords.TryGetValue(guildId, out words);
                return words;
            }

            static FilterCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var guildConfigs = uow.GuildConfigs.GetAll();

                    InviteFilteringServers = new HashSet<ulong>(guildConfigs.Where(gc => gc.FilterInvites).Select(gc => gc.GuildId));
                    InviteFilteringChannels = new HashSet<ulong>(guildConfigs.SelectMany(gc => gc.FilterInvitesChannelIds.Select(fci => fci.ChannelId)));

                    var dict = guildConfigs.ToDictionary(gc => gc.GuildId, gc => new HashSet<string>(gc.FilteredWords.Select(fw => fw.Word)));

                    ServerFilteredWords = new ConcurrentDictionary<ulong, HashSet<string>>(dict);

                    var serverFiltering = guildConfigs.Where(gc => gc.FilterWords);
                    WordFilteringServers = new HashSet<ulong>(serverFiltering.Select(gc => gc.GuildId));

                    WordFilteringChannels = new HashSet<ulong>(guildConfigs.SelectMany(gc => gc.FilterWordsChannelIds.Select(fwci => fwci.ChannelId)));

                }
            }

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task SrvrFilterInv(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    enabled = config.FilterInvites = !config.FilterInvites;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (enabled)
                {
                    InviteFilteringServers.Add(channel.Guild.Id);
                    await channel.SendMessageAsync("`Invite filtering enabled on the whole server.`").ConfigureAwait(false);
                }
                else
                {
                    InviteFilteringServers.Remove(channel.Guild.Id);
                    await channel.SendMessageAsync("`Invite filtering disabled on the whole server.`").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task ChnlFilterInv(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    removed = config.FilterInvitesChannelIds.RemoveWhere(fc => fc.ChannelId == channel.Id);
                    if (removed == 0)
                    {
                        config.FilterInvitesChannelIds.Add(new Services.Database.Models.FilterChannelId()
                        {
                            ChannelId = channel.Id
                        });
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (removed == 0)
                {
                    InviteFilteringChannels.Add(channel.Id);
                    await channel.SendMessageAsync("`Invite filtering enabled on this channel.`").ConfigureAwait(false);
                }
                else
                {
                    InviteFilteringChannels.Remove(channel.Id);
                    await channel.SendMessageAsync("`Invite filtering disabled on this channel.`").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task SrvrFilterWords(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    enabled = config.FilterWords = !config.FilterWords;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (enabled)
                {
                    WordFilteringServers.Add(channel.Guild.Id);
                    await channel.SendMessageAsync("`Word filtering enabled on the whole server.`").ConfigureAwait(false);
                }
                else
                {
                    WordFilteringServers.Remove(channel.Guild.Id);
                    await channel.SendMessageAsync("`Word filtering disabled on the whole server.`").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task ChnlFilterWords(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    removed = config.FilterWordsChannelIds.RemoveWhere(fc => fc.ChannelId == channel.Id);
                    if (removed == 0)
                    {
                        config.FilterWordsChannelIds.Add(new Services.Database.Models.FilterChannelId()
                        {
                            ChannelId = channel.Id
                        });
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (removed == 0)
                {
                    WordFilteringChannels.Add(channel.Id);
                    await channel.SendMessageAsync("`Word filtering enabled on this channel.`").ConfigureAwait(false);
                }
                else
                {
                    WordFilteringChannels.Remove(channel.Id);
                    await channel.SendMessageAsync("`Word filtering disabled on this channel.`").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task AddFilterWord(IUserMessage imsg, [Remainder] string word)
            {
                var channel = (ITextChannel)imsg.Channel;

                word = word?.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(word))
                    return;

                bool contains;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);

                    contains = config.FilteredWords.Any(fw => fw.Word == word);

                    if (!contains)
                        config.FilteredWords.Add(new Services.Database.Models.FilteredWord() { Word = word});
                }

                if (!contains)
                {
                    var filteredWords = ServerFilteredWords.GetOrAdd(channel.Guild.Id, new HashSet<string>());

                    filteredWords.Add(word);
                    await channel.SendMessageAsync($"Word `{word}` successfully added to the list of filtered words.");
                }
            }

        }
    }
}
