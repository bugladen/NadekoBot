using Discord;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Discord.WebSocket;

namespace NadekoBot.Modules.Searches.Services
{
    public class FeedsService : INService
    {
        private readonly DbService _db;
        private readonly ConcurrentDictionary<string, HashSet<FeedSub>> _subs;
        private readonly DiscordSocketClient _client;
        private readonly ConcurrentDictionary<string, DateTime> _lastPosts = 
            new ConcurrentDictionary<string, DateTime>();

        public FeedsService(NadekoBot bot, DbService db, DiscordSocketClient client)
        {
            _db = db;

            _subs = bot
                .AllGuildConfigs
                .SelectMany(x => x.FeedSubs)
                .GroupBy(x => x.Url)
                .ToDictionary(x => x.Key, x => x.ToHashSet())
                .ToConcurrent();

            _client = client;

            foreach (var kvp in _subs)
            {
                // to make sure rss feeds don't post right away, but 
                // only the updates from after the bot has started
                _lastPosts.AddOrUpdate(kvp.Key, DateTime.UtcNow, (k, old) => DateTime.UtcNow);
            }
#if !GLOBAL_NADEKO
            var _ = Task.Run(TrackFeeds);
#endif
        }
        
        public async Task<EmbedBuilder> TrackFeeds()
        {
            while (true)
            {
                foreach (var kvp in _subs)
                {
                    if (kvp.Value.Count == 0)
                        continue;

                    if (!_lastPosts.TryGetValue(kvp.Key, out DateTime lastTime))
                        lastTime = _lastPosts.AddOrUpdate(kvp.Key, DateTime.UtcNow, (k, old) => DateTime.UtcNow);

                    var rssUrl = kvp.Key;
                    try
                    {
                        using (var xmlReader = XmlReader.Create(rssUrl, new XmlReaderSettings() { Async = true }))
                        {
                            var feedReader = new RssFeedReader(xmlReader);

                            var embed = new EmbedBuilder()
                                .WithAuthor(kvp.Key)
                                .WithOkColor();

                            while (await feedReader.Read().ConfigureAwait(false) && feedReader.ElementType != SyndicationElementType.Item)
                            {
                                switch (feedReader.ElementType)
                                {
                                    case SyndicationElementType.Link:
                                        var uri = await feedReader.ReadLink().ConfigureAwait(false);
                                        embed.WithAuthor(kvp.Key, url: uri.Uri.AbsoluteUri);
                                        break;
                                    case SyndicationElementType.Content:
                                        var content = await feedReader.ReadContent().ConfigureAwait(false);
                                        break;
                                    case SyndicationElementType.Category:
                                        break;
                                    case SyndicationElementType.Image:
                                        ISyndicationImage image = await feedReader.ReadImage().ConfigureAwait(false);
                                        embed.WithThumbnailUrl(image.Url.AbsoluteUri);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            ISyndicationItem item = await feedReader.ReadItem().ConfigureAwait(false);
                            if (item.Published.UtcDateTime <= lastTime)
                                continue;

                            var desc = item.Description.StripHTML();

                            lastTime = item.Published.UtcDateTime;
                            var title = string.IsNullOrWhiteSpace(item.Title) ? "-" : item.Title;
                            desc = Format.Code(item.Published.ToString()) + Environment.NewLine + desc;
                            var link = item.Links.FirstOrDefault();
                            if (link != null)
                                desc = $"[link]({link.Uri}) " + desc;

                            var img = item.Links.FirstOrDefault(x => x.RelationshipType == "enclosure")?.Uri.AbsoluteUri
                                ?? Regex.Match(item.Description, @"src=""(?<src>.*?)""").Groups["src"].ToString();

                            if (!string.IsNullOrWhiteSpace(img) && Uri.IsWellFormedUriString(img, UriKind.Absolute))
                                embed.WithImageUrl(img);

                            embed.AddField(title, desc);

                            //send the created embed to all subscribed channels
                            var sendTasks = kvp.Value
                                .Where(x => x.GuildConfig != null)
                                .Select(x => _client.GetGuild(x.GuildConfig.GuildId)
                                    ?.GetTextChannel(x.ChannelId))
                                .Where(x => x != null)
                                .Select(x => x.EmbedAsync(embed));

                            _lastPosts.AddOrUpdate(kvp.Key, item.Published.UtcDateTime, (k, old) => item.Published.UtcDateTime);

                            await Task.WhenAll(sendTasks).ConfigureAwait(false);
                        }
                    }
                    catch { }
                }

                await Task.Delay(10000).ConfigureAwait(false);
            }
        }

        public List<FeedSub> GetFeeds(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.FeedSubs))
                    .FeedSubs
                    .OrderBy(x => x.Id)
                    .ToList();
            }
        }

        public bool AddFeed(ulong guildId, ulong channelId, string rssFeed)
        {
            rssFeed.ThrowIfNull(nameof(rssFeed));

            var fs = new FeedSub()
            {
                ChannelId = channelId,
                Url = rssFeed.Trim().ToLowerInvariant(),
            };

            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.FeedSubs));

                if (gc.FeedSubs.Contains(fs))
                {
                    return false;
                }
                else if (gc.FeedSubs.Count >= 10)
                {
                    return false;
                }

                gc.FeedSubs.Add(fs);

                //adding all, in case bot wasn't on this guild when it started
                foreach (var f in gc.FeedSubs)
                {
                    _subs.AddOrUpdate(f.Url, new HashSet<FeedSub>(), (k, old) =>
                    {
                        old.Add(f);
                        return old;
                    });
                }

                uow.SaveChanges();
            }

            return true;
        }

        public bool RemoveFeed(ulong guildId, int index)
        {
            if (index < 0)
                return false;

            using (var uow = _db.GetDbContext())
            {
                var items = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.FeedSubs))
                    .FeedSubs
                    .OrderBy(x => x.Id)
                    .ToList();

                if (items.Count <= index)
                    return false;
                var toRemove = items[index];
                _subs.AddOrUpdate(toRemove.Url, new HashSet<FeedSub>(), (key, old) =>
                {
                    old.Remove(toRemove);
                    return old;
                });
                uow._context.Remove(toRemove);
                uow.SaveChanges();
            }
            return true;
        }
    }
}
