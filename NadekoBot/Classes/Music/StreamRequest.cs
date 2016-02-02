using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Audio;
using NadekoBot.Modules;
using System.IO;
using System.Diagnostics;
using NadekoBot.Extensions;
using System.Threading;
using Timer = System.Timers.Timer;
using VideoLibrary;

namespace NadekoBot.Classes.Music {
    public enum StreamState {
        Resolving,
        Queued,
        Buffering, //not using it atm
        Playing,
        Completed
    }

    public class StreamRequest {
        public Server Server { get; }
        public User User { get; }
        public string Query { get; }

        public string Title { get; internal set; } = String.Empty;

        private MusicStreamer musicStreamer = null;
        public StreamState State => musicStreamer?.State ?? privateState;
        private StreamState privateState = StreamState.Resolving;

        public bool IsPaused => MusicControls.IsPaused;

        public MusicControls MusicControls;

        public StreamRequest(CommandEventArgs e, string query, MusicControls mc) {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            this.MusicControls = mc;
            this.Server = e.Server;
            this.Query = query;
            Task.Run(() => ResolveStreamLink());
            mc.SongQueue.Add(this);
        }

        private void ResolveStreamLink() {
            Video video = null;
            try {
                if (OnResolving != null)
                    OnResolving();
                Console.WriteLine("Resolving video link");

                video = YouTube.Default.GetAllVideos(Searches.FindYoutubeUrlByKeywords(Query))
                        .Where(v => v.AdaptiveKind == AdaptiveKind.Audio)
                        .OrderByDescending(v => v.AudioBitrate)
                        .FirstOrDefault();

                if (video == null) // do something with this error
                    throw new Exception("Could not load any video elements based on the query.");

                Title = video.Title; //.Substring(0,video.Title.Length-10); // removing trailing "- You Tube"
            } catch (Exception ex) {
                privateState = StreamState.Completed;
                if (OnResolvingFailed != null)
                    OnResolvingFailed(ex.Message);
                Console.WriteLine($"Failed resolving the link.{ex.Message}");
                return;
            }

            musicStreamer = new MusicStreamer(this, video.Uri);
            if (OnQueued != null)
                OnQueued();
            return;
        }

        internal string PrintStats() => musicStreamer?.Stats();

        public Action OnQueued = null;
        public Action OnBuffering = null;
        public Action OnStarted = null;
        public Action OnCompleted = null;
        public Action OnResolving = null;
        public Action<string> OnResolvingFailed = null;

        internal void Cancel() {
            musicStreamer?.Cancel();
        }

        internal void Stop() {
            musicStreamer?.Stop();
        }

