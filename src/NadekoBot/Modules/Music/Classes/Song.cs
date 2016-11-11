using Discord.Audio;
using NadekoBot.Extensions;
using NLog;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoLibrary;

namespace NadekoBot.Modules.Music.Classes
{
    public class SongInfo
    {
        public string Provider { get; set; }
        public MusicType ProviderType { get; set; }
        /// <summary>
        /// Will be set only if the providertype is normal
        /// </summary>
        public string Query { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
    }
    public class Song
    {
        public StreamState State { get; set; }
        public string PrettyName =>
            $"**【 {SongInfo.Title.TrimTo(55)} 】**`{(SongInfo.Provider ?? "-")}` `by {QueuerName}`";
        public SongInfo SongInfo { get; }
        public string QueuerName { get; set; }

        public MusicPlayer MusicPlayer { get; set; }

        public string PrettyCurrentTime()
        {
            var time = TimeSpan.FromSeconds(bytesSent / 3840 / 50);
            var str = $"【{(int)time.TotalMinutes}m {time.Seconds}s】**/** ";
            if (TotalLength == TimeSpan.Zero)
                str += "**?**";
            else if (TotalLength == TimeSpan.MaxValue)
                str += "**∞**";
            else
                str += $"【{(int)TotalLength.TotalMinutes}m {TotalLength.Seconds}s】";
            return str;
        }

        const int milliseconds = 20;
        const int samplesPerFrame = (48000 / 1000) * milliseconds;
        const int frameBytes = 3840; //16-bit, 2 channels

        private ulong bytesSent { get; set; } = 0;

        public bool PrintStatusMessage { get; set; } = true;

        private int skipTo = 0;
        private Logger _log;

        public int SkipTo {
            get { return skipTo; }
            set {
                skipTo = value;
                bytesSent = (ulong)skipTo * 3840 * 50;
            }
        }

        public TimeSpan TotalLength { get; set; } = TimeSpan.Zero;

        public Song(SongInfo songInfo)
        {
            this.SongInfo = songInfo;
            this._log = LogManager.GetCurrentClassLogger();
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

        public async Task Play(IAudioClient voiceClient, CancellationToken cancelToken)
        {
            var filename = Path.Combine(Music.MusicDataPath, DateTime.Now.UnixTimestamp().ToString());

            SongBuffer inStream = new SongBuffer(MusicPlayer, filename, SongInfo, skipTo, frameBytes * 100);
            var bufferTask = inStream.BufferSong(cancelToken).ConfigureAwait(false);

            bytesSent = 0;

            try
            {
                var attempt = 0;             

                var prebufferingTask = CheckPrebufferingAsync(inStream, cancelToken, 1.MiB()); //Fast connection can do this easy
                var finished = false;
                var count = 0;
                var sw = new Stopwatch();
                var slowconnection = false;
                sw.Start();
                while (!finished)
                {
                    var t = await Task.WhenAny(prebufferingTask, Task.Delay(2000, cancelToken));
                    if (t != prebufferingTask)
                    {
                        count++;
                        if (count == 10)
                        {
                            slowconnection = true;
                            prebufferingTask = CheckPrebufferingAsync(inStream, cancelToken, 20.MiB());
                            _log.Warn("Slow connection buffering more to ensure no disruption, consider hosting in cloud");
                            continue;
                        }
                        
                        if (inStream.BufferingCompleted && count == 1)
                        {
                            _log.Debug("Prebuffering canceled. Cannot get any data from the stream.");
                            return;
                        }
                        else
                        {
                            continue;
                        }
                     }
                    else if (prebufferingTask.IsCanceled)
                    {
                        _log.Debug("Prebuffering canceled. Cannot get any data from the stream.");
                        return;
                    }
                    finished = true;
                }
                sw.Stop();
                _log.Debug("Prebuffering successfully completed in "+ sw.Elapsed);

                var outStream = voiceClient.CreatePCMStream(960);

                int nextTime = Environment.TickCount + milliseconds;

                byte[] buffer = new byte[frameBytes];
                while (!cancelToken.IsCancellationRequested)
                {
                    //Console.WriteLine($"Read: {songBuffer.ReadPosition}\nWrite: {songBuffer.WritePosition}\nContentLength:{songBuffer.ContentLength}\n---------");
                    var read = await inStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    //await inStream.CopyToAsync(voiceClient.OutputStream);
                    if(read < frameBytes)
                        _log.Debug("read {0}", read);
                    unchecked
                    {
                        bytesSent += (ulong)read;
                    }
                    if (read < frameBytes)
                    {
                        if (read == 0)
                        {
                            if (inStream.BufferingCompleted)
                                break;
                            if (attempt++ == 20)
                            {
                                MusicPlayer.SongCancelSource.Cancel();
                                break;
                            }
                            if (slowconnection)
                            {
                                _log.Warn("Slow connection has disrupted music, waiting a bit for buffer");
                                await Task.Delay(1000, cancelToken).ConfigureAwait(false);
                            }
                            else
                                await Task.Delay(100, cancelToken).ConfigureAwait(false);
                        }
                        else
                            attempt = 0;
                    }
                    else
                        attempt = 0;

                    while (this.MusicPlayer.Paused)
                        await Task.Delay(200, cancelToken).ConfigureAwait(false);


                    buffer = AdjustVolume(buffer, MusicPlayer.Volume);
                    if (read != frameBytes) continue;
                    nextTime = unchecked(nextTime + milliseconds);
                    int delayMillis = unchecked(nextTime - Environment.TickCount);
                    if (delayMillis > 0)
                        await Task.Delay(delayMillis, cancelToken).ConfigureAwait(false);
                    await outStream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                }
            }
            finally
            {
                await bufferTask;
                if(inStream != null)
                    inStream.Dispose();
            }
        }

        private async Task CheckPrebufferingAsync(SongBuffer inStream, CancellationToken cancelToken, long size)
        {
            while (!inStream.BufferingCompleted && inStream.Length < size)
            {
                await Task.Delay(100, cancelToken);
            }
            _log.Debug("Buffering successfull");
        }

        /*
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
        */

        //aidiakapi ftw
        public unsafe static byte[] AdjustVolume(byte[] audioSamples, float volume)
        {
            Contract.Requires(audioSamples != null);
            Contract.Requires(audioSamples.Length % 2 == 0);
            Contract.Requires(volume >= 0f && volume <= 1f);
            Contract.Assert(BitConverter.IsLittleEndian);

            if (Math.Abs(volume - 1f) < 0.0001f) return audioSamples;

            // 16-bit precision for the multiplication
            int volumeFixed = (int)Math.Round(volume * 65536d);

            int count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples)
            {
                short* src = (short*)srcBytes;

                for (int i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }

            return audioSamples;
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
                        })
                        { TotalLength = TimeSpan.MaxValue };
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
                    })
                    { TotalLength = TimeSpan.FromMilliseconds(svideo.Duration) };
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
                    })
                    { TotalLength = TimeSpan.FromMilliseconds(svideo.Duration) };
                }

                var link = (await NadekoBot.Google.GetVideosByKeywordsAsync(query).ConfigureAwait(false)).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(link))
                    throw new OperationCanceledException("Not a valid youtube query.");
                var allVideos = await Task.Run(async () => { try { return await YouTube.Default.GetAllVideosAsync(link).ConfigureAwait(false); } catch { return Enumerable.Empty<YouTubeVideo>(); } }).ConfigureAwait(false);
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
