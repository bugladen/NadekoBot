using Discord.Audio;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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

        private bool bufferingCompleted { get; set; } = false;
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

        private Task BufferSong(string filename, CancellationToken cancelToken) =>
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
                    var prebufferSize = 100ul.MiB();
                    using (var outStream = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        byte[] buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false)) != 0)
                        {
                            await outStream.WriteAsync(buffer, 0, bytesRead, cancelToken).ConfigureAwait(false);
                            while ((ulong)outStream.Length - bytesSent > prebufferSize)
                                await Task.Delay(100, cancelToken);
                        }
                    }
                        
                    bufferingCompleted = true;
                }
                catch (System.ComponentModel.Win32Exception) {
                    var oldclr = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(@"You have not properly installed or configured FFMPEG. 
Please install and configure FFMPEG to play music. 
Check the guides for your platform on how to setup ffmpeg correctly:
    Windows Guide: https://goo.gl/SCv72y
    Linux Guide:  https://goo.gl/rRhjCp");
                    Console.ForegroundColor = oldclr;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Buffering stopped: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine($"Buffering done.");
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
            var filename = Path.Combine(MusicModule.MusicDataPath, DateTime.Now.UnixTimestamp().ToString());

            var bufferTask = BufferSong(filename, cancelToken).ConfigureAwait(false);

            var inStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);

            bytesSent = 0;

            try
            {
                var prebufferingTask = CheckPrebufferingAsync(inStream, cancelToken);
                var sw = new Stopwatch();
                sw.Start();
                var t = await Task.WhenAny(prebufferingTask, Task.Delay(5000, cancelToken));
                if (t != prebufferingTask)
                {
                    Console.WriteLine("Prebuffering timed out or canceled. Cannot get any data from the stream.");
                    return;
                }
                else if(prebufferingTask.IsCanceled)
                {
                    Console.WriteLine("Prebuffering timed out. Cannot get any data from the stream.");
                    return;
                }
                sw.Stop();
                Console.WriteLine("Prebuffering successfully completed in "+ sw.Elapsed);

                const int blockSize = 3840;
                var attempt = 0;
                byte[] buffer = new byte[blockSize];
                while (!cancelToken.IsCancellationRequested)
                {
                    //Console.WriteLine($"Read: {songBuffer.ReadPosition}\nWrite: {songBuffer.WritePosition}\nContentLength:{songBuffer.ContentLength}\n---------");
                    var read = inStream.Read(buffer, 0, buffer.Length);
                    //await inStream.CopyToAsync(voiceClient.OutputStream);
                    unchecked
                    {
                        bytesSent += (ulong)read;
                    }
                    if (read == 0)
                        if (attempt++ == 20)
                        {
                            voiceClient.Wait();
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
            }
            finally
            {
                await bufferTask;
                await Task.Run(() => voiceClient.Clear());
                inStream.Dispose();
                try { File.Delete(filename); } catch { }
            }
        }

        private async Task CheckPrebufferingAsync(Stream inStream, CancellationToken cancelToken)
        {
            while (!bufferingCompleted && inStream.Length < 2.MiB())
            {
                await Task.Delay(100, cancelToken);
            }
            Console.WriteLine("Buffering successfull");
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
