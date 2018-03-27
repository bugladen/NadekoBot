using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Common.Exceptions;
using Newtonsoft.Json;
using NLog;

namespace NadekoBot.Modules.Searches.Services
{
    public class StreamNotificationService : INService
    {
        private bool _firstStreamNotifPass = true;

        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly NadekoStrings _strings;
        private readonly IDataCache _cache;
        private readonly HttpClient _http;
        private readonly Logger _log;
        private readonly IBotCredentials _creds;
        private readonly ConcurrentDictionary<
            (FollowedStream.FType Type, string Username),
            ConcurrentHashSet<(ulong GuildId, FollowedStream fs)>> _followedStreams;

        public StreamNotificationService(NadekoBot bot, DbService db, DiscordSocketClient client,
            NadekoStrings strings, IDataCache cache, IBotCredentials creds)
        {
            _db = db;
            _client = client;
            _strings = strings;
            _cache = cache;
            _creds = creds;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Client-ID", _creds.TwitchClientId);
            _log = LogManager.GetCurrentClassLogger();

            _followedStreams = bot.AllGuildConfigs.SelectMany(x => x.FollowedStreams)
                .GroupBy(x => (x.Type, x.Username))
                .ToDictionary(x => x.Key, x => new ConcurrentHashSet<(ulong, FollowedStream)>(x.Select(y => (y.GuildId, y))))
                .ToConcurrent();

            _cache.SubscribeToStreamUpdates(OnStreamsUpdated);

            if (_client.ShardId == 0)
            {
                var _ = Task.Run(async () =>
                {
                    await Task.Delay(20000);
                    var sw = Stopwatch.StartNew();
                    while (true)
                    {
                        sw.Restart();
                        try
                        {
                            // get old statuses' live data
                            var oldStreamStatuses = (await _cache.GetAllStreamDataAsync())
                                .ToDictionary(x => x.ApiUrl, x => x.Live);
                            // clear old statuses
                            await _cache.ClearAllStreamData();
                            // get a list of streams which are followed right now.
                            IEnumerable<FollowedStream> fss;
                            using (var uow = _db.UnitOfWork)
                            {
                                fss = uow.GuildConfigs.GetFollowedStreams()
                                    .Distinct(fs => (fs.Type, fs.Username.ToLowerInvariant()));
                                uow.Complete();
                            }
                            // get new statuses for those streams
                            var newStatuses = (await Task.WhenAll(fss.Select(f => GetStreamStatus(f.Type, f.Username, false))))
                                .Where(x => x != null);
                            if (_firstStreamNotifPass)
                            {
                                _firstStreamNotifPass = false;
                                continue;
                            }

                            // for each new one, if there is an old one with a different status, add it to the list
                            List<StreamResponse> toPublish = new List<StreamResponse>();
                            foreach (var s in newStatuses)
                            {
                                if (oldStreamStatuses.TryGetValue(s.ApiUrl, out var live) &&
                                    live != s.Live)
                                {
                                    toPublish.Add(s);
                                }
                            }
                            // publish the list
                            if (toPublish.Any())
                            {
                                await _cache.PublishStreamUpdates(toPublish);
                                sw.Stop();
                                _log.Info("Retreived and published stream statuses in {0:F2}s", sw.Elapsed.TotalSeconds);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Warn(ex);
                        }
                        finally
                        {
                            await Task.Delay(30000);
                        }
                    }
                });
            }
        }

        private async Task OnStreamsUpdated(StreamResponse[] updates)
        {
            List<Task> sendTasks = new List<Task>();
            //going through all of the updates
            foreach (var u in updates)
            {
                // get the list of channels which need to be notified for this stream
                if (_followedStreams.TryGetValue((u.StreamType, u.Name.Trim().ToLowerInvariant()), out var locs))
                {
                    // notify them all
                    var tasks = locs.Select(x =>
                    {
                        return _client.GetGuild(x.GuildId)
                            ?.GetTextChannel(x.fs.ChannelId)
                            ?.EmbedAsync(GetEmbed(x.fs, u, x.GuildId));
                    }).Where(x => x != null);

                    sendTasks.AddRange(tasks);
                }
            }
            // wait for all messages to be sent out
            await Task.WhenAll(sendTasks);
        }

        public async Task<StreamResponse> GetStreamStatus(FollowedStream.FType t, string username, bool checkCache = true)
        {
            string url = string.Empty;
            Type type = null;
            username = username.ToLowerInvariant();
            switch (t)
            {
                case FollowedStream.FType.Twitch:
                    url = $"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(username)}";
                    type = typeof(TwitchResponse);
                    break;
                case FollowedStream.FType.Smashcast:
                    url = $"https://api.smashcast.tv/user/{username}";
                    type = typeof(SmashcastResponse);
                    break;
                case FollowedStream.FType.Mixer:
                    url = $"https://mixer.com/api/v1/channels/{username}";
                    type = typeof(MixerResponse);
                    break;
                case FollowedStream.FType.Picarto:
                    url = $"https://api.picarto.tv/v1/channel/name/{username}";
                    type = typeof(PicartoResponse);
                    break;
                default:
                    break;
            }
            try
            {
                if (checkCache && _cache.TryGetStreamData(url, out string dataStr))
                    return JsonConvert.DeserializeObject<StreamResponse>(dataStr);

                var response = await _http.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new StreamNotFoundException($"Stream Not Found: {username} [{type.Name}]");
                var responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject(responseStr, type) as IStreamResponse;
                data.ApiUrl = url;
                var sr = new StreamResponse
                {
                    ApiUrl = data.ApiUrl,
                    Followers = data.Followers,
                    Game = data.Game,
                    Icon = data.Icon,
                    Live = data.Live,
                    Name = data.Name ?? username.ToLowerInvariant(),
                    StreamType = data.StreamType,
                    Title = data.Title,
                    Viewers = data.Viewers,
                    Preview = data.Preview,
                };
                await _cache.SetStreamDataAsync(url, JsonConvert.SerializeObject(sr));
                return sr;
            }
            catch (StreamNotFoundException ex)
            {
                _log.Warn(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message);
                return null;
            }
        }

        public void UntrackStream(FollowedStream fs)
        {
            if (_followedStreams.TryGetValue((fs.Type, fs.Username), out var data))
            {
                data.TryRemove((fs.GuildId, fs));
            }
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

            if (!string.IsNullOrWhiteSpace(status.Preview))
                embed.WithImageUrl(status.Preview);

            return embed;
        }

        public void TrackStream(FollowedStream fs)
        {
            _followedStreams.AddOrUpdate((fs.Type, fs.Username),
                (k) => new ConcurrentHashSet<(ulong, FollowedStream)>(new[] { (fs.GuildId, fs) }),
                (k, old) =>
                {
                    old.Add((fs.GuildId, fs));
                    return old;
                });
        }

        public string GetText(FollowedStream fs, string key, params object[] replacements) =>
            _strings.GetText(key,
                fs.GuildId,
                "Searches".ToLowerInvariant(),
                replacements);

        public string GetLink(FollowedStream fs)
        {
            if (fs.Type == FollowedStream.FType.Smashcast)
                return $"https://www.smashcast.tv/{fs.Username}/";
            if (fs.Type == FollowedStream.FType.Twitch)
                return $"https://www.twitch.tv/{fs.Username}/";
            if (fs.Type == FollowedStream.FType.Mixer)
                return $"https://www.mixer.com/{fs.Username}/";
            if (fs.Type == FollowedStream.FType.Picarto)
                return $"https://www.picarto.tv/{fs.Username}";
            return "??";
        }
    }
}