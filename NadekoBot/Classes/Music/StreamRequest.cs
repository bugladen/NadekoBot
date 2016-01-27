using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Audio;
using YoutubeExtractor;
using NadekoBot.Modules;
using System.IO;
using System.Diagnostics;
using NadekoBot.Extensions;
using System.Threading;
using Timer = System.Timers.Timer;

namespace NadekoBot.Classes.Music {
    public enum StreamState {
        Resolving,
        Queued,
        Buffering, //not using it atm
        Playing,
        Completed
    }

    public class StreamRequest {
        public Channel Channel { get; }
        public Server Server { get; }
        public User User { get; }
        public string Query { get; }

        private MusicStreamer musicStreamer = null;
        public StreamState State => musicStreamer?.State ?? StreamState.Resolving;

        public StreamRequest(CommandEventArgs e, string query) {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (e.User.VoiceChannel == null)
                throw new NullReferenceException("Voicechannel is null.");

            this.Server = e.Server;
            this.Channel = e.User.VoiceChannel;
            this.User = e.User;
            this.Query = query;
            ResolveStreamLink();
        }

        private Task ResolveStreamLink() =>
            Task.Run(() => {
                Console.WriteLine("Resolving video link");
                var video = DownloadUrlResolver.GetDownloadUrls(Searches.FindYoutubeUrlByKeywords(Query))
                        .Where(v => v.AdaptiveType == AdaptiveType.Audio)
                        .OrderByDescending(v => v.AudioBitrate).FirstOrDefault();

                if (video == null) // do something with this error
                    throw new Exception("Could not load any video elements");

                Title = video.Title;

                musicStreamer = new MusicStreamer(this, video.DownloadUrl, Channel);
                OnQueued();
            });

        internal void Pause() {
            throw new NotImplementedException();
        }

        public string Title { get; internal set; } = String.Empty;

        public Action OnQueued = null;
        public Action OnBuffering = null;
        public Action OnStarted = null;
        public Action OnCompleted = null;

        //todo maybe add remove, in order to create remove at position command

        internal void Cancel() {
            musicStreamer?.StopPlayback();
        }

        internal Task Start() =>
            Task.Run(async () => {
                Console.WriteLine("Start called.");

                int attemptsLeft = 7;
                //wait for up to 7 seconds to resolve a link
                while (State == StreamState.Resolving) {
                    await Task.Delay(1000);
                    Console.WriteLine("Resolving...");
                    if (--attemptsLeft == 0) {
                        Console.WriteLine("Resolving timed out.");
                        return;
                    }
                }
                try {
                    await musicStreamer.StartPlayback();
                } catch (Exception ex) {
                    Console.WriteLine("Error in start playback." + ex);
                }
            });
    }

    public class MusicStreamer {
        private Channel channel;
        private DualStream buffer;

        public StreamState State { get; internal set; }
        public string Url { get; private set; }

        StreamRequest parent;
        private readonly object _bufferLock = new object();
        private CancellationTokenSource cancelSource;

        public static Timer logTimer = new Timer();

        public MusicStreamer(StreamRequest parent, string directUrl, Channel channel) {
            this.parent = parent;
            this.channel = channel;
            this.buffer = new DualStream();
            this.Url = directUrl;
            Console.WriteLine("Created new streamer");
            State = StreamState.Queued;
            cancelSource = new CancellationTokenSource();

            if (!logTimer.Enabled) {
                logTimer.Interval = 5000;
                logTimer.Start();
            }
            logTimer.Elapsed += LogTimer_Elapsed;
        }

        private void LogTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            if (cancelSource.IsCancellationRequested) {
                logTimer.Elapsed -= LogTimer_Elapsed;
                return;
            }
            Console.WriteLine($"Music stats for {string.Join("", parent.Title.Take(parent.Title.Length > 10 ? 10 : parent.Title.Length))}");
            Console.WriteLine($"Server: {parent.Server.Name}");
            Console.WriteLine($"Title:  - Length:{buffer.Length * 1.0f / 1.MB()}MB Status: {State} - Canceled: { cancelSource.IsCancellationRequested}");
            Console.WriteLine("--------------------------------");
        }

