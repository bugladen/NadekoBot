using Discord.Commands;
using Discord;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Services;
using System.Threading;
using System.Collections.Generic;
using NadekoBot.Services.Database.Models;
using System.Net.Http;
using NadekoBot.Attributes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public class StreamStatus
        {
            public bool IsLive { get; set; }
            public string ApiLink { get; set; }
            public string Views { get; set; }
        }

        public class HitboxResponse {
            public bool Success { get; set; } = true;
            [JsonProperty("media_is_live")]
            public string MediaIsLive { get; set; }
            public bool IsLive  => MediaIsLive == "1";
            [JsonProperty("media_views")]
            public string Views { get; set; }
        }

        public class TwitchResponse
        {
            public string Error { get; set; } = null;
            public bool IsLive => Stream != null;
            public StreamInfo Stream { get; set; }

            public class StreamInfo
            {
                public int Viewers { get; set; }
            }
        }

        public class BeamResponse
        {
            public string Error { get; set; } = null;

            [JsonProperty("online")]
            public bool IsLive { get; set; }
            public int ViewersCurrent { get; set; }
        }

        public class StreamNotFoundException : Exception
        {
            public StreamNotFoundException(string message) : base("Stream '" + message + "' not found.")
            {
            }
        }

        [Group]
        public class StreamNotificationCommands : NadekoSubmodule
        {
            private static readonly Timer _checkTimer;
            private static readonly ConcurrentDictionary<string, StreamStatus> _cachedStatuses = new ConcurrentDictionary<string, StreamStatus>();

            private static bool firstPass { get; set; } = true;

            static StreamNotificationCommands()
            {
                _checkTimer = new Timer(async (state) =>
                {
                    var oldCachedStatuses = new ConcurrentDictionary<string, StreamStatus>(_cachedStatuses);
                    _cachedStatuses.Clear();
                    IEnumerable<FollowedStream> streams;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        streams = uow.GuildConfigs.GetAllFollowedStreams();
                    }

                    await Task.WhenAll(streams.Select(async fs =>
                    {
                        try
                        {
                            var newStatus = await GetStreamStatus(fs).ConfigureAwait(false);
                            if (firstPass)
                            {
                                return;
                            }

                            StreamStatus oldStatus;
                            if (oldCachedStatuses.TryGetValue(newStatus.ApiLink, out oldStatus) &&
                                oldStatus.IsLive != newStatus.IsLive)
                            {
                                var server = NadekoBot.Client.GetGuild(fs.GuildId);
                                var channel = server?.GetTextChannel(fs.ChannelId);
                                if (channel == null)
                                    return;
                                try
                                {
                                    await channel.EmbedAsync(fs.GetEmbed(newStatus, channel.Guild.Id)).ConfigureAwait(false);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }));

                    firstPass = false;
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            }

            private static async Task<StreamStatus> GetStreamStatus(FollowedStream stream, bool checkCache = true)
            {
                string response;
                StreamStatus result;
                switch (stream.Type)
                {
                    case FollowedStream.FollowedStreamType.Hitbox:
                        var hitboxUrl = $"https://api.hitbox.tv/media/status/{stream.Username.ToLowerInvariant()}";
                        if (checkCache && _cachedStatuses.TryGetValue(hitboxUrl, out result))
                            return result;
                        using (var http = new HttpClient())
                        {
                            response = await http.GetStringAsync(hitboxUrl).ConfigureAwait(false);
                        }
                        var hbData = JsonConvert.DeserializeObject<HitboxResponse>(response);
                        if (!hbData.Success)
                            throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
                        result = new StreamStatus()
                        {
                            IsLive = hbData.IsLive,
                            ApiLink = hitboxUrl,
                            Views = hbData.Views
                        };
                        _cachedStatuses.AddOrUpdate(hitboxUrl, result, (key, old) => result);
                        return result;
                    case FollowedStream.FollowedStreamType.Twitch:
                        var twitchUrl = $"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(stream.Username.ToLowerInvariant())}?client_id=67w6z9i09xv2uoojdm9l0wsyph4hxo6";
                        if (checkCache && _cachedStatuses.TryGetValue(twitchUrl, out result))
                            return result;
                        using (var http = new HttpClient())
                        {
                            response = await http.GetStringAsync(twitchUrl).ConfigureAwait(false);
                        }
                        var twData = JsonConvert.DeserializeObject<TwitchResponse>(response);
                        if (twData.Error != null)
                        {
                            throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
                        }
                        result = new StreamStatus()
                        {
                            IsLive = twData.IsLive,
                            ApiLink = twitchUrl,
                            Views = twData.Stream?.Viewers.ToString() ?? "0"
                        };
                        _cachedStatuses.AddOrUpdate(twitchUrl, result, (key, old) => result);
                        return result;
                    case FollowedStream.FollowedStreamType.Beam:
                        var beamUrl = $"https://beam.pro/api/v1/channels/{stream.Username.ToLowerInvariant()}";
                        if (checkCache && _cachedStatuses.TryGetValue(beamUrl, out result))
                            return result;
                        using (var http = new HttpClient())
                        {
                            response = await http.GetStringAsync(beamUrl).ConfigureAwait(false);
                        }

                        var bmData = JsonConvert.DeserializeObject<BeamResponse>(response);
                        if (bmData.Error != null)
                            throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
                        result = new StreamStatus()
                        {
                            IsLive = bmData.IsLive,
                            ApiLink = beamUrl,
                            Views = bmData.ViewersCurrent.ToString()
                        };
                        _cachedStatuses.AddOrUpdate(beamUrl, result, (key, old) => result);
                        return result;
                    default:
                        break;
                }
                return null;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Hitbox([Remainder] string username) =>
                await TrackStream((ITextChannel)Context.Channel, username, FollowedStream.FollowedStreamType.Hitbox)
                    .ConfigureAwait(false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Twitch([Remainder] string username) =>
                await TrackStream((ITextChannel)Context.Channel, username, FollowedStream.FollowedStreamType.Twitch)
                    .ConfigureAwait(false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Beam([Remainder] string username) =>
                await TrackStream((ITextChannel)Context.Channel, username, FollowedStream.FollowedStreamType.Beam)
                    .ConfigureAwait(false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListStreams()
            {
                IEnumerable<FollowedStream> streams;
                using (var uow = DbHandler.UnitOfWork())
                {
                    streams = uow.GuildConfigs
                                 .For(Context.Guild.Id, 
                                      set => set.Include(gc => gc.FollowedStreams))
                                 .FollowedStreams;
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
            public async Task RemoveStream(FollowedStream.FollowedStreamType type, [Remainder] string username)
            {
                username = username.ToLowerInvariant().Trim();

                var fs = new FollowedStream()
                {
                    ChannelId = Context.Channel.Id,
                    Username = username,
                    Type = type
                };

                bool removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FollowedStreams));
                    removed = config.FollowedStreams.Remove(fs);
                    if (removed)
                        await uow.CompleteAsync().ConfigureAwait(false);
                }
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
            public async Task CheckStream(FollowedStream.FollowedStreamType platform, [Remainder] string username)
            {
                var stream = username?.Trim();
                if (string.IsNullOrWhiteSpace(stream))
                    return;
                try
                {
                    var streamStatus = (await GetStreamStatus(new FollowedStream
                    {
                        Username = stream,
                        Type = platform,
                    }));
                    if (streamStatus.IsLive)
                    {
                        await ReplyConfirmLocalized("streamer_online",
                                username,
                                streamStatus.Views)
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

            private async Task TrackStream(ITextChannel channel, string username, FollowedStream.FollowedStreamType type)
            {
                username = username.ToLowerInvariant().Trim();
                var fs = new FollowedStream
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    Username = username,
                    Type = type,
                };

                StreamStatus status;
                try
                {
                    status = await GetStreamStatus(fs).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("stream_not_exist").ConfigureAwait(false);
                    return;
                }

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FollowedStreams))
                                    .FollowedStreams
                                    .Add(fs);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await channel.EmbedAsync(fs.GetEmbed(status, Context.Guild.Id), GetText("stream_tracked")).ConfigureAwait(false);
            }
        }
    }

    public static class FollowedStreamExtensions
    {
        public static EmbedBuilder GetEmbed(this FollowedStream fs, Searches.StreamStatus status, ulong guildId)
        {
            var embed = new EmbedBuilder().WithTitle(fs.Username)
                                          .WithUrl(fs.GetLink())
                                          .AddField(efb => efb.WithName(fs.GetText("status"))
                                                            .WithValue(status.IsLive ? "Online" : "Offline")
                                                            .WithIsInline(true))
                                          .AddField(efb => efb.WithName(fs.GetText("viewers"))
                                                            .WithValue(status.IsLive ? status.Views : "-")
                                                            .WithIsInline(true))
                                          .AddField(efb => efb.WithName(fs.GetText("platform"))
                                                            .WithValue(fs.Type.ToString())
                                                            .WithIsInline(true))
                                          .WithColor(status.IsLive ? NadekoBot.OkColor : NadekoBot.ErrorColor);

            return embed;
        }

        public static string GetText(this FollowedStream fs, string key, params object[] replacements) =>
            NadekoTopLevelModule.GetTextStatic(key,
                NadekoBot.Localization.GetCultureInfo(fs.GuildId),
                typeof(Searches).Name.ToLowerInvariant(),
                replacements);

        public static string GetLink(this FollowedStream fs)
        {
            if (fs.Type == FollowedStream.FollowedStreamType.Hitbox)
                return $"http://www.hitbox.tv/{fs.Username}/";
            if (fs.Type == FollowedStream.FollowedStreamType.Twitch)
                return $"http://www.twitch.tv/{fs.Username}/";
            if (fs.Type == FollowedStream.FollowedStreamType.Beam)
                return $"https://beam.pro/{fs.Username}/";
            return "??";
        }
    }
}
