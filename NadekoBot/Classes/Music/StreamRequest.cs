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

        public float Volume => MusicControls?.Volume ?? 1.0f;

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

        private async void ResolveStreamLink() {
            string uri = null;
            try {
                if (SoundCloud.Default.IsSoundCloudLink(Query)) {

                    var svideo = await SoundCloud.Default.GetVideoAsync(Query);
                    Title = svideo.FullName + " - SoundCloud";
                    uri = svideo.StreamLink;
                    Console.WriteLine(uri);
                } else {

                    if (OnResolving != null)
                        OnResolving();
                    var links = await Searches.FindYoutubeUrlByKeywords(Query);
                    var videos = await YouTube.Default.GetAllVideosAsync(links);
                    var video = videos
                                .Where(v => v.AdaptiveKind == AdaptiveKind.Audio)
                                .OrderByDescending(v => v.AudioBitrate)
                                .FirstOrDefault();

                    if (video == null) // do something with this error
                        throw new Exception("Could not load any video elements based on the query.");

                    Title = video.Title; //.Substring(0,video.Title.Length-10); // removing trailing "- You Tube"
                    uri = video.Uri;
                }
            } catch (Exception ex) {
                privateState = StreamState.Completed;
                if (OnResolvingFailed != null)
                    OnResolvingFailed(ex.Message);
                Console.WriteLine($"Failed resolving the link.{ex.Message}");
                return;
            }

            musicStreamer = new MusicStreamer(this, uri);
            if (OnQueued != null)
                OnQueued();
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
            int attemptsLeft = 4;
            //wait for up to 4 seconds to resolve a link
            try {
                while (State == StreamState.Resolving) {
                    await Task.Delay(1000);
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
            var p = Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-i {Url} -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
            int attempt = 0;
            while (true) {
                int magickBuffer = 1;
                //wait for the read pos to catch up with write pos
                while (buffer.writePos - buffer.readPos > 1.MB() && State != StreamState.Completed) {
                    prebufferingComplete = true;
                    await Task.Delay(150);
                }

                if (State == StreamState.Completed) {
                    try {
                        p.CancelOutputRead();
                        p.Close();
                    } catch (Exception) { }
                    Console.WriteLine("Buffering canceled, stream is completed.");
                    return;
                }

                if (buffer.readPos > 1.MiB() && buffer.writePos > 1.MiB()) { // if buffer is over 5 MiB, create new one
                    var skip = 1.MB(); //remove only 5 MB, just in case
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

            Task.Factory.StartNew(async () => {
                await BufferSong();
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);

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
                //adjust volume
                
                lock (_bufferLock) {
                    readCount = buffer.Read(voiceBuffer, 0, voiceBuffer.Length);
                }

                if (readCount == 0) {
                    if (attempt == 4) {
                        Console.WriteLine($"Failed to read {attempt} times. Breaking out. [{DateTime.Now.Second}]");
                        break;
                    } else {
                        ++attempt;
                        await Task.Delay(15);
                    }
                } else
                    attempt = 0;

                if (State == StreamState.Completed) {
                    Console.WriteLine("Canceled");
                    break;
                }
                voiceBuffer = adjustVolume(voiceBuffer, parent.Volume);
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
            if (State == StreamState.Completed) return;
            var oldState = State;
            State = StreamState.Completed;
            if (oldState == StreamState.Playing)
                if (parent.OnCompleted != null)
                    parent.OnCompleted();
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
