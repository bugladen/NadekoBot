using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.SyndicationFeed.Rss;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class FeedCommands : NadekoSubmodule<FeedsService>
        {
            private readonly DiscordSocketClient _client;

            public FeedCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Feed(string url, [Remainder] ITextChannel channel = null)
            {
                var success = Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                if (success)
                {
                    channel = channel ?? (ITextChannel)Context.Channel;
                    using (var xmlReader = XmlReader.Create(url, new XmlReaderSettings() { Async = true }))
                    {
                        var reader = new RssFeedReader(xmlReader);
                        try
                        {
                            await reader.Read();
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine(ex);
                            success = false;
                        }
                    }

                    if (success)
                    {
                        success = _service.AddFeed(Context.Guild.Id, channel.Id, url);
                        if (success)
                        {
                            await ReplyConfirmLocalized("feed_added").ConfigureAwait(false);
                            return;
                        }
                    }
                }

                await ReplyErrorLocalized("feed_not_valid").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task FeedRemove(int index)
            {
                if (_service.RemoveFeed(Context.Guild.Id, --index))
                {
                    await ReplyConfirmLocalized("feed_removed").ConfigureAwait(false);
                }
                else
                    await ReplyErrorLocalized("feed_out_of_range").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task FeedList()
            {
                var feeds = _service.GetFeeds(Context.Guild.Id);

                if (!feeds.Any())
                {
                    await Context.Channel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithDescription(GetText("feed_no_feed")))
                        .ConfigureAwait(false);
                    return;
                }

                await Context.SendPaginatedConfirmAsync(0, (cur) =>
                {
                    var embed = new EmbedBuilder()
                       .WithOkColor();
                    var i = 0;
                    var fs = string.Join("\n", feeds.Skip(cur * 10)
                        .Take(10)
                        .Select(x => $"`{(cur * 10) + (++i)}.` <#{x.ChannelId}> {x.Url}"));

                    return embed.WithDescription(fs);

                }, feeds.Count, 10);
            }
        }
    }
}