        //todo app will crash if song is too long, should load only next 20-ish seconds
        private async Task BufferSong() {
            Console.WriteLine("Buffering...");
            //start feeding the buffer
            var p = Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-i {Url} -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
            while (true) {
                while (buffer.writePos - buffer.readPos > 2.MB() && !cancelSource.IsCancellationRequested) {
                    await Task.Delay(1000);
                }

                if (cancelSource.IsCancellationRequested) {
                    try {
                        p.CancelOutputRead();
                        p.Close();
                    } catch (Exception) { }
                    return;
                }

                if (buffer.Length > 5.MB()) { // if buffer is over 10 MB, create new one
                    Console.WriteLine("Buffer over 10 megs, clearing.");

                    var skip = 2.MB();
                    byte[] data = buffer.ToArray().Skip(skip).ToArray();
                    
                    lock (_bufferLock) {
                        var newWritePos = buffer.writePos - skip;
                        var newReadPos = buffer.readPos - skip;
                        var newPos = buffer.Position - skip;

                        buffer = new DualStream();
                        buffer.Write(data, 0, data.Length);

                        buffer.writePos = newWritePos;
                        buffer.readPos = newReadPos;
                        buffer.Position = newPos;
                    }
                    
                }
                
                var buf = new byte[1024];
                int read = 0;
                read = await p.StandardOutput.BaseStream.ReadAsync(buf, 0, 1024);
                
                if (read == 0) {
                    try {
                        p.CancelOutputRead();
                        p.Close();
                    } catch (Exception) { }

                    Console.WriteLine("Didn't read anything from the stream");
                    return;
                }
                await buffer.WriteAsync(buf, 0, read);
            }
        }

        internal Task StartPlayback() =>
            Task.Run(async () => {
                Console.WriteLine("Starting playback.");
                State = StreamState.Playing;
                if (parent.OnBuffering != null)
                    parent.OnBuffering();
                Task.Run(async () => await BufferSong());
                await Task.Delay(2000,cancelSource.Token);
                if (parent.OnStarted != null)
                    parent.OnStarted();
                Console.WriteLine("Prebuffering complete.");
                //for now wait for 3 seconds before starting playback.

                var audio = NadekoBot.client.Audio();
                
                var voiceClient = await audio.Join(channel);
                int blockSize = 1920 * NadekoBot.client.Audio().Config.Channels;
                byte[] voiceBuffer = new byte[blockSize];

                while (true) {
                    int readCount = 0;
                    lock (_bufferLock) {
                         readCount = buffer.Read(voiceBuffer, 0, voiceBuffer.Length);
                    }

                    if (readCount == 0) {
                        Console.WriteLine("Nothing to read, stream finished.");
                        break;
                    }

                   // while (MusicControls.IsPaused && !cancelSource.IsCancellationRequested)
                   //     await Task.Delay(100);

                    if (cancelSource.IsCancellationRequested) {
                        Console.WriteLine("Canceled");
                        break;
                    }

                    voiceClient.Send(voiceBuffer, 0, voiceBuffer.Length);
                }

                voiceClient.Wait();
                await voiceClient.Disconnect();
                if (parent.OnCompleted != null)
                    parent.OnCompleted();
                State = StreamState.Completed;
                Console.WriteLine("Song completed.");
            });

        internal void StopPlayback() {
            Console.WriteLine("Stopping playback");
            State = StreamState.Completed;
            if(cancelSource.Token.CanBeCanceled)
                cancelSource.Cancel();
        }
    }

    public class DualStream : MemoryStream {
        public long readPos;
        public long writePos;

        public DualStream() : base() { }
        public DualStream(byte[] data) : base(data) { }

        public override int Read(byte[] buffer, int offset, int count) {
            int read;
            lock (this) {
                Position = readPos;
                read = base.Read(buffer, offset, count);
                readPos = Position;
            }
            return read;
        }
        public override void Write(byte[] buffer, int offset, int count) {
            lock (this) {
                Position = writePos;
                base.Write(buffer, offset, count);
                writePos = Position;
            }
        }
    }

}