        internal async Task Start() {
            Console.WriteLine("Start called.");

            int attemptsLeft = 4;
            //wait for up to 4 seconds to resolve a link
            try {
                while (State == StreamState.Resolving) {
                    await Task.Delay(1000);
                    Console.WriteLine("Resolving...");
                    if (--attemptsLeft == 0) {
                        throw new TimeoutException("Resolving timed out.");
                    }
                }
                await musicStreamer.StartPlayback();
            } catch (TimeoutException) {
                Console.WriteLine("Resolving timed out.");
                privateState = StreamState.Completed;
            } catch (Exception ex) {
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

        StreamRequest parent;
        private readonly object _bufferLock = new object();
        private bool prebufferingComplete = false;

        public MusicStreamer(StreamRequest parent, string directUrl) {
            this.parent = parent;
            this.buffer = new DualStream();
            this.Url = directUrl;
            Console.WriteLine("Created new streamer");
            State = StreamState.Queued;
        }

        public string Stats() =>
            "--------------------------------\n" +
            $"Music stats for {string.Join("", parent.Title.TrimTo(50))}\n" +
            $"Server: {parent.Server.Name}\n" +
            $"Length:{buffer.Length * 1.0f / 1.MB()}MB Status: {State}\n" +
            "--------------------------------\n";
        
        private async Task BufferSong() {
            Console.WriteLine("Buffering...");
            //start feeding the buffer
            var p = Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-i {Url} -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
            int attempt = 0;
            while (true) {

                //wait for the read pos to catch up with write pos
                while (buffer.writePos - buffer.readPos > 5.MB() && State != StreamState.Completed) {
                    prebufferingComplete = true;
                    await Task.Delay(500);
                }

                if (State == StreamState.Completed) {
                    try {
                        p.CancelOutputRead();
                        p.Close();
                    } catch (Exception) { }
                    Console.WriteLine("Buffering canceled, stream is completed.");
                    return;
                }

                if (buffer.readPos > 5.MiB()) { // if buffer is over 5 MiB, create new one
                    Console.WriteLine("Buffer over 5 megs, clearing.");

                    var skip = 5.MB(); //remove only 5 MB, just in case
                    var newBuffer = new DualStream();

                    lock (_bufferLock) {
                        byte[] data = buffer.ToArray().Skip(skip).ToArray();
                        var newReadPos = buffer.readPos - skip;
                        var newPos = buffer.Position - skip;
                        buffer = newBuffer;
                        buffer.Write(data, 0, data.Length);
                        buffer.readPos = newReadPos;
                        buffer.Position = newPos;
                    }
                }             

                var buf = new byte[1024];
                int read = 0;
                read = await p.StandardOutput.BaseStream.ReadAsync(buf, 0, 1024);
                //Console.WriteLine($"Read: {read}");
                if (read == 0) {
                    if (attempt == 5) {
                        try {
                            p.CancelOutputRead();
                            p.Close();
                        } catch (Exception) { }

                        Console.WriteLine($"Didn't read anything from the stream for {attempt} attempts. {buffer.Length/1.MB()}MB length");
                        return;
                    } else {
                        ++attempt;
                        await Task.Delay(20);
                    }
                } else {
                    attempt = 0;
                    await buffer.WriteAsync(buf, 0, read);
                }
            }
        }

        internal async Task StartPlayback() {
            Console.WriteLine("Starting playback.");
            if (State == StreamState.Playing) return;
            State = StreamState.Playing;
            if (parent.OnBuffering != null)
                parent.OnBuffering();
            BufferSong();

            // prebuffering wait stuff start
            int bufferAttempts = 0;
            int waitPerAttempt = 500;
            while (!prebufferingComplete && bufferAttempts++ < 15) {
                await Task.Delay(waitPerAttempt);
            }
            if (prebufferingComplete) {
                Console.WriteLine($"Prebuffering finished in {bufferAttempts*500}");
            }
            // prebuffering wait stuff end
            
            if (buffer.Length > 0) {
                Console.WriteLine("Prebuffering complete.");
            } else {
                Console.WriteLine("Didn't buffer jack shit.");
            }
            
            int blockSize = 1920 * NadekoBot.client.Audio().Config.Channels;
            byte[] voiceBuffer = new byte[blockSize];

            if (parent.OnStarted != null)
                parent.OnStarted();

            int attempt = 0;
            while (!IsCanceled) {
                int readCount = 0;
                lock (_bufferLock) {
                    readCount = buffer.Read(voiceBuffer, 0, voiceBuffer.Length);
                }

                if (readCount == 0) {
                    if (attempt == 2) {
                        Console.WriteLine($"Failed to read {attempt} times. Stopping playback.");
                        break;
                    } else {
                        ++attempt;
                        await Task.Delay(10);
                    }
                } else
                    attempt = 0;

                if (State == StreamState.Completed) {
                    Console.WriteLine("Canceled");
                    break;
                }

                parent.MusicControls.VoiceClient.Send(voiceBuffer, 0, voiceBuffer.Length);

                while (IsPaused) {
                    await Task.Delay(50);
                }
            }
            parent.MusicControls.VoiceClient.Wait();
            Stop();
        }

        internal void Cancel() {
            IsCanceled = true;
        }

        internal void Stop() {
            Console.WriteLine("Stopping playback");
            if (State != StreamState.Completed) {
                State = StreamState.Completed;
                parent.OnCompleted();
            }
        }
    }

    public class DualStream : MemoryStream {
        public long readPos;
        public long writePos;

        public DualStream() : base() {
            readPos = writePos = 0;
        }

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
