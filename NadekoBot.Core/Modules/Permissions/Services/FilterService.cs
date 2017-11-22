using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Permissions.Services
{
    public class FilterService : IEarlyBlocker, INService
    {
        private readonly Logger _log;

        public ConcurrentHashSet<ulong> InviteFilteringChannels { get; }
        public ConcurrentHashSet<ulong> InviteFilteringServers { get; }

        //serverid, filteredwords
        public ConcurrentDictionary<ulong, ConcurrentHashSet<string>> ServerFilteredWords { get; }

        public ConcurrentHashSet<ulong> WordFilteringChannels { get; }
        public ConcurrentHashSet<ulong> WordFilteringServers { get; }

        //public ConcurrentHashSet<ulong> LinkFilteringServers { get; }
        //public ConcurrentDictionary<ulong, bool> LinkFilteringChannelSettings { get; }

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

        public FilterService(DiscordSocketClient _client, NadekoBot bot)
        {
            _log = LogManager.GetCurrentClassLogger();

            InviteFilteringServers = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs.Where(gc => gc.FilterInvites).Select(gc => gc.GuildId));
            InviteFilteringChannels = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs.SelectMany(gc => gc.FilterInvitesChannelIds.Select(fci => fci.ChannelId)));

            var dict = bot.AllGuildConfigs.ToDictionary(gc => gc.GuildId, gc => new ConcurrentHashSet<string>(gc.FilteredWords.Select(fw => fw.Word)));

            ServerFilteredWords = new ConcurrentDictionary<ulong, ConcurrentHashSet<string>>(dict);

            var serverFiltering = bot.AllGuildConfigs.Where(gc => gc.FilterWords);
            WordFilteringServers = new ConcurrentHashSet<ulong>(serverFiltering.Select(gc => gc.GuildId));
            WordFilteringChannels = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs.SelectMany(gc => gc.FilterWordsChannelIds.Select(fwci => fwci.ChannelId)));

            //LinkFilteringServers = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs.Where(gc => gc.FilterLinks).Select(x => x.GuildId));

            _client.MessageUpdated += (oldData, newMsg, channel) =>
            {
                var _ = Task.Run(() =>
                {
                    var guild = (channel as ITextChannel)?.Guild;
                    var usrMsg = newMsg as IUserMessage;

                    if (guild == null || usrMsg == null)
                        return Task.CompletedTask;

                    return TryBlockEarly(guild, usrMsg);
                });
                return Task.CompletedTask;
            };
        }

        public async Task<bool> TryBlockEarly(IGuild guild, IUserMessage msg)
            =>  !(msg.Author is IGuildUser gu) //it's never filtered outside of guilds, and never block administrators
                ? false
                : !gu.GuildPermissions.Administrator && (await FilterInvites(guild, msg) || await FilterWords(guild, msg));

        public async Task<bool> FilterWords(IGuild guild, IUserMessage usrMsg)
        {
            if (guild is null)
                return false;
            if (usrMsg is null)
                return false;

            var filteredChannelWords = FilteredWordsForChannel(usrMsg.Channel.Id, guild.Id) ?? new ConcurrentHashSet<string>();
            var filteredServerWords = FilteredWordsForServer(guild.Id) ?? new ConcurrentHashSet<string>();
            var wordsInMessage = usrMsg.Content.ToLowerInvariant().Split(' ');
            if (filteredChannelWords.Count != 0 || filteredServerWords.Count != 0)
            {
                foreach (var word in wordsInMessage)
                {
                    if (filteredChannelWords.Contains(word) ||
                        filteredServerWords.Contains(word))
                    {
                        try
                        {
                            await usrMsg.DeleteAsync().ConfigureAwait(false);
                        }
                        catch (HttpException ex)
                        {
                            _log.Warn("I do not have permission to filter words in channel with id " + usrMsg.Channel.Id, ex);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<bool> FilterInvites(IGuild guild, IUserMessage usrMsg)
        {
            if (guild is null)
                return false;
            if (usrMsg is null)
                return false;

            if ((InviteFilteringChannels.Contains(usrMsg.Channel.Id) 
                || InviteFilteringServers.Contains(guild.Id)) 
                && usrMsg.Content.IsDiscordInvite())
            {
                try
                {
                    await usrMsg.DeleteAsync().ConfigureAwait(false);
                    return true;
                }
                catch (HttpException ex)
                {
                    _log.Warn("I do not have permission to filter invites in channel with id " + usrMsg.Channel.Id, ex);
                    return true;
                }
            }
            return false;
        }
    }
}
