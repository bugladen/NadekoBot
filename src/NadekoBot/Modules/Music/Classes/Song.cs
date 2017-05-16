using Discord.Audio;
using NadekoBot.Extensions;
using NLog;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace NadekoBot.Modules.Music.Classes
{
    public class SongInfo
    {
        public string Provider { get; set; }
        public MusicType ProviderType { get; set; }
        public string Query { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
        public string AlbumArt { get; set; }
    }

    public class Song
    {
        public SongInfo SongInfo { get; }
        public MusicPlayer MusicPlayer { get; set; }

        private string _queuerName;
        public string QueuerName { get{
            return Discord.Format.Sanitize(_queuerName);
        } set { _queuerName = value; } }

        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;
        public TimeSpan CurrentTime => TimeSpan.FromSeconds(bytesSent / (float)_frameBytes / (1000 / (float)_milliseconds));

        private const int _milliseconds = 20;
        private const int _samplesPerFrame = (48000 / 1000) * _milliseconds;
        private const int _frameBytes = 3840; //16-bit, 2 channels

        private ulong bytesSent { get; set; }

        //pwetty

        public string PrettyProvider =>
            $"{(SongInfo.Provider ?? "???")}";

        public string PrettyFullTime => PrettyCurrentTime + " / " + PrettyTotalTime;

        public string PrettyName => $"**[{SongInfo.Title.TrimTo(65)}]({SongUrl})**";

        public string PrettyInfo => $"{MusicPlayer.PrettyVolume} | {PrettyTotalTime} | {PrettyProvider} | {QueuerName}";

        public string PrettyFullName => $"{PrettyName}\n\t\t`{PrettyTotalTime} | {PrettyProvider} | {QueuerName}`";

        public string PrettyCurrentTime {
            get {
                var time = CurrentTime.ToString(@"mm\:ss");
                var hrs = (int)CurrentTime.TotalHours;

                if (hrs > 0)
                    return hrs + ":" + time;
                else
                    return time;
            }
        }

        public string PrettyTotalTime {
            get
            {
                if (TotalTime == TimeSpan.Zero)
                    return "(?)";
                if (TotalTime == TimeSpan.MaxValue)
                    return "∞";
                var time = TotalTime.ToString(@"mm\:ss");
                var hrs = (int)TotalTime.TotalHours;

                if (hrs > 0)
                    return hrs + ":" + time;
                return time;
            }
        }

        public string Thumbnail {
            get {
                switch (SongInfo.ProviderType)
                {
                    case MusicType.Radio:
                        return "https://cdn.discordapp.com/attachments/155726317222887425/261850925063340032/1482522097_radio.png"; //test links
                    case MusicType.Normal:
                        //todo have videoid in songinfo from the start
                        var videoId = Regex.Match(SongInfo.Query, "<=v=[a-zA-Z0-9-]+(?=&)|(?<=[0-9])[^&\n]+|(?<=v=)[^&\n]+");
                        return $"https://img.youtube.com/vi/{ videoId }/0.jpg";
                    case MusicType.Local:
                        return "https://cdn.discordapp.com/attachments/155726317222887425/261850914783100928/1482522077_music.png"; //test links
                    case MusicType.Soundcloud:
                        return SongInfo.AlbumArt;
                    default:
                        return "";
                }
            }
        }

        public string SongUrl {
            get {
                switch (SongInfo.ProviderType)
                {
                    case MusicType.Normal:
                        return SongInfo.Query;
                    case MusicType.Soundcloud:
                        return SongInfo.Query;
                    case MusicType.Local:
                        return $"https://google.com/search?q={ WebUtility.UrlEncode(SongInfo.Title).Replace(' ', '+') }";
                    case MusicType.Radio:
                        return $"https://google.com/search?q={SongInfo.Title}";
                    default:
                        return "";
                }
            }
        }

        public int SkipTo { get; set; }

        private readonly Logger _log;

        public Song(SongInfo songInfo)
        {
            SongInfo = songInfo;
            _log = LogManager.GetCurrentClassLogger();
        }

        public Song Clone()
        {
            var s = new Song(SongInfo)
            {
                MusicPlayer = MusicPlayer,
                QueuerName = QueuerName
            };
            return s;
        }

        public async Task Play(IAudioClient voiceClient, CancellationToken cancelToken)
        {
            bytesSent = (ulong) SkipTo * 3840 * 50;
            var filename = Path.Combine(Music.MusicDataPath, DateTime.Now.UnixTimestamp().ToString());

            var inStream = new SongBuffer(MusicPlayer, filename, SongInfo, SkipTo, _frameBytes * 100);
            var bufferTask = inStream.BufferSong(cancelToken).ConfigureAwait(false);

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
                _log.Debug("Prebuffering successfully completed in " + sw.Elapsed);

                var outStream = voiceClient.CreatePCMStream(960);

                int nextTime = Environment.TickCount + _milliseconds;

                byte[] buffer = new byte[_frameBytes];
                while (!cancelToken.IsCancellationRequested && //song canceled for whatever reason
                    !(MusicPlayer.MaxPlaytimeSeconds != 0 && CurrentTime.TotalSeconds >= MusicPlayer.MaxPlaytimeSeconds)) // or exceedded max playtime
                {
                    //Console.WriteLine($"Read: {songBuffer.ReadPosition}\nWrite: {songBuffer.WritePosition}\nContentLength:{songBuffer.ContentLength}\n---------");
                    var read = await inStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    //await inStream.CopyToAsync(voiceClient.OutputStream);
                    if (read < _frameBytes)
                        _log.Debug("read {0}", read);
                    unchecked
                    {
                        bytesSent += (ulong)read;
                    }
                    if (read < _frameBytes)
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
                                nextTime = Environment.TickCount + _milliseconds;
                            }
                            else
                            {
                                await Task.Delay(100, cancelToken).ConfigureAwait(false);
                                nextTime = Environment.TickCount + _milliseconds;
                            }
                        }
                        else
                            attempt = 0;
                    }
                    else
                        attempt = 0;

                    while (MusicPlayer.Paused)
                    {
                        await Task.Delay(200, cancelToken).ConfigureAwait(false);
                        nextTime = Environment.TickCount + _milliseconds;
                    }


                    buffer = AdjustVolume(buffer, MusicPlayer.Volume);
                    if (read != _frameBytes) continue;
                    nextTime = unchecked(nextTime + _milliseconds);
                    int delayMillis = unchecked(nextTime - Environment.TickCount);
                    if (delayMillis > 0)
                        await Task.Delay(delayMillis, cancelToken).ConfigureAwait(false);
                    await outStream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                }
            }
            finally
            {
                await bufferTask;
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

        //aidiakapi ftw
        public static unsafe byte[] AdjustVolume(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1f) < 0.0001f) return audioSamples;

            // 16-bit precision for the multiplication
            var volumeFixed = (int)Math.Round(volume * 65536d);

            var count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples)
            {
                var src = (short*)srcBytes;

                for (var i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }

            return audioSamples;
        }
    }
}