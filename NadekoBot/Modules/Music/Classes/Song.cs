using Discord.Audio;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoLibrary;

namespace NadekoBot.Modules.Music.Classes
{
    public class SongInfo
    {
        public string Provider { get; internal set; }
        public MusicType ProviderType { get; internal set; }
        /// <summary>
        /// Will be set only if the providertype is normal
        /// </summary>
        public string Query { get; internal set; }
        public string Title { get; internal set; }
        public string Uri { get; internal set; }
    }
    public class Song
    {
        public StreamState State { get; internal set; }
        public string PrettyName =>
            $"**【 {SongInfo.Title.TrimTo(55)} 】**`{(SongInfo.Provider ?? "-")}` `by {QueuerName}`";
        public SongInfo SongInfo { get; }
        public string QueuerName { get; set; }

        private PoopyBuffer songBuffer { get; } = new PoopyBuffer(NadekoBot.Config.BufferSize);

        private bool prebufferingComplete { get; set; } = false;
        public MusicPlayer MusicPlayer { get; set; }

        public string PrettyCurrentTime()
        {
            var time = TimeSpan.FromSeconds(bytesSent / 3840 / 50);
            return $"【{(int)time.TotalMinutes}m {time.Seconds}s】";
        }

        private ulong bytesSent { get; set; } = 0;

        public bool PrintStatusMessage { get; set; } = true;

        private int skipTo = 0;
        public int SkipTo {
            get { return SkipTo; }
            set {
                skipTo = value;
                bytesSent = (ulong)skipTo * 3840 * 50;
            }
        }

        public Song(SongInfo songInfo)
        {
            this.SongInfo = songInfo;
        }

        public Song Clone()
        {
            var s = new Song(SongInfo);
            s.MusicPlayer = MusicPlayer;
            s.State = StreamState.Queued;
            return s;
        }

        public Song SetMusicPlayer(MusicPlayer mp)
        {
            this.MusicPlayer = mp;
            return this;
        }

        private Task BufferSong(CancellationToken cancelToken) =>
            Task.Factory.StartNew(async () =>
            {
                Process p = null;
                try
                {
                    p = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-ss {skipTo} -i {SongInfo.Uri} -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = false,
                        CreateNoWindow = true,
                    });
                    const int blockSize = 3840;
                    var buffer = new byte[blockSize];
                    var attempt = 0;
                    while (!cancelToken.IsCancellationRequested)
                    {
                        var read = 0;
                        try
                        {
                            read = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize, cancelToken)
                                          .ConfigureAwait(false);
                        }
                        catch
                        {
                            return;
                        }
                        if (read == 0)
                            if (attempt++ == 50)
                                break;
                            else
                                await Task.Delay(100, cancelToken).ConfigureAwait(false);
                        else
                            attempt = 0;
                        await songBuffer.WriteAsync(buffer, read, cancelToken).ConfigureAwait(false);
                        if (songBuffer.ContentLength > 2.MB())
                            prebufferingComplete = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Buffering errored: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine($"Buffering done." + $" [{songBuffer.ContentLength}]");
                    if (p != null)
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
            }, TaskCreationOptions.LongRunning);

        internal async Task Play(IAudioClient voiceClient, CancellationToken cancelToken)
        {
            var bufferTask = BufferSong(cancelToken).ConfigureAwait(false);
            var bufferAttempts = 0;
            const int waitPerAttempt = 500;
            var toAttemptTimes = SongInfo.ProviderType != MusicType.Normal ? 5 : 9;
            while (!prebufferingComplete && bufferAttempts++ < toAttemptTimes)
            {
                await Task.Delay(waitPerAttempt, cancelToken).ConfigureAwait(false);
            }
            cancelToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Prebuffering done? in {waitPerAttempt * bufferAttempts}");
            const int blockSize = 3840;
            var attempt = 0;
            while (!cancelToken.IsCancellationRequested)
            {
                //Console.WriteLine($"Read: {songBuffer.ReadPosition}\nWrite: {songBuffer.WritePosition}\nContentLength:{songBuffer.ContentLength}\n---------");
                byte[] buffer = new byte[blockSize];
                var read = songBuffer.Read(buffer, blockSize);
                unchecked
                {
                    bytesSent += (ulong)read;
                }
                if (read == 0)
                    if (attempt++ == 20)
                    {
                        voiceClient.Wait();
                        Console.WriteLine($"Song finished. [{songBuffer.ContentLength}]");
                        break;
                    }
                    else
                        await Task.Delay(100, cancelToken).ConfigureAwait(false);
                else
                    attempt = 0;

                while (this.MusicPlayer.Paused)
                    await Task.Delay(200, cancelToken).ConfigureAwait(false);
                buffer = AdjustVolume(buffer, MusicPlayer.Volume);
                voiceClient.Send(buffer, 0, read);
            }
            Console.WriteLine("Awiting buffer task");
            await bufferTask;
            Console.WriteLine("Buffer task done.");
            voiceClient.Clear();
            cancelToken.ThrowIfCancellationRequested();
        }

        //stackoverflow ftw
        private static byte[] AdjustVolume(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1.0f) < 0.01f)
                return audioSamples;
            var array = new byte[audioSamples.Length];
            for (var i = 0; i < array.Length; i += 2)
            {

                // convert byte pair to int
                short buf1 = audioSamples[i + 1];
                short buf2 = audioSamples[i];

                buf1 = (short)((buf1 & 0xff) << 8);
                buf2 = (short)(buf2 & 0xff);

                var res = (short)(buf1 | buf2);
                res = (short)(res * volume);

                // convert back
                array[i] = (byte)res;
                array[i + 1] = (byte)(res >> 8);

            }
            return array;
        }

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
                        });
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
                    });
                }

                if (musicType == MusicType.Soundcloud)
                {
                    var svideo = await SoundCloud.Default.GetVideoByQueryAsync(query).ConfigureAwait(false);
                    return new Song(new SongInfo
                    {
                        Title = svideo.FullName,
                        Provider = "SoundCloud",
                        Uri = svideo.StreamLink,
                        ProviderType = MusicType.Normal,
                        Query = svideo.TrackLink,
                    });
                }

                var link = await SearchHelper.FindYoutubeUrlByKeywords(query).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(link))
                    throw new OperationCanceledException("Not a valid youtube query.");
                var allVideos = await Task.Factory.StartNew(async () => await YouTube.Default.GetAllVideosAsync(link).ConfigureAwait(false)).Unwrap().ConfigureAwait(false);
                var videos = allVideos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
                var video = videos
                    .Where(v => v.AudioBitrate < 192)
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
                    Uri = video.Uri,
                    Query = link,
                    ProviderType = musicType,
                });
                song.SkipTo = gotoTime;
                return song;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed resolving the link.{ex.Message}");
                return null;
            }
        }

        private static async Task<string> HandleStreamContainers(string query)
        {
            string file = null;
            try
            {
                file = await SearchHelper.GetResponseStringAsync(query).ConfigureAwait(false);
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
                    Console.WriteLine($"Failed reading .pls:\n{file}");
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
                    Console.WriteLine($"Failed reading .m3u:\n{file}");
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
                    Console.WriteLine($"Failed reading .asx:\n{file}");
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
                    Console.WriteLine($"Failed reading .xspf:\n{file}");
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
