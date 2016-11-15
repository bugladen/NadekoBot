using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Services;
using System.Threading;
using System.Collections.Generic;
using NadekoBot.Services.Database.Models;
using System.Net.Http;
using Discord.WebSocket;
using NadekoBot.Attributes;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public class StreamStatus
        {
            public StreamStatus(string link, bool isLive, string views)
            {
                Link = link;
                IsLive = isLive;
                Views = views;
            }

            public bool IsLive { get; set; }
            public string Link { get; set; }
            public string Views { get; set; }
        }
        [Group]
        public class StreamNotificationCommands
        {
            private Timer checkTimer { get; }
            private ConcurrentDictionary<string, StreamStatus> oldCachedStatuses = new ConcurrentDictionary<string, StreamStatus>();
            private ConcurrentDictionary<string, StreamStatus> cachedStatuses = new ConcurrentDictionary<string, StreamStatus>();
            private bool FirstPass { get; set; } = true;

            public StreamNotificationCommands()
            {
                checkTimer = new Timer(async (state) =>
                {
                    oldCachedStatuses = new ConcurrentDictionary<string, StreamStatus>(cachedStatuses);
                    cachedStatuses = new ConcurrentDictionary<string, StreamStatus>();
                    try
                    {
                        IEnumerable<FollowedStream> streams;
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            streams = uow.GuildConfigs.GetAllFollowedStreams();
                        }
                        foreach (var stream in streams)
                        {
                            StreamStatus data;
                            try
                            {
                                data = await GetStreamStatus(stream).ConfigureAwait(false);
                                if (data == null)
                                    return;
                            }
                            catch
                            {
                                continue;
                            }

                            StreamStatus oldData;
                            oldCachedStatuses.TryGetValue(data.Link, out oldData);

                            if (oldData == null || data.IsLive != oldData.IsLive)
                            {
                                if (FirstPass)
                                    continue;
                                var server = NadekoBot.Client.GetGuild(stream.GuildId);
                                var channel = server?.GetTextChannel(stream.ChannelId);
                                if (channel == null)
                                    continue;
                                var msg = $"`{stream.Username}`'s stream is now " +
                                          $"**{(data.IsLive ? "ONLINE" : "OFFLINE")}** with " +
                                          $"**{data.Views}** viewers.";
                                if (data.IsLive)
                                    if (stream.Type == FollowedStream.FollowedStreamType.Hitbox)
                                        msg += $"\n`Here is the Link:`【 http://www.hitbox.tv/{stream.Username}/ 】";
                                    else if (stream.Type == FollowedStream.FollowedStreamType.Twitch)
                                        msg += $"\n`Here is the Link:`【 http://www.twitch.tv/{stream.Username}/ 】";
                                    else if (stream.Type == FollowedStream.FollowedStreamType.Beam)
                                        msg += $"\n`Here is the Link:`【 http://www.beam.pro/{stream.Username}/ 】";
                                try { await channel.SendMessageAsync(msg).ConfigureAwait(false); } catch { }
                            }
                        }
                        FirstPass = false;
                    }
                    catch { }
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            }

            private async Task<StreamStatus> GetStreamStatus(FollowedStream stream, bool checkCache = true)
            {
                bool isLive;
                string response;
                JObject data;
                StreamStatus result;
                switch (stream.Type)
                {
                    case FollowedStream.FollowedStreamType.Hitbox:
                        var hitboxUrl = $"https://api.hitbox.tv/media/status/{stream.Username}";
                        if (checkCache && cachedStatuses.TryGetValue(hitboxUrl, out result))
                            return result;
                        using (var http = new HttpClient())
                        {
                            response = await http.GetStringAsync(hitboxUrl).ConfigureAwait(false);
                        }
                        data = JObject.Parse(response);
                        isLive = data["media_is_live"].ToString() == "1";
                        result = new StreamStatus(hitboxUrl, isLive, data["media_views"].ToString());
                        cachedStatuses.TryAdd(hitboxUrl, result);
                        return result;
                    case FollowedStream.FollowedStreamType.Twitch:
                        var twitchUrl = $"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(stream.Username)}?client_id=67w6z9i09xv2uoojdm9l0wsyph4hxo6";
                        if (checkCache && cachedStatuses.TryGetValue(twitchUrl, out result))
                            return result;
                        using (var http = new HttpClient())
                        {
                            response = await http.GetStringAsync(twitchUrl).ConfigureAwait(false);
                        }
                        data = JObject.Parse(response);
                        isLive = !string.IsNullOrWhiteSpace(data["stream"].ToString());
                        result = new StreamStatus(twitchUrl, isLive, isLive ? data["stream"]["viewers"].ToString() : "0");
                        cachedStatuses.TryAdd(twitchUrl, result);
                        return result;
                    case FollowedStream.FollowedStreamType.Beam:
                        var beamUrl = $"https://beam.pro/api/v1/channels/{stream.Username}";
                        if (checkCache && cachedStatuses.TryGetValue(beamUrl, out result))
                            return result;
                        using (var http = new HttpClient())
                        {
                            response = await http.GetStringAsync(beamUrl).ConfigureAwait(false);
                        }
                        data = JObject.Parse(response);
                        isLive = data["online"].ToObject<bool>() == true;
                        result = new StreamStatus(beamUrl, isLive, data["viewersCurrent"].ToString());
                        cachedStatuses.TryAdd(beamUrl, result);
                        return result;
                    default:
                        break;
                }
                return null;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageMessages)]
            public async Task Hitbox(IUserMessage msg, [Remainder] string username) =>
                await TrackStream((ITextChannel)msg.Channel, username, FollowedStream.FollowedStreamType.Hitbox)
                    .ConfigureAwait(false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageMessages)]
            public async Task Twitch(IUserMessage msg, [Remainder] string username) =>
                await TrackStream((ITextChannel)msg.Channel, username, FollowedStream.FollowedStreamType.Twitch)
                    .ConfigureAwait(false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageMessages)]
            public async Task Beam(IUserMessage msg, [Remainder] string username) =>
                await TrackStream((ITextChannel)msg.Channel, username, FollowedStream.FollowedStreamType.Beam)
                    .ConfigureAwait(false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListStreams(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                IEnumerable<FollowedStream> streams;
                using (var uow = DbHandler.UnitOfWork())
                {
                    streams = uow.GuildConfigs.For(channel.Guild.Id).FollowedStreams;
                }

                if (!streams.Any())
                {
                    await channel.SendMessageAsync("You are not following any streams on this server.").ConfigureAwait(false);
                    return;
                }

                var text = string.Join("\n", streams.Select(snc =>
                {
                    return $"`{snc.Username}`'s stream on **{channel.Guild.GetTextChannel(snc.ChannelId)?.Name}** channel. 【`{snc.Type.ToString()}`】";
                }));

                await channel.SendMessageAsync($"You are following **{streams.Count()}** streams on this server.\n\n" + text).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageMessages)]
            public async Task RemoveStream(IUserMessage msg, [Remainder] string username)
            {
                var channel = (ITextChannel)msg.Channel;

                username = username.ToLowerInvariant().Trim();

                FollowedStream toRemove;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    var streams = config.FollowedStreams;
                    toRemove = streams.Where(fs => fs.ChannelId == channel.Id && fs.Username.ToLowerInvariant() == username).FirstOrDefault();
                    if (toRemove != null)
                    {
                        config.FollowedStreams = new HashSet<FollowedStream>(streams.Except(new[] { toRemove }));
                        await uow.CompleteAsync();
                    }
                }
                if (toRemove == null)
                {
                    await channel.SendMessageAsync(":anger: No such stream.").ConfigureAwait(false);
                    return;
                }
                await channel.SendMessageAsync($":ok: Removed `{toRemove.Username}`'s stream ({toRemove.Type}) from notifications.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task CheckStream(IUserMessage imsg, FollowedStream.FollowedStreamType platform, [Remainder] string username)
            {
                var channel = (ITextChannel)imsg.Channel;

                var stream = username?.Trim();
                if (string.IsNullOrWhiteSpace(stream))
                    return;
                try
                {
                    var streamStatus = (await GetStreamStatus(new FollowedStream
                    {
                        Username = stream,
                        Type = platform
                    }));
                    if (streamStatus.IsLive)
                    {
                        await channel.SendMessageAsync($"`Streamer {username} is online with {streamStatus.Views} viewers.`");
                    }
                    else
                    {
                        await channel.SendMessageAsync($"`Streamer {username} is offline.`");
                    }
                }
                catch
                {
                    await channel.SendMessageAsync("No channel found.");
                }
            }

            private async Task TrackStream(ITextChannel channel, string username, FollowedStream.FollowedStreamType type)
            {
                username = username.ToLowerInvariant().Trim();
                var stream = new FollowedStream
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    Username = username,
                    Type = type,
                };
                bool exists;
                using (var uow = DbHandler.UnitOfWork())
                {
                    exists = uow.GuildConfigs.For(channel.Guild.Id).FollowedStreams.Where(fs => fs.ChannelId == channel.Id && fs.Username.ToLowerInvariant().Trim()  == username).Any();
                }
                if (exists)
                {
                    await channel.SendMessageAsync($":anger: I am already following `{username}` ({type}) stream on this channel.").ConfigureAwait(false);
                    return;
                }
                StreamStatus data;
                try
                {
                    data = await GetStreamStatus(stream).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendMessageAsync(":anger: Stream probably doesn't exist.").ConfigureAwait(false);
                    return;
                }
                var msg = $"Stream is currently **{(data.IsLive ? "ONLINE" : "OFFLINE")}** with **{data.Views}** viewers";
                if (data.IsLive)
                    if (type == FollowedStream.FollowedStreamType.Hitbox)
                        msg += $"\n`Here is the Link:`【 http://www.hitbox.tv/{stream.Username}/ 】";
                    else if (type == FollowedStream.FollowedStreamType.Twitch)
                        msg += $"\n`Here is the Link:`【 http://www.twitch.tv/{stream.Username}/ 】";
                    else if (type == FollowedStream.FollowedStreamType.Beam)
                        msg += $"\n`Here is the Link:`【 https://beam.pro/{stream.Username}/ 】";
                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.GuildConfigs.For(channel.Guild.Id).FollowedStreams.Add(stream);
                    await uow.CompleteAsync();
                }
                msg = $":ok: I will notify this channel when status changes.\n{msg}";
                await channel.SendMessageAsync(msg).ConfigureAwait(false);
            }
        }
    }
}