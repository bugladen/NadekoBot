using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using VideoLibrary;

namespace NadekoBot.Modules.Music.Classes
{
    public static class SongHandler
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public static async Task<Song> ResolveSong(string query, MusicType musicType = MusicType.Normal)
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
                if (SoundCloud.Default.IsSoundCloudLink(query))
                {
                    var svideo = await SoundCloud.Default.ResolveVideoAsync(query).ConfigureAwait(false);
                    return new Song(new SongInfo
                    {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = svideo.StreamLink,
                        ProviderType = musicType,
                        Query = svideo.TrackLink,
                        AlbumArt = svideo.artwork_url,
                    })
                    { TotalTime = TimeSpan.FromMilliseconds(svideo.Duration) };
                }

                if (musicType == MusicType.Soundcloud)
                {
                    var svideo = await SoundCloud.Default.GetVideoByQueryAsync(query).ConfigureAwait(false);
                    return new Song(new SongInfo
                    {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = svideo.StreamLink,
                        ProviderType = MusicType.Soundcloud,
                        Query = svideo.TrackLink,
                        AlbumArt = svideo.artwork_url,
                    })
                    { TotalTime = TimeSpan.FromMilliseconds(svideo.Duration) };
                }

                var link = (await NadekoBot.Google.GetVideosByKeywordsAsync(query).ConfigureAwait(false)).FirstOrDefault();
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

        private static async Task<string> HandleStreamContainers(string query)
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

        private static bool IsRadioLink(string query) =>
            (query.StartsWith("http") ||
            query.StartsWith("ww"))
            &&
            (query.Contains(".pls") ||
            query.Contains(".m3u") ||
            query.Contains(".asx") ||
            query.Contains(".xspf"));
    }
}
