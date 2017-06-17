using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using System.Text.RegularExpressions;
using NLog;
using System.IO;
using VideoLibrary;
using System.Net.Http;
using System.Collections.Generic;

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

        public ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public MusicService(IGoogleApiService google, 
            NadekoStrings strings, ILocalization localization, DbService db, 
            SoundCloudApiService sc, IBotCredentials creds, IEnumerable<GuildConfig> gcs)
        {
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

        public MusicPlayer GetPlayer(ulong guildId)
        {
            MusicPlayers.TryGetValue(guildId, out var player);
            return player;
        }

        public MusicPlayer GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh)
        {
            string GetText(string text, params object[] replacements) =>
                _strings.GetText(text, _localization.GetCultureInfo(textCh.Guild), "Music".ToLowerInvariant(), replacements);

            return MusicPlayers.GetOrAdd(guildId, server =>
            {
                var vol = _defaultVolumes.GetOrAdd(guildId, (id) =>
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        return uow.GuildConfigs.For(guildId, set => set).DefaultMusicVolume;
                    }
                });
                
                var mp = new MusicPlayer(voiceCh, textCh, vol, _google);
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

                        if (mp.Autoplay && mp.Playlist.Count == 0 && song.SongInfo.ProviderType == MusicType.Normal)
                        {
                            var relatedVideos = (await _google.GetRelatedVideosAsync(song.SongInfo.Query, 4)).ToList();
                            if (relatedVideos.Count > 0)
                                await QueueSong(await textCh.Guild.GetCurrentUserAsync(),
                                    textCh,
                                    voiceCh,
                                    relatedVideos[new NadekoRandom().Next(0, relatedVideos.Count)],
                                    true).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                };

                mp.OnStarted += async (player, song) =>
                {
                    try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); }
                    catch
                    {
                        // ignored
                    }
                    var sender = player;
                    if (sender == null)
                        return;
                    try
                    {
                        playingMessage?.DeleteAfter(0);

                        playingMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                    .WithAuthor(eab => eab.WithName(GetText("playing_song")).WithMusicIcon())
                                                    .WithDescription(song.PrettyName)
                                                    .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                                    .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnPauseChanged += async (paused) =>
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

                mp.SongRemoved += async (song, index) =>
                {
                    try
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eab => eab.WithName(GetText("removed_song") + " #" + (index + 1)).WithMusicIcon())
                            .WithDescription(song.PrettyName)
                            .WithFooter(ef => ef.WithText(song.PrettyInfo))
                            .WithErrorColor();

                        await mp.OutputTextChannel.EmbedAsync(embed).ConfigureAwait(false);

                    }
                    catch
                    {
                        // ignored
                    }
                };
                return mp;
            });
        }


        public async Task QueueSong(IGuildUser queuer, ITextChannel textCh, IVoiceChannel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
        {
            string GetText(string text, params object[] replacements) => 
                _strings.GetText(text, _localization.GetCultureInfo(textCh.Guild), "Music".ToLowerInvariant(), replacements);

            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (!silent)
                    await textCh.SendErrorAsync(GetText("must_be_in_voice")).ConfigureAwait(false);
                throw new ArgumentNullException(nameof(voiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("Invalid song query.", nameof(query));

            var musicPlayer = GetOrCreatePlayer(textCh.Guild.Id, voiceCh, textCh);
            Song resolvedSong;
            try
            {
                musicPlayer.ThrowIfQueueFull();
                resolvedSong = await ResolveSong(query, musicType).ConfigureAwait(false);

                if (resolvedSong == null)
                    throw new SongNotFoundException();

                musicPlayer.AddSong(resolvedSong, queuer.Username);
            }
            catch (PlaylistFullException)
            {
                try
                {
                    await textCh.SendConfirmAsync(GetText("queue_full", musicPlayer.MaxQueueSize));
                }
                catch
                {
                    // ignored
                }
                throw;
            }
            if (!silent)
            {
                try
                {
                    //var queuedMessage = await textCh.SendConfirmAsync($"🎵 Queued **{resolvedSong.SongInfo.Title}** at `#{musicPlayer.Playlist.Count + 1}`").ConfigureAwait(false);
                    var queuedMessage = await textCh.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                            .WithAuthor(eab => eab.WithName(GetText("queued_song") + " #" + (musicPlayer.Playlist.Count + 1)).WithMusicIcon())
                                                            .WithDescription($"{resolvedSong.PrettyName}\n{GetText("queue")} ")
                                                            .WithThumbnailUrl(resolvedSong.Thumbnail)
                                                            .WithFooter(ef => ef.WithText(resolvedSong.PrettyProvider)))
                                                            .ConfigureAwait(false);
                    queuedMessage?.DeleteAfter(10);
                }
                catch
                {
                    // ignored
                } // if queued message sending fails, don't attempt to delete it
            }
        }

        public void DestroyPlayer(ulong id)
        {
            if (MusicPlayers.TryRemove(id, out var mp))
                mp.Destroy();
        }


        public async Task<Song> ResolveSong(string query, MusicType musicType = MusicType.Normal)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            if (musicType != MusicType.Local && IsRadioLink(query))
            {
                musicType = MusicType.Radio;
                query = await HandleStreamContainers(query).ConfigureAwait(false) ?? query;
            }

            try
            {
                switch (musicType)
                {
                    case MusicType.Local:
                        return new Song(new SongInfo
                        {
                            Uri = "\"" + Path.GetFullPath(query) + "\"",
                            Title = Path.GetFileNameWithoutExtension(query),
                            Provider = "Local File",
                            ProviderType = musicType,
                            Query = query,
                        });
                    case MusicType.Radio:
                        return new Song(new SongInfo
                        {
                            Uri = query,
                            Title = $"{query}",
                            Provider = "Radio Stream",
                            ProviderType = musicType,
                            Query = query
                        })
                        { TotalTime = TimeSpan.MaxValue };
                }
                if (_sc.IsSoundCloudLink(query))
                {
                    var svideo = await _sc.ResolveVideoAsync(query).ConfigureAwait(false);
                    return new Song(new SongInfo
                    {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = await svideo.StreamLink(),
                        ProviderType = musicType,
                        Query = svideo.TrackLink,
                        AlbumArt = svideo.artwork_url,
                    })
                    { TotalTime = TimeSpan.FromMilliseconds(svideo.Duration) };
                }

                if (musicType == MusicType.Soundcloud)
                {
                    var svideo = await _sc.GetVideoByQueryAsync(query).ConfigureAwait(false);
                    return new Song(new SongInfo
                    {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = await svideo.StreamLink(),
                        ProviderType = MusicType.Soundcloud,
                        Query = svideo.TrackLink,
                        AlbumArt = svideo.artwork_url,
                    })
                    { TotalTime = TimeSpan.FromMilliseconds(svideo.Duration) };
                }

                var link = (await _google.GetVideoLinksByKeywordAsync(query).ConfigureAwait(false)).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(link))
                    throw new OperationCanceledException("Not a valid youtube query.");
                var allVideos = await Task.Run(async () => { try { return await YouTube.Default.GetAllVideosAsync(link).ConfigureAwait(false); } catch { return Enumerable.Empty<YouTubeVideo>(); } }).ConfigureAwait(false);
                var videos = allVideos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
                var video = videos
                    .Where(v => v.AudioBitrate < 256)
                    .OrderByDescending(v => v.AudioBitrate)
                    .FirstOrDefault();

                if (video == null) // do something with this error
                    throw new Exception("Could not load any video elements based on the query.");
                var m = Regex.Match(query, @"\?t=(?<t>\d*)");
                int gotoTime = 0;
                if (m.Captures.Count > 0)
                    int.TryParse(m.Groups["t"].ToString(), out gotoTime);
                var song = new Song(new SongInfo
                {
                    Title = video.Title.Substring(0, video.Title.Length - 10), // removing trailing "- You Tube"
                    Provider = "YouTube",
                    Uri = await video.GetUriAsync().ConfigureAwait(false),
                    Query = link,
                    ProviderType = musicType,
                });
                song.SkipTo = gotoTime;
                return song;
            }
            catch (Exception ex)
            {
                _log.Warn($"Failed resolving the link.{ex.Message}");
                _log.Warn(ex);
                return null;
            }
        }

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
                    var m = Regex.Match(file, "File1=(?<url>.*?)\\n");
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
                    var m = Regex.Match(file, "(?<url>^[^#].*)", RegexOptions.Multiline);
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
                    var m = Regex.Match(file, "<ref href=\"(?<url>.*?)\"");
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
                    var m = Regex.Match(file, "<location>(?<url>.*?)</location>");
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

        private bool IsRadioLink(string query) =>
            (query.StartsWith("http") ||
            query.StartsWith("ww"))
            &&
            (query.Contains(".pls") ||
            query.Contains(".m3u") ||
            query.Contains(".asx") ||
            query.Contains(".xspf"));
    }
}
