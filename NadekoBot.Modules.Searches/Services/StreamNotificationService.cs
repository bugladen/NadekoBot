using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
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
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Modules.Searches.Services
{
    public class StreamNotificationService : INService
    {
        private readonly Timer _streamCheckTimer;
        private bool firstStreamNotifPass { get; set; } = true;
        private readonly ConcurrentDictionary<string, IStreamResponse> _cachedStatuses = new ConcurrentDictionary<string, IStreamResponse>();
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly NadekoStrings _strings;
        private readonly HttpClient _http;

        public StreamNotificationService(DbService db, DiscordSocketClient client, NadekoStrings strings)
        {
            _db = db;
            _client = client;
            _strings = strings;
            _http = new HttpClient();
            _streamCheckTimer = new Timer(async (state) =>
            {
                var oldCachedStatuses = new ConcurrentDictionary<string, IStreamResponse>(_cachedStatuses);
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

                        IStreamResponse oldResponse;
                        if (oldCachedStatuses.TryGetValue(newStatus.Url, out oldResponse) &&
                            oldResponse.Live != newStatus.Live)
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

        public async Task<IStreamResponse> GetStreamStatus(FollowedStream stream, bool checkCache = true)
        {
            string response;
            IStreamResponse result;
            switch (stream.Type)
            {
                case FollowedStream.FollowedStreamType.Smashcast:
                    var smashcastUrl = $"https://api.smashcast.tv/user/{stream.Username.ToLowerInvariant()}";
                    if (checkCache && _cachedStatuses.TryGetValue(smashcastUrl, out result))
                        return result;
                    response = await _http.GetStringAsync(smashcastUrl).ConfigureAwait(false);

                    var scData = JsonConvert.DeserializeObject<SmashcastResponse>(response);
                    if (!scData.Success)
                        throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
                    scData.Url = smashcastUrl;
                    _cachedStatuses.AddOrUpdate(smashcastUrl, scData, (key, old) => scData);
                    return scData;
                case FollowedStream.FollowedStreamType.Twitch:
                    var twitchUrl = $"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(stream.Username.ToLowerInvariant())}?client_id=67w6z9i09xv2uoojdm9l0wsyph4hxo6";
                    if (checkCache && _cachedStatuses.TryGetValue(twitchUrl, out result))
                        return result;
                    response = await _http.GetStringAsync(twitchUrl).ConfigureAwait(false);

                    var twData = JsonConvert.DeserializeObject<TwitchResponse>(response);
                    if (twData.Error != null)
                    {
                        throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
                    }
                    twData.Url = twitchUrl;
                    _cachedStatuses.AddOrUpdate(twitchUrl, twData, (key, old) => twData);
                    return twData;
                case FollowedStream.FollowedStreamType.Mixer:
                    var beamUrl = $"https://mixer.com/api/v1/channels/{stream.Username.ToLowerInvariant()}";
                    if (checkCache && _cachedStatuses.TryGetValue(beamUrl, out result))
                        return result;
                    response = await _http.GetStringAsync(beamUrl).ConfigureAwait(false);


                    var bmData = JsonConvert.DeserializeObject<MixerResponse>(response);
                    if (bmData.Error != null)
                        throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
                    bmData.Url = beamUrl;
                    _cachedStatuses.AddOrUpdate(beamUrl, bmData, (key, old) => bmData);
                    return bmData;
                default:
                    break;
            }
            return null;
        }

        public EmbedBuilder GetEmbed(FollowedStream fs, IStreamResponse status, ulong guildId)
        {
            var embed = new EmbedBuilder()
                .WithTitle(fs.Username)
                .WithUrl(GetLink(fs))
                .WithDescription(GetLink(fs))
                .AddField(efb => efb.WithName(GetText(fs, "status"))
                                .WithValue(status.Live ? "Online" : "Offline")
                                .WithIsInline(true))
                .AddField(efb => efb.WithName(GetText(fs, "viewers"))
                                .WithValue(status.Live ? status.Viewers.ToString() : "-")
                                .WithIsInline(true))
                .WithColor(status.Live ? NadekoBot.OkColor : NadekoBot.ErrorColor);

            if (!string.IsNullOrWhiteSpace(status.Title))
                embed.WithAuthor(status.Title);

            if (!string.IsNullOrWhiteSpace(status.Game))
                embed.AddField(GetText(fs, "streaming"),
                                status.Game,
                                true);

            embed.AddField(GetText(fs, "followers"),
                            status.FollowerCount.ToString(),
                            true);

            if (!string.IsNullOrWhiteSpace(status.Icon))
                embed.WithThumbnailUrl(status.Icon);

            return embed;
        }

        public string GetText(FollowedStream fs, string key, params object[] replacements) =>
            _strings.GetText(key,
                fs.GuildId,
                "Searches".ToLowerInvariant(),
                replacements);

        public string GetLink(FollowedStream fs)
        {
            if (fs.Type == FollowedStream.FollowedStreamType.Smashcast)
                return $"https://www.smashcast.tv/{fs.Username}/";
            if (fs.Type == FollowedStream.FollowedStreamType.Twitch)
                return $"https://www.twitch.tv/{fs.Username}/";
            if (fs.Type == FollowedStream.FollowedStreamType.Mixer)
                return $"https://www.mixer.com/{fs.Username}/";
            return "??";
        }
    }
}