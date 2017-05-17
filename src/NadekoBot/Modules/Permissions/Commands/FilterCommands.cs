using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class FilterCommands : NadekoSubmodule
        {
            public static ConcurrentHashSet<ulong> InviteFilteringChannels { get; }
            public static ConcurrentHashSet<ulong> InviteFilteringServers { get; }

            //serverid, filteredwords
            private static ConcurrentDictionary<ulong, ConcurrentHashSet<string>> serverFilteredWords { get; }

            public static ConcurrentHashSet<ulong> WordFilteringChannels { get; }
            public static ConcurrentHashSet<ulong> WordFilteringServers { get; }

            public static ConcurrentHashSet<string> FilteredWordsForChannel(ulong channelId, ulong guildId)
            {
                ConcurrentHashSet<string> words = new ConcurrentHashSet<string>();
                if(WordFilteringChannels.Contains(channelId))
                    serverFilteredWords.TryGetValue(guildId, out words);
                return words;
            }

            public static ConcurrentHashSet<string> FilteredWordsForServer(ulong guildId)
            {
                var words = new ConcurrentHashSet<string>();
                if(WordFilteringServers.Contains(guildId))
                    serverFilteredWords.TryGetValue(guildId, out words);
                return words;
            }

            static FilterCommands()
            {
                var guildConfigs = NadekoBot.AllGuildConfigs;

                InviteFilteringServers = new ConcurrentHashSet<ulong>(guildConfigs.Where(gc => gc.FilterInvites).Select(gc => gc.GuildId));
                InviteFilteringChannels = new ConcurrentHashSet<ulong>(guildConfigs.SelectMany(gc => gc.FilterInvitesChannelIds.Select(fci => fci.ChannelId)));

                var dict = guildConfigs.ToDictionary(gc => gc.GuildId, gc => new ConcurrentHashSet<string>(gc.FilteredWords.Select(fw => fw.Word)));

                serverFilteredWords = new ConcurrentDictionary<ulong, ConcurrentHashSet<string>>(dict);

                var serverFiltering = guildConfigs.Where(gc => gc.FilterWords);
                WordFilteringServers = new ConcurrentHashSet<ulong>(serverFiltering.Select(gc => gc.GuildId));

                WordFilteringChannels = new ConcurrentHashSet<ulong>(guildConfigs.SelectMany(gc => gc.FilterWordsChannelIds.Select(fwci => fwci.ChannelId)));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task SrvrFilterInv()
            {
                var channel = (ITextChannel)Context.Channel;

                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id, set => set);
                    enabled = config.FilterInvites = !config.FilterInvites;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (enabled)
                {
                    InviteFilteringServers.Add(channel.Guild.Id);
                    await ReplyConfirmLocalized("invite_filter_server_on").ConfigureAwait(false);
                }
                else
                {
                    InviteFilteringServers.TryRemove(channel.Guild.Id);
                    await ReplyConfirmLocalized("invite_filter_server_off").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ChnlFilterInv()
            {
                var channel = (ITextChannel)Context.Channel;

                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FilterInvitesChannelIds));
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
                    await ReplyConfirmLocalized("invite_filter_channel_on").ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("invite_filter_channel_off").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task SrvrFilterWords()
            {
                var channel = (ITextChannel)Context.Channel;

                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id, set => set);
                    enabled = config.FilterWords = !config.FilterWords;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (enabled)
                {
                    WordFilteringServers.Add(channel.Guild.Id);
                    await ReplyConfirmLocalized("word_filter_server_on").ConfigureAwait(false);
                }
                else
                {
                    WordFilteringServers.TryRemove(channel.Guild.Id);
                    await ReplyConfirmLocalized("word_filter_server_off").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ChnlFilterWords()
            {
                var channel = (ITextChannel)Context.Channel;

                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FilterWordsChannelIds));
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
                    await ReplyConfirmLocalized("word_filter_channel_on").ConfigureAwait(false);
                }
                else
                {
                    WordFilteringChannels.TryRemove(channel.Id);
                    await ReplyConfirmLocalized("word_filter_channel_off").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task FilterWord([Remainder] string word)
            {
                var channel = (ITextChannel)Context.Channel;

                word = word?.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(word))
                    return;

                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FilteredWords));

                    removed = config.FilteredWords.RemoveWhere(fw => fw.Word.Trim().ToLowerInvariant() == word);

                    if (removed == 0)
                        config.FilteredWords.Add(new Services.Database.Models.FilteredWord() { Word = word });

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                var filteredWords = serverFilteredWords.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<string>());

                if (removed == 0)
                {
                    filteredWords.Add(word);
                    await ReplyConfirmLocalized("filter_word_add", Format.Code(word)).ConfigureAwait(false);
                }
                else
                {
                    filteredWords.TryRemove(word);
                    await ReplyConfirmLocalized("filter_word_remove", Format.Code(word)).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task LstFilterWords()
            {
                var channel = (ITextChannel)Context.Channel;

                ConcurrentHashSet<string> filteredWords;
                serverFilteredWords.TryGetValue(channel.Guild.Id, out filteredWords);

                await channel.SendConfirmAsync(GetText("filter_word_list"), string.Join("\n", filteredWords))
                        .ConfigureAwait(false);
            }
        }
    }
}
