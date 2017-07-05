using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using NLog;
using System.IO;
using VideoLibrary;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace NadekoBot.Services.Music
{
    public class MusicService
    {
        public const string MusicDataPath = "data/musicdata";

        private readonly IGoogleApiService _google;
        private readonly NadekoStrings _strings;
        private readonly ILocalization _localization;
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly SoundCloudApiService _sc;
        private readonly IBotCredentials _creds;
        private readonly ConcurrentDictionary<ulong, float> _defaultVolumes;
        private readonly DiscordSocketClient _client;

        public ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public MusicService(DiscordSocketClient client, IGoogleApiService google,
            NadekoStrings strings, ILocalization localization, DbService db,
            SoundCloudApiService sc, IBotCredentials creds, IEnumerable<GuildConfig> gcs)
        {
            _client = client;
            _google = google;
            _strings = strings;
            _localization = localization;
            _db = db;
            _sc = sc;
            _creds = creds;
            _log = LogManager.GetCurrentClassLogger();

            try { Directory.Delete(MusicDataPath, true); } catch { }

            _defaultVolumes = new ConcurrentDictionary<ulong, float>(gcs.ToDictionary(x => x.GuildId, x => x.DefaultMusicVolume));

            Directory.CreateDirectory(MusicDataPath);
        }

        public float GetDefaultVolume(ulong guildId)
        {
            return _defaultVolumes.GetOrAdd(guildId, (id) =>
            {
                using (var uow = _db.UnitOfWork)
                {
                    return uow.GuildConfigs.For(guildId, set => set).DefaultMusicVolume;
                }
            });
        }

        public Task<MusicPlayer> GetOrCreatePlayer(ICommandContext context)
        {
            var gUsr = (IGuildUser)context.User;
            var txtCh = (ITextChannel)context.Channel;
            var vCh = gUsr.VoiceChannel;
            return GetOrCreatePlayer(context.Guild.Id, vCh, txtCh);
        }

        public async Task<MusicPlayer> GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh)
        {
            string GetText(string text, params object[] replacements) =>
                _strings.GetText(text, _localization.GetCultureInfo(textCh.Guild), "Music".ToLowerInvariant(), replacements);

            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (textCh != null)
                {
                    await textCh.SendErrorAsync(GetText("must_be_in_voice")).ConfigureAwait(false);
                }
                throw new ArgumentException(nameof(voiceCh));
            }

            return MusicPlayers.GetOrAdd(guildId, _ =>
            {
                var vol = GetDefaultVolume(guildId);
                var mp = new MusicPlayer(this, _google, voiceCh, textCh, vol);

                IUserMessage playingMessage = null;
                IUserMessage lastFinishedMessage = null;
                mp.OnCompleted += async (s, song) =>
                {
                    try
                    {
                        lastFinishedMessage?.DeleteAfter(0);

                        try
                        {
                            lastFinishedMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                    .WithAuthor(eab => eab.WithName(GetText("finished_song")).WithMusicIcon())
                                    .WithDescription(song.PrettyName)
                                    .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnStarted += async (player, song) =>
                {
                    //try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); }
                    //catch
                    //{
                    //    // ignored
                    //}
                    var sender = player;
                    if (sender == null)
                        return;
                    try
                    {
                        playingMessage?.DeleteAfter(0);

                        playingMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                    .WithAuthor(eab => eab.WithName(GetText("playing_song", song.Index + 1)).WithMusicIcon())
                                                    .WithDescription(song.Song.PrettyName)
                                                    .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + song.Song.PrettyInfo)))
                                                    .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnPauseChanged += async (player, paused) =>
                {
                    try
                    {
                        IUserMessage msg;
                        if (paused)
                            msg = await mp.OutputTextChannel.SendConfirmAsync(GetText("paused")).ConfigureAwait(false);
                        else
                            msg = await mp.OutputTextChannel.SendConfirmAsync(GetText("resumed")).ConfigureAwait(false);

                        msg?.DeleteAfter(10);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                
                return mp;
            });
        }

        public MusicPlayer GetPlayerOrDefault(ulong guildId)
        {
            if (MusicPlayers.TryGetValue(guildId, out var mp))
                return mp;
            else
                return null;
        }

        public async Task TryQueueRelatedSongAsync(string query, ITextChannel txtCh, IVoiceChannel vch)
        {
            var related = (await _google.GetRelatedVideosAsync(query, 4)).ToArray();
            if (!related.Any())
                return;

            var si = await ResolveSong(related[new NadekoRandom().Next(related.Length)], _client.CurrentUser.ToString(), MusicType.YouTube);
            if (si == null)
                throw new SongNotFoundException();
            var mp = await GetOrCreatePlayer(txtCh.GuildId, vch, txtCh);
            mp.Enqueue(si);
        }

        public async Task<SongInfo> ResolveSong(string query, string queuerName, MusicType? musicType = null)
        {
            query.ThrowIfNull(nameof(query));

            SongInfo sinfo = null;
            switch (musicType)
            {
                case MusicType.YouTube:
                    sinfo = await ResolveYoutubeSong(query, queuerName);
                    break;
                case MusicType.Radio:
                    try { sinfo = ResolveRadioSong(IsRadioLink(query) ? await HandleStreamContainers(query) : query, queuerName); } catch { };
                    break;
                case MusicType.Local:
                    sinfo = ResolveLocalSong(query, queuerName);
                    break;
                case MusicType.Soundcloud:
                    sinfo = await ResolveSoundCloudSong(query, queuerName);
                    break;
                case null:
                    if (_sc.IsSoundCloudLink(query))
                        sinfo = await ResolveSoundCloudSong(query, queuerName);
                    else if (IsRadioLink(query))
                        sinfo = ResolveRadioSong(await HandleStreamContainers(query), queuerName);
                    else
                        try
                        {
                            sinfo = await ResolveYoutubeSong(query, queuerName);
                        }
                        catch
                        {
                            sinfo = null;
                        }
                    break;
            }

            return sinfo;
        }

        public async Task<SongInfo> ResolveSoundCloudSong(string query, string queuerName)
        {
            var svideo = !_sc.IsSoundCloudLink(query) ? 
                await _sc.GetVideoByQueryAsync(query).ConfigureAwait(false):
                await _sc.ResolveVideoAsync(query).ConfigureAwait(false);

            if (svideo == null)
                return null;
            return await SongInfoFromSVideo(svideo, queuerName);
        }

        public Task<SongInfo> SongInfoFromSVideo(SoundCloudVideo svideo, string queuerName) =>
            Task.FromResult(new SongInfo
            {
                Title = svideo.FullName,
                Provider = "SoundCloud",
                Uri = () => svideo.StreamLink(),
                ProviderType = MusicType.Soundcloud,
                Query = svideo.TrackLink,
                AlbumArt = svideo.artwork_url,
                QueuerName = queuerName
            });

    public SongInfo ResolveLocalSong(string query, string queuerName)
        {
            return new SongInfo
            {
                Uri = () => Task.FromResult("\"" + Path.GetFullPath(query) + "\""),
                Title = Path.GetFileNameWithoutExtension(query),
                Provider = "Local File",
                ProviderType = MusicType.Local,
                Query = query,
                QueuerName = queuerName
            };
        }

        public SongInfo ResolveRadioSong(string query, string queuerName)
        {
            return new SongInfo
            {
                Uri = () => Task.FromResult(query),
                Title = query,
                Provider = "Radio Stream",
                ProviderType = MusicType.Radio,
                Query = query,
                QueuerName = queuerName
            };
        }

        public async Task DestroyAllPlayers()
        {
            foreach (var key in MusicPlayers.Keys)
            {
                await DestroyPlayer(key);
            }
        }

        public async Task<SongInfo> ResolveYoutubeSong(string query, string queuerName)
        {
            _log.Info("Getting video");
            var (link, video) = await GetYoutubeVideo(query);

            if (video == null) // do something with this error
            {
                _log.Info("Could not load any video elements based on the query.");
                return null;
            }
            //var m = Regex.Match(query, @"\?t=(?<t>\d*)");
            //int gotoTime = 0;
            //if (m.Captures.Count > 0)
            //    int.TryParse(m.Groups["t"].ToString(), out gotoTime);

            _log.Info("Creating song info");
            var song = new SongInfo
            {
                Title = video.Title.Substring(0, video.Title.Length - 10), // removing trailing "- You Tube"
                Provider = "YouTube",
                Uri = async () => {
                    var vid = await GetYoutubeVideo(query);
                    if (vid.Item2 == null)
                        throw new HttpRequestException();

                    return await vid.Item2.GetUriAsync();
                },
                Query = link,
                ProviderType = MusicType.YouTube,
                QueuerName = queuerName
            };
            return song;
        }

        private async Task<(string, YouTubeVideo)> GetYoutubeVideo(string query)
        {
            _log.Info("Getting link");
            var link = (await _google.GetVideoLinksByKeywordAsync(query).ConfigureAwait(false)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(link))
            {
                _log.Info("No song found.");
                return (null, null);
            }
            _log.Info("Getting all videos");
            var allVideos = await Task.Run(async () => { try { return await YouTube.Default.GetAllVideosAsync(link).ConfigureAwait(false); } catch { return Enumerable.Empty<YouTubeVideo>(); } }).ConfigureAwait(false);
            var videos = allVideos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
            var video = videos
                .Where(v => v.AudioBitrate < 256)
                .OrderByDescending(v => v.AudioBitrate)
                .FirstOrDefault();

            return (link, video);
        }

        private bool IsRadioLink(string query) =>
            (query.StartsWith("http") ||
            query.StartsWith("ww"))
            &&
            (query.Contains(".pls") ||
            query.Contains(".m3u") ||
            query.Contains(".asx") ||
            query.Contains(".xspf"));

        public async Task DestroyPlayer(ulong id)
        {
            if (MusicPlayers.TryRemove(id, out var mp))
                await mp.Destroy();
        }

        private readonly Regex plsRegex = new Regex("File1=(?<url>.*?)\\n", RegexOptions.Compiled);
        private readonly Regex m3uRegex = new Regex("(?<url>^[^#].*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex asxRegex = new Regex("<ref href=\"(?<url>.*?)\"", RegexOptions.Compiled);
        private readonly Regex xspfRegex = new Regex("<location>(?<url>.*?)</location>", RegexOptions.Compiled);

        private async Task<string> HandleStreamContainers(string query)
        {
            string file = null;
            try
            {
                using (var http = new HttpClient())
                {
                    file = await http.GetStringAsync(query).ConfigureAwait(false);
                }
            }
            catch
            {
                return query;
            }
            if (query.Contains(".pls"))
            {
                //File1=http://armitunes.com:8000/
                //Regex.Match(query)
                try
                {
                    var m = plsRegex.Match(file);
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch
                {
                    _log.Warn($"Failed reading .pls:\n{file}");
                    return null;
                }
            }
            if (query.Contains(".m3u"))
            {
                /* 
# This is a comment
                   C:\xxx4xx\xxxxxx3x\xx2xxxx\xx.mp3
                   C:\xxx5xx\x6xxxxxx\x7xxxxx\xx.mp3
                */
                try
                {
                    var m = m3uRegex.Match(file);
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch
                {
                    _log.Warn($"Failed reading .m3u:\n{file}");
                    return null;
                }

            }
            if (query.Contains(".asx"))
            {
                //<ref href="http://armitunes.com:8000"/>
                try
                {
                    var m = asxRegex.Match(file);
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch
                {
                    _log.Warn($"Failed reading .asx:\n{file}");
                    return null;
                }
            }
            if (query.Contains(".xspf"))
            {
                /*
                <?xml version="1.0" encoding="UTF-8"?>
                    <playlist version="1" xmlns="http://xspf.org/ns/0/">
                        <trackList>
                            <track><location>file:///mp3s/song_1.mp3</location></track>
                */
                try
                {
                    var m = xspfRegex.Match(file);
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch
                {
                    _log.Warn($"Failed reading .xspf:\n{file}");
                    return null;
                }
            }

            return query;
        }
    }
}