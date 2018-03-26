using Discord.Commands;
using Discord;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Core.Services;
using System.Collections.Generic;
using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using NadekoBot.Modules.Searches.Common;
using System.Text.RegularExpressions;
using System;
using Discord.WebSocket;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class StreamNotificationCommands : NadekoSubmodule<StreamNotificationService>
        {
            private readonly DbService _db;

            public StreamNotificationCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public Task Smashcast([Remainder] string username) =>
                smashcastRegex.IsMatch(username)
                ? StreamAdd(username)
                : TrackStream((ITextChannel)Context.Channel,
                    username,
                    FollowedStream.FType.Smashcast);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public Task Twitch([Remainder] string username) =>
                twitchRegex.IsMatch(username)
                ? StreamAdd(username)
                : TrackStream((ITextChannel)Context.Channel,
                    username,
                    FollowedStream.FType.Twitch);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public Task Picarto([Remainder] string username) =>
                picartoRegex.IsMatch(username)
                ? StreamAdd(username)
                : TrackStream((ITextChannel)Context.Channel,
                    username,
                    FollowedStream.FType.Picarto);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public Task Mixer([Remainder] string username) =>
                mixerRegex.IsMatch(username)
                ? StreamAdd(username)
                : TrackStream((ITextChannel)Context.Channel,
                    username,
                    FollowedStream.FType.Mixer);

            private static readonly Regex twitchRegex = new Regex(@"twitch.tv/(?<name>.+)/?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private static readonly Regex mixerRegex = new Regex(@"mixer.com/(?<name>.+)/?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private static readonly Regex smashcastRegex = new Regex(@"smashcast.tv/(?<name>.+)/?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private static readonly Regex picartoRegex = new Regex(@"picarto.tv/(?<name>.+)/?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task StreamAdd(string link)
            {
                var streamRegexes = new(Func<string, Task> Func, Regex Regex)[]
                {
                    (Twitch, twitchRegex),
                    (Mixer, mixerRegex),
                    (Smashcast, smashcastRegex),
                    (Picarto, picartoRegex)
                };

                foreach (var s in streamRegexes)
                {
                    var m = s.Regex.Match(link);
                    if (m.Captures.Count != 0)
                    {
                        await s.Func(m.Groups["name"].ToString());
                        return;
                    }
                }

                await ReplyErrorLocalized("stream_not_exist").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task StreamRemove(string link)
            {
                var streamRegexes = new(Func<string, Task> Func, Regex Regex)[]
                {
                    ((u) => StreamRemove(FollowedStream.FType.Twitch, u), twitchRegex),
                    ((u) => StreamRemove(FollowedStream.FType.Mixer, u), mixerRegex),
                    ((u) => StreamRemove(FollowedStream.FType.Smashcast, u), smashcastRegex),
                    ((u) => StreamRemove(FollowedStream.FType.Picarto, u), picartoRegex),
                };

                foreach (var s in streamRegexes)
                {
                    var m = s.Regex.Match(link);
                    if (m.Captures.Count != 0)
                    {
                        await s.Func(m.Groups["name"].ToString());
                        return;
                    }
                }

                await ReplyErrorLocalized("stream_not_exist").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListStreams()
            {
                IEnumerable<FollowedStream> streams;
                using (var uow = _db.UnitOfWork)
                {
                    var all = uow.GuildConfigs
                                 .For(Context.Guild.Id,
                                      set => set.Include(gc => gc.FollowedStreams))
                                 .FollowedStreams;

                    var toRemove = all.Where(x => ((SocketGuild)Context.Guild).GetTextChannel(x.ChannelId) == null);
                    streams = all.Except(toRemove);
                    if(toRemove.Any())
                    {
                        foreach (var r in toRemove)
                        {
                            _service.UntrackStream(r);
                        }
                        uow._context.RemoveRange(toRemove);
                        uow.Complete();
                    }
                }

                if (!streams.Any())
                {
                    await ReplyErrorLocalized("streams_none").ConfigureAwait(false);
                    return;
                }

                var text = string.Join("\n", await Task.WhenAll(streams.Select(async snc =>
                {
                    var ch = await Context.Guild.GetTextChannelAsync(snc.ChannelId);
                    return string.Format("{0}'s stream on {1} channel. 【{2}】",
                        Format.Code(snc.Username),
                        Format.Bold(ch?.Name ?? "deleted-channel"),
                        Format.Code(snc.Type.ToString()));
                })));

                await Context.Channel.SendConfirmAsync(GetText("streams_following", streams.Count()) + "\n\n" + text)
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task StreamRemove(FollowedStream.FType type, [Remainder] string username)
            {
                username = username.ToLowerInvariant().Trim();

                var fs = new FollowedStream()
                {
                    GuildId = Context.Guild.Id,
                    ChannelId = Context.Channel.Id,
                    Username = username,
                    Type = type
                };

                bool removed;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FollowedStreams));
                    removed = config.FollowedStreams.Remove(fs);
                    if (removed)
                        await uow.CompleteAsync().ConfigureAwait(false);
                }
                _service.UntrackStream(fs);
                if (!removed)
                {
                    await ReplyErrorLocalized("stream_no").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("stream_removed",
                    Format.Code(username),
                    type).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task CheckStream(FollowedStream.FType platform, [Remainder] string username)
            {
                var stream = username?.Trim();
                if (string.IsNullOrWhiteSpace(stream))
                    return;
                try
                {
                    var streamStatus = await _service.GetStreamStatus(platform, username);
                    if (streamStatus == null)
                    {
                        await ReplyErrorLocalized("no_channel_found").ConfigureAwait(false);
                        return;
                    }
                    if (streamStatus.Live)
                    {
                        await ReplyConfirmLocalized("streamer_online",
                                Format.Bold(username),
                                Format.Bold(streamStatus.Viewers.ToString()))
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyConfirmLocalized("streamer_offline",
                            username).ConfigureAwait(false);
                    }
                }
                catch
                {
                    await ReplyErrorLocalized("no_channel_found").ConfigureAwait(false);
                }
            }

            private async Task TrackStream(ITextChannel channel, string username, FollowedStream.FType type)
            {
                username = username.ToLowerInvariant().Trim();
                var fs = new FollowedStream
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    Username = username,
                    Type = type,
                };

                IStreamResponse status;
                try
                {
                    status = await _service.GetStreamStatus(fs.Type, fs.Username).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("stream_not_exist").ConfigureAwait(false);
                    return;
                }

                if (status == null)
                {
                    await ReplyErrorLocalized("stream_not_exist").ConfigureAwait(false);
                    return;
                }

                using (var uow = _db.UnitOfWork)
                {
                    uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FollowedStreams))
                                    .FollowedStreams
                                    .Add(fs);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                _service.TrackStream(fs);
                await channel.EmbedAsync(_service.GetEmbed(fs, status, Context.Guild.Id), GetText("stream_tracked")).ConfigureAwait(false);
            }
        }
    }
}
