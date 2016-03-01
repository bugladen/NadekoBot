/*
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Audio;
using System.IO;
using System.Diagnostics;
using NadekoBot.Extensions;
using VideoLibrary;

namespace NadekoBot.Classes.Music {

    public class StreamRequest {
        public Server Server { get; }
        public User User { get; }
        public string Query { get; }

        public string Title { get; internal set; } = String.Empty;
        private string Provider { get; set; }

        public string FullPrettyName => $"**【 {Title.TrimTo(55)} 】**`{(Provider == null ? "-" : Provider)}`";

        private MusicStreamer musicStreamer = null;
        public StreamState State => musicStreamer?.State ?? privateState;
        private StreamState privateState = StreamState.Resolving;

        public bool IsPaused => MusicControls.IsPaused;

        public float Volume => MusicControls?.Volume ?? 1.0f;

        public MusicType LinkType { get; }

        public MusicPlayer MusicControls;

        public StreamRequest(CommandEventArgs e, string query, MusicPlayer mc, MusicType musicType = MusicType.Normal) {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            this.MusicControls = mc;
            this.Server = e.Server;
            this.Query = query;
            this.LinkType = musicType;
            mc.AddSong(this);
        }

        public async Task Resolve() {
            string uri = null;
            

            musicStreamer = new MusicStreamer(this, uri);
            musicStreamer.OnCompleted += () => {
                OnCompleted();
            };
            OnQueued();
        }

        internal string PrintStats() => musicStreamer?.Stats();

        public event Action OnQueued = delegate { };
        public event Action OnStarted = delegate { };
        public event Action OnCompleted = delegate { };
        public event Action<string> OnResolvingFailed = delegate { };

        internal void Cancel() {
            musicStreamer?.Cancel();
        }

        internal void Stop() {
            musicStreamer?.Stop();
        }

        protected void Complete() {
            OnCompleted();
        }

        internal async Task Start() {
            int attemptsLeft = 4;
            //wait for up to 4 seconds to resolve a link
            try {
                while (State == StreamState.Resolving) {
                    await Task.Delay(1000);
                    if (--attemptsLeft == 0) {
                        throw new TimeoutException("Resolving timed out.");
                    }
                }
                OnStarted();
                await musicStreamer.StartPlayback();
            }
            catch (TimeoutException) {
                Console.WriteLine("Resolving timed out.");
                privateState = StreamState.Completed;
            }
            catch (Exception ex) {
                Console.WriteLine("Error in start playback." + ex.Message);
                privateState = StreamState.Completed;
            }
        }
    }

    public class MusicStreamer {
        private DualStream buffer;

        public StreamState State { get; internal set; }
        public string Url { get; }
        private bool IsCanceled { get; set; }
        public bool IsPaused => parent.IsPaused;

        public event Action OnCompleted = delegate { };

        StreamRequest parent;
        private readonly object _bufferLock = new object();
        private bool prebufferingComplete = false;

        public MusicStreamer(StreamRequest parent, string directUrl) {
            this.parent = parent;
            this.buffer = new DualStream();
            this.Url = directUrl;
            State = StreamState.Queued;
        }

        public string Stats() =>
            "--------------------------------\n" +
            $"Music stats for {string.Join("", parent.Title.TrimTo(50))}\n" +
            $"Server: {parent.Server.Name}\n" +
            $"Length:{buffer.Length * 1.0f / 1.MB()}MB Status: {State}\n" +
            "--------------------------------\n";

        private async Task BufferSong() {
            //start feeding the buffer

            Process p = null;
            try {
                var psi = new ProcessStartInfo {
                    FileName = "ffmpeg",
                    Arguments = $"-i {Url} -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet", //+ (NadekoBot.IsLinux ? "2> /dev/null" : "2>NUL"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };
                using (p = Process.Start(psi)) {
                    int attempt = 0;
                    while (true) {
                        while (buffer.writePos - buffer.readPos > 5.MB() && State != StreamState.Completed) {
                            prebufferingComplete = true;
                            await Task.Delay(200);
                        }

                        if (State == StreamState.Completed) {
                            Console.WriteLine("Buffering canceled, stream is completed.");
                            try {
                                p.CancelOutputRead();
                            }
                            catch { }
                            try {
                                p.Close();
                            }
                            catch { }
                            p.Dispose();
                            return;
                        }
                        if (buffer.readPos > 5.MiB() && buffer.writePos > 5.MiB()) {
                            var skip = 5.MB();
                            lock (_bufferLock) {
                                byte[] data = new byte[buffer.Length - skip];
                                Buffer.BlockCopy(buffer.GetBuffer(), skip, data, 0, (int)(buffer.Length - skip));
                                var newReadPos = buffer.readPos - skip;
                                var newPos = buffer.Position - skip;
                                buffer = new DualStream();
                                buffer.Write(data, 0, data.Length);
                                buffer.readPos = newReadPos;
                                buffer.Position = newPos;
                            }
                        }
                        int blockSize = 1920 * NadekoBot.client.GetService<AudioService>()?.Config?.Channels ?? 3840;
                        var buf = new byte[blockSize];
                        int read = 0;
                        read = await p.StandardOutput.BaseStream.ReadAsync(buf, 0, blockSize);
                        //Console.WriteLine($"Read: {read}");
                        if (read == 0) {
                            if (attempt == 5) {
                                Console.WriteLine($"Didn't read anything from the stream for {attempt} attempts. {buffer.Length / 1.MB()}MB length");
                                try {
                                    p.CancelOutputRead();
                                }
                                catch { }
                                try {
                                    p.Close();
                                }
                                catch { }
                                p.Dispose();
                                return;
                            }
                            else {
                                ++attempt;
                                await Task.Delay(20);
                            }
                        }
                        else {
                            attempt = 0;
                            lock (_bufferLock) {
                                buffer.Write(buf, 0, read);
                            }
                        }
                    }
                }
            }
            catch { }
            finally {
                if (p != null) {
                    p.Dispose();
                    p = null;
                }
            }
        }

        internal async Task StartPlayback() {
            Console.WriteLine("Starting playback.");
            if (State == StreamState.Playing) return;
            State = StreamState.Playing;

            Task.Factory.StartNew(async () => {
                await BufferSong();
            }).ConfigureAwait(false);

            // prebuffering wait stuff start
            int bufferAttempts = 0;
            int waitPerAttempt = 500;
            int toAttemptTimes = parent.LinkType != MusicType.Normal ? 4 : 8;
            while (!prebufferingComplete && bufferAttempts++ < toAttemptTimes) {
                await Task.Delay(waitPerAttempt);
            }
            if (prebufferingComplete) {
                Console.WriteLine($"Prebuffering finished in {bufferAttempts * 500}");
            }
            // prebuffering wait stuff end

            if (buffer.Length > 0) {
                Console.WriteLine("Prebuffering complete.");
            }
            else {
                Console.WriteLine("Nothing was buffered, try another song and check your GoogleApikey.");
            }

            int blockSize = 1920 * NadekoBot.client.GetService<AudioService>()?.Config?.Channels ?? 3840;
            byte[] voiceBuffer = new byte[blockSize];

            int attempt = 0;
            while (!IsCanceled) {
                int readCount = 0;
                //adjust volume

                lock (_bufferLock) {
                    readCount = buffer.Read(voiceBuffer, 0, blockSize);
                }

                if (readCount == 0) {
                    if (attempt == 4) {
                        Console.WriteLine($"Failed to read {attempt} times. Breaking out.");
                        break;
                    }
                    else {
                        ++attempt;
                        await Task.Delay(15);
                    }
                }
                else
                    attempt = 0;

                if (State == StreamState.Completed) {
                    Console.WriteLine("Canceled");
                    break;
                }
                voiceBuffer = adjustVolume(voiceBuffer, parent.Volume);
                parent.MusicControls.VoiceClient.Send(voiceBuffer, 0, readCount);

                while (IsPaused) {
                    await Task.Delay(100);
                }
            }
            Stop();
        }

        internal void Cancel() {
            IsCanceled = true;
        }

        internal void Stop() {
            if (State == StreamState.Completed) return;
            var oldState = State;
            State = StreamState.Completed;
            if (oldState == StreamState.Playing)
                OnCompleted();
        }
    }

    public class DualStream : MemoryStream {
        public long readPos;
        public long writePos;

        public DualStream()  {
            readPos = writePos = 0;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            Position = readPos;
            int read = base.Read(buffer, offset, count);
            readPos = Position;
            return read;
        }
        public override void Write(byte[] buffer, int offset, int count) {
            Position = writePos;
            base.Write(buffer, offset, count);
            writePos = Position;
        }
    }
}
*/