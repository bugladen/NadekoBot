using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Common.Exceptions;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Services
{
    public class StreamNotificationService : INService
    {
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
#if !GLOBAL_NADEKO
            var _ = Task.Run(async () =>
           {
               while (true)
               {
                   await Task.Delay(60000);
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

                            if (oldCachedStatuses.TryGetValue(newStatus.ApiUrl, out var oldResponse) &&
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
               }
           });
#endif
        }

        public async Task<IStreamResponse> GetStreamStatus(FollowedStream stream, bool checkCache = true)
        {
            string url = string.Empty;
            IStreamResponse obj = null;
            Type type = null;
            switch (stream.Type)
            {
                case FollowedStream.FollowedStreamType.Twitch:
                    url = $"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(stream.Username.ToLowerInvariant())}?client_id=67w6z9i09xv2uoojdm9l0wsyph4hxo6";
                    type = typeof(TwitchResponse);
                    break;
                case FollowedStream.FollowedStreamType.Smashcast:
                    url = $"https://api.smashcast.tv/user/{stream.Username.ToLowerInvariant()}";
                    type = typeof(SmashcastResponse);
                    break;
                case FollowedStream.FollowedStreamType.Mixer:
                    url = $"https://mixer.com/api/v1/channels/{stream.Username.ToLowerInvariant()}";
                    type = typeof(MixerResponse);
                    break;
                case FollowedStream.FollowedStreamType.Picarto:
                    url = $"https://api.picarto.tv/v1/channel/name/{stream.Username.ToLowerInvariant()}";
                    type = typeof(PicartoResponse);
                    break;
                default:
                    break;
            }

            if (checkCache && _cachedStatuses.TryGetValue(url, out var result))
                return result;

            var response = await _http.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new StreamNotFoundException($"{stream.Username} [{stream.Type}]");
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonConvert.DeserializeObject(responseString, type) as IStreamResponse;
            data.ApiUrl = url;
            _cachedStatuses.AddOrUpdate(url, data, (key, old) => data);
            return data;
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
                            status.Followers.ToString(),
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
            if (fs.Type == FollowedStream.FollowedStreamType.Picarto)
                return $"https://www.picarto.tv/{fs.Username}";
            return "??";
        }
    }
}