using Discord.Audio;
using NadekoBot.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoLibrary;

namespace NadekoBot.Classes.Music {


    public class SongInfo {
        public string Provider { get; internal set; }
        public string Title { get; internal set; }
        public string Uri { get; internal set; }
    }

    public class Song {
        public StreamState State { get; internal set; }
        public object PrettyName =>
            $"**【 {SongInfo.Title.TrimTo(55)} 】**`{(SongInfo.Provider ?? "-")}`";
        public SongInfo SongInfo { get; }

        private Song(SongInfo songInfo) {
            this.SongInfo = songInfo;
        }

        internal void Play(IAudioClient voiceClient, CancellationToken cancelToken) {
            var p = Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-i {SongInfo.Uri} -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            Task.Delay(2000); //give it 2 seconds to get some dataz
            int blockSize = 3840; // 1920 for mono
            byte[] buffer = new byte[blockSize];
            int read;
            while (!cancelToken.IsCancellationRequested) {
                read = p.StandardOutput.BaseStream.Read(buffer, 0, blockSize);
                if (read == 0)
                    break; //nothing to read
                voiceClient.Send(buffer, 0, read);
            }
            voiceClient.Wait();
        }

        public static async Task<Song> ResolveSong(string query, MusicType musicType = MusicType.Normal) {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            if (musicType != MusicType.Local && IsRadioLink(query)) {
                musicType = MusicType.Radio;
                query = await HandleStreamContainers(query) ?? query;
            }

            try {
                if (musicType == MusicType.Local) {
                    return new Song(new SongInfo {
                        Uri = "\"" + Path.GetFullPath(query) + "\"",
                        Title = Path.GetFileNameWithoutExtension(query),
                        Provider = "Local File",
                    });
                }
                else if (musicType == MusicType.Radio) {
                    return new Song(new SongInfo {
                        Uri = query,
                        Title = $"{query}",
                        Provider = "Radio Stream",
                    });
                }
                else if (SoundCloud.Default.IsSoundCloudLink(query)) {
                    var svideo = await SoundCloud.Default.GetVideoAsync(query);
                    return new Song(new SongInfo {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = svideo.StreamLink,
                    });
                }
                else {
                    var links = await SearchHelper.FindYoutubeUrlByKeywords(query);
                    if (links == String.Empty)
                        throw new OperationCanceledException("Not a valid youtube query.");
                    var allVideos = await Task.Factory.StartNew(async () => await YouTube.Default.GetAllVideosAsync(links)).Unwrap();
                    var videos = allVideos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
                    var video = videos
                                    .Where(v => v.AudioBitrate < 192)
                                    .OrderByDescending(v => v.AudioBitrate)
                                    .FirstOrDefault();

                    if (video == null) // do something with this error
                        throw new Exception("Could not load any video elements based on the query.");
                    return new Song(new SongInfo {
                        Title = video.Title.Substring(0, video.Title.Length - 10), // removing trailing "- You Tube"
                        Provider = "YouTube",
                        Uri = video.Uri,
                    });

                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed resolving the link.{ex.Message}");
                return null;
            }
        }

        private static async Task<string> HandleStreamContainers(string query) {
            string file = null;
            try {
                file = await SearchHelper.GetResponseAsync(query);
            }
            catch {
                return query;
            }
            if (query.Contains(".pls")) {
                //File1=http://armitunes.com:8000/
                //Regex.Match(query)
                try {
                    var m = Regex.Match(file, "File1=(?<url>.*?)\\n");
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch {
                    Console.WriteLine($"Failed reading .pls:\n{file}");
                    return null;
                }
            }
            else if (query.Contains(".m3u")) {
                /* 
                    # This is a comment
                   C:\xxx4xx\xxxxxx3x\xx2xxxx\xx.mp3
                   C:\xxx5xx\x6xxxxxx\x7xxxxx\xx.mp3
                */
                try {
                    var m = Regex.Match(file, "(?<url>^[^#].*)", RegexOptions.Multiline);
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch {
                    Console.WriteLine($"Failed reading .m3u:\n{file}");
                    return null;
                }

            }
            else if (query.Contains(".asx")) {
                //<ref href="http://armitunes.com:8000"/>
                try {
                    var m = Regex.Match(file, "<ref href=\"(?<url>.*?)\"");
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch {
                    Console.WriteLine($"Failed reading .asx:\n{file}");
                    return null;
                }
            }
            else if (query.Contains(".xspf")) {
                /*
                <?xml version="1.0" encoding="UTF-8"?>
                    <playlist version="1" xmlns="http://xspf.org/ns/0/">
                        <trackList>
                            <track><location>file:///mp3s/song_1.mp3</location></track>
                */
                try {
                    var m = Regex.Match(file, "<location>(?<url>.*?)</location>");
                    var res = m.Groups["url"]?.ToString();
                    return res?.Trim();
                }
                catch {
                    Console.WriteLine($"Failed reading .xspf:\n{file}");
                    return null;
                }
            }

            return query;
        }

        private static bool IsRadioLink(string query) {
            return (query.StartsWith("http") ||
                    query.StartsWith("ww"))
                    &&
                    (query.Contains(".pls") ||
                    query.Contains(".m3u") ||
                    query.Contains(".asx") ||
                    query.Contains(".xspf"));
        }
    }
}
