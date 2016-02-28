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
        public MusicType ProviderType { get; internal set; }
        public string Title { get; internal set; }
        public string Uri { get; internal set; }
    }
    /// <summary>
    /// 💩
    /// </summary>
    public class PoopyBuffer {

        private byte[] ringBuffer;

        public int WritePosition { get; private set; } = 0;

        public PoopyBuffer(int size) {
            ringBuffer = new byte[size];
        }

        public int Read(byte[] buffer, int count) {
            lock (this) {
                if (count > WritePosition)
                    count = WritePosition;
                if (count == 0)
                    return 0;

                Buffer.BlockCopy(ringBuffer, 0, buffer, 0, count);
                Buffer.BlockCopy(ringBuffer, count, ringBuffer, 0, WritePosition -= count);

                return count;
            }
        }

        public async Task WriteAsync(byte[] buffer, int count, CancellationToken cancelToken) {
            if (count > buffer.Length)
                throw new ArgumentException();
            while (count + WritePosition > ringBuffer.Length) {
                await Task.Delay(20);
                if (cancelToken.IsCancellationRequested)
                    return;
            }
            lock (this) {
                Buffer.BlockCopy(buffer, 0, ringBuffer, WritePosition, count);
                WritePosition += count;
            }
        }
    }
    public class Song {
        public StreamState State { get; internal set; }
        public string PrettyName =>
            $"**【 {SongInfo.Title.TrimTo(55)} 】**`{(SongInfo.Provider ?? "-")}`";
        public SongInfo SongInfo { get; }
        
        private PoopyBuffer songBuffer { get; } = new PoopyBuffer(10.MB());

        private bool prebufferingComplete { get; set; } = false;
        public MusicPlayer MusicPlayer { get; set; }

        private Song(SongInfo songInfo) {
            this.SongInfo = songInfo;
        }

        private Task BufferSong(CancellationToken cancelToken) =>
            Task.Run(async () => {
                var p = Process.Start(new ProcessStartInfo {
                    FileName = "ffmpeg",
                    Arguments = $"-i {SongInfo.Uri} -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                });

                int blockSize = 512;
                byte[] buffer = new byte[blockSize];
                int attempt = 0;
                while (!cancelToken.IsCancellationRequested) {
                    int read = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize);
                    if (read == 0)
                        if (attempt++ == 10)
                            break;
                        else
                            await Task.Delay(50);
                    else
                        attempt = 0;
                    await songBuffer.WriteAsync(buffer, read, cancelToken);
                    if (songBuffer.WritePosition > 5.MB())
                        prebufferingComplete = true;
                }
                Console.WriteLine("Buffering done.");
            });

        internal async Task Play(IAudioClient voiceClient, CancellationToken cancelToken) {
            var t = BufferSong(cancelToken).ConfigureAwait(false);
            int bufferAttempts = 0;
            int waitPerAttempt = 500;
            int toAttemptTimes = SongInfo.ProviderType != MusicType.Normal ? 4 : 8;
            while (!prebufferingComplete && bufferAttempts++ < toAttemptTimes) {
                await Task.Delay(waitPerAttempt);
            }
            int blockSize = 3840;
            byte[] buffer = new byte[blockSize];
            int attempt = 0;
            while (!cancelToken.IsCancellationRequested) {
                int read = songBuffer.Read(buffer, blockSize);
                if (read == 0)
                    if (attempt++ == 10) {
                        voiceClient.Wait();
                        Console.WriteLine("Playing done.");
                        return;
                    }
                    else
                        await Task.Delay(50);
                else
                    attempt = 0;

                while (this.MusicPlayer.Paused)
                    await Task.Delay(200);
                buffer = adjustVolume(buffer, MusicPlayer.Volume);
                voiceClient.Send(buffer, 0, read);
            }
            //try {
            //    voiceClient.Clear();
            //    Console.WriteLine("CLEARED");
            //}
            //catch {
            //    Console.WriteLine("CLEAR FAILED!!!");
            //}
        }

        //stackoverflow ftw
        private byte[] adjustVolume(byte[] audioSamples, float volume) {
            if (volume == 1.0f)
                return audioSamples;
            byte[] array = new byte[audioSamples.Length];
            for (int i = 0; i < array.Length; i += 2) {

                // convert byte pair to int
                short buf1 = audioSamples[i + 1];
                short buf2 = audioSamples[i];

                buf1 = (short)((buf1 & 0xff) << 8);
                buf2 = (short)(buf2 & 0xff);

                short res = (short)(buf1 | buf2);
                res = (short)(res * volume);

                // convert back
                array[i] = (byte)res;
                array[i + 1] = (byte)(res >> 8);

            }
            return array;
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
                        ProviderType = musicType,
                    });
                }
                else if (musicType == MusicType.Radio) {
                    return new Song(new SongInfo {
                        Uri = query,
                        Title = $"{query}",
                        Provider = "Radio Stream",
                        ProviderType = musicType,
                    });
                }
                else if (SoundCloud.Default.IsSoundCloudLink(query)) {
                    var svideo = await SoundCloud.Default.GetVideoAsync(query);
                    return new Song(new SongInfo {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = svideo.StreamLink,
                        ProviderType = musicType,
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
                        ProviderType = musicType,
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
