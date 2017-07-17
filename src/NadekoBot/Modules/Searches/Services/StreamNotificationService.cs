using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Common.Exceptions;
using NadekoBot.Services.Impl;

namespace NadekoBot.Modules.Searches.Services
{
    public class StreamNotificationService : INService
    {
        private readonly Timer _streamCheckTimer;
        private bool firstStreamNotifPass { get; set; } = true;
        private readonly ConcurrentDictionary<string, StreamStatus> _cachedStatuses = new ConcurrentDictionary<string, StreamStatus>();

        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly NadekoStrings _strings;

        public StreamNotificationService(DbService db, DiscordSocketClient client, NadekoStrings strings)
        {
            _db = db;
            _client = client;
            _strings = strings;
            _streamCheckTimer = new Timer(async (state) =>
            {
                var oldCachedStatuses = new ConcurrentDictionary<string, StreamStatus>(_cachedStatuses);
                _cachedStatuses.Clear();
                IEnumerable<FollowedStream> streams;
                using (var uow = _db.UnitOfWork)
                {
                    streams = uow.GuildConfigs.GetAllFollowedStreams(client.Guilds.Select(x => (long)x.Id).ToList());
                }

                await Task.WhenAll(streams.Select(async fs =>
                {
                    try
                    {
                        var newStatus = await GetStreamStatus(fs).ConfigureAwait(false);
                        if (firstStreamNotifPass)
                        {
                            return;
                        }

                        StreamStatus oldStatus;
                        if (oldCachedStatuses.TryGetValue(newStatus.ApiLink, out oldStatus) &&
                            oldStatus.IsLive != newStatus.IsLive)
                        {
                            var server = _client.GetGuild(fs.GuildId);
                            var channel = server?.GetTextChannel(fs.ChannelId);
                            if (channel == null)
                                return;
                            try
                            {
                                await channel.EmbedAsync(GetEmbed(fs, newStatus, channel.Guild.Id)).ConfigureAwait(false);
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

                firstStreamNotifPass = false;
            }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        }

        public async Task<StreamStatus> GetStreamStatus(FollowedStream stream, bool checkCache = true)
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

        public EmbedBuilder GetEmbed(FollowedStream fs, StreamStatus status, ulong guildId)
        {
            var embed = new EmbedBuilder().WithTitle(fs.Username)
                                          .WithUrl(GetLink(fs))
                                          .AddField(efb => efb.WithName(GetText(fs, "status"))
                                                            .WithValue(status.IsLive ? "Online" : "Offline")
                                                            .WithIsInline(true))
                                          .AddField(efb => efb.WithName(GetText(fs, "viewers"))
                                                            .WithValue(status.IsLive ? status.Views : "-")
                                                            .WithIsInline(true))
                                          .AddField(efb => efb.WithName(GetText(fs, "platform"))
                                                            .WithValue(fs.Type.ToString())
                                                            .WithIsInline(true))
                                          .WithColor(status.IsLive ? NadekoBot.OkColor : NadekoBot.ErrorColor);

            return embed;
        }

        public string GetText(FollowedStream fs, string key, params object[] replacements) =>
            _strings.GetText(key,
                fs.GuildId,
                "Searches".ToLowerInvariant(),
                replacements);

        public string GetLink(FollowedStream fs)
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
