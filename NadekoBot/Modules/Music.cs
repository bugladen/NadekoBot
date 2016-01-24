// Thanks to @Bloodskilled for providing most of the music code from his BooBot
// check out his server https://discord.gg/0aMlLYi2e2V7h2Kr
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Modules;
using Discord.Commands;
using System.IO;
using Discord;
using Discord.Audio;
using YoutubeExtractor;
using System.Threading;
using System.Diagnostics;
using NadekoBot.Extensions;
using System.Net;
using System.Globalization;
using System.Collections.Concurrent;

namespace NadekoBot.Modules {
    class Music : DiscordModule {

        public class MusicControls {
            public bool NextSong = false;
            public IAudioClient Voice;
            public Channel VoiceChannel;
            public bool Pause = false;
            public List<StreamRequest> SongQueue = new List<StreamRequest>();
            public StreamRequest CurrentSong;

            public MusicControls() {
                Task.Run(async () => {
                    while (true) {
                        try {
                            if (CurrentSong == null || CurrentSong.State == StreamTaskState.Completed) {
                                LoadNextSong();
                            }
                        } catch (Exception e) {
                            Console.WriteLine("Bug in music task run. " + e);
                        }
                        await Task.Delay(200);

                        CleanMusicPlayers();
                    }
                });
            }

            private void LoadNextSong() {
                if (SongQueue.Count == 0 || !SongQueue[0].LinkResolved) {
                    if (CurrentSong != null)
                        CurrentSong.Cancel();
                    CurrentSong = null;
                    return;
                }
                CurrentSong = SongQueue[0];
                SongQueue.RemoveAt(0);
                CurrentSong.Start();
                return;
            }

        }

        public static ConcurrentDictionary<Server, MusicControls> musicPlayers = new ConcurrentDictionary<Server, MusicControls>();


        public Music() : base() {
            //commands.Add(new PlayMusic());
        }

        //m r,radio - init
        //m n,next - next in que
        //m p,pause - pauses, call again to unpause
        //m yq [key_words] - queue from yt by keywords
        //m s,stop - stop
        //m sh - shuffle songs
        //m pl - current playlist

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;



            manager.CreateCommands("!m", cgb => {
                //queue all more complex commands
                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("n")
                    .Alias("next")
                    .Description("Goes to the next song in the queue.")
                    .Do(e => {
                        if (musicPlayers.ContainsKey(e.Server) == false || (musicPlayers[e.Server]?.CurrentSong) == null) return;
                        var CurrentSong = musicPlayers[e.Server].CurrentSong;
                        CurrentSong.Cancel();
                        CurrentSong = musicPlayers[e.Server].SongQueue.Take(1).FirstOrDefault();
                        if (CurrentSong != null) {
                            CurrentSong.Start();
                        }
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music and unbinds the bot from the channel and cleanes up files.")
                    .Do(e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        var player = musicPlayers[e.Server];
                        player.SongQueue.Clear();
                        if (player.CurrentSong != null) {
                            player.CurrentSong.Cancel();
                            player.CurrentSong = null;
                        }
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses the song")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        await e.Send("Not yet implemented.");
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using keywords or link. **You must be in a voice channel**.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("Query", ParameterType.Unparsed)
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false)
                            musicPlayers.TryAdd(e.Server, new MusicControls());
                        var player = musicPlayers[e.Server];
                        player.SongQueue.Add(new StreamRequest(NadekoBot.client, e, e.GetArg("Query")));
                        await e.Send(":warning: Music is unstable atm, working on a fix. :warning:");
                    });

                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 10 currently queued songs.")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) await e.Send(":musical_note: No active music player.");
                        var player = musicPlayers[e.Server];

                        await e.Send(":musical_note: " + player.SongQueue.Count + " videos currently queued.");
                        await e.Send(string.Join("\n", player.SongQueue.Select(v => v.Title).Take(10)));
                    });

                cgb.CreateCommand("np")
                 .Alias("playing")
                 .Description("Shows the song currently playing.")
                 .Do(async e => {
                     if (musicPlayers.ContainsKey(e.Server) == false) return;
                     var player = musicPlayers[e.Server];
                     await e.Send($"Now Playing **{player.CurrentSong.Title}**");
                 });

                cgb.CreateCommand("clrbfr")
                    .Alias("clearbuffers")
                    .Description("Clears the music buffer across all servers. **Owner only.**")
                    .Do(e => {
                        if (NadekoBot.OwnerID != e.User.Id) return;
                        Directory.Delete("StreamBuffers", true);
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        var player = musicPlayers[e.Server];
                        if (player.SongQueue.Count < 2) {
                            await e.Send("Not enough songs in order to perform the shuffle.");
                            return;
                        }

                        player.SongQueue.Shuffle();
                        await e.Send(":musical_note: Songs shuffled!");
                    });
            });
        }
        internal static void CleanMusicPlayers() {
            foreach (var mp in musicPlayers
                                .Where(kvp => kvp.Value.CurrentSong == null
                                && kvp.Value.SongQueue.Count == 0)) {
                var val = mp.Value;
                musicPlayers.TryRemove(mp.Key, out val);
            }
        }

        internal static string GetMusicStats() {
            var servers = 0;
            var queued = 0;
            musicPlayers.ForEach(kvp => {
                var mp = kvp.Value;
                if(mp.SongQueue.Count > 0 || mp.CurrentSong != null)
                    queued += mp.SongQueue.Count + 1;
                servers++;
            });

            return $"Playing {queued} songs across {servers} servers.";

        }
    }

    enum StreamTaskState {
        Queued,
        Playing,
        Completed,
        NotReady
    }

    class StreamRequest {
        static readonly string[] validFormats = { ".ogg", ".wav", ".mp3", ".webm", ".aac", ".mp4", ".flac" };

        readonly DiscordClient client;
        public readonly Server Server;
        public readonly Channel Channel;
        public Channel VoiceChannel;
        public readonly User User;
        public readonly string RequestText;

        const string DefaultTitle = "<??>";
        public string Title = DefaultTitle;
        public TimeSpan Length = TimeSpan.FromSeconds(0);
        public string FileName;

        public bool LinkResolved = false;
        public string StreamUrl;
        public bool NetworkDone;
        public long TotalSourceBytes;

        Stream bufferingStream;
        StreamTask streamTask;

        public StreamTaskState State => streamTask?.State ?? StreamTaskState.Queued;


        public StreamRequest(DiscordClient client, CommandEventArgs e, string text) {
            this.client = client;
            Server = e.Server;
            Channel = e.Channel;
            User = e.User;
            RequestText = text.Trim();

            FileName = "unresolved_" + Uri.EscapeUriString(RequestText);

            Task.Run(() => {
                try {
                    ResolveLink();
                } catch (Exception ex) {
                    Console.WriteLine("Exception in ResolveLink: " + ex);
                }
            });
        }

        void ResolveLink() {
            var query = RequestText;
            try {
                var video = DownloadUrlResolver.GetDownloadUrls(Searches.FindYoutubeUrlByKeywords(query))
                        .Where(v => v.AdaptiveType == AdaptiveType.Audio)
                        .OrderByDescending(v => v.AudioBitrate).FirstOrDefault();

                if (video == null)
                    throw new Exception("Could not load any video elements");

                StreamUrl = video.DownloadUrl;
                Title = video.Title;
                string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                foreach (char c in invalidChars) {
                    FileName = FileName.Replace(c.ToString(), "_");
                }

                StartBuffering();
                LinkResolved = true;
                Channel.Send($"{User.Mention}, Queued **{video.Title}**");
            } catch (Exception e) {
                // Send a message to the guy that queued that
                Channel.SendMessage("This video is unavailable in the country the Bot is running in, or you enter an invalid name or url.");

                Console.WriteLine("Cannot parse youtube url: " + query);
                Cancel();
            }
        }

        internal void StartBuffering() {
            var folder = "StreamBuffers";
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, FileName);

            FileStream fileStream;
            try {
                if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 1024 * 2) {
                    NetworkDone = true;
                    TotalSourceBytes = new FileInfo(fullPath).Length;
                    bufferingStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    if (Length.TotalSeconds < double.Epsilon)
                        Length = GetFileLength(fullPath);
                    return;
                }

                // Open a new file to stream into
                fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                bufferingStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            } catch (Exception ex) {
                Console.WriteLine("Exception while creating or opening stream buffers: " + ex);
                return;
            }
            
            Task.Run(() => {
                int byteCounter = 0;

                try {
                    var webClient = new WebClient();
                    var networkStream = webClient.OpenRead(StreamUrl);

                    if (networkStream == null)
                        return;

                    byte[] buffer = new byte[0x1000];
                    while (true) {
                        int read = networkStream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            break;
                        byteCounter += read;
                        TotalSourceBytes += read;
                        fileStream.Write(buffer, 0, read);

                        if (TotalSourceBytes > 1024 * 2 && Length.TotalSeconds < 0.1) {
                            Length = GetFileLength(fullPath);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine("Exception while buffering (network->file): " + ex);
                }

                fileStream.Close();
                NetworkDone = true;
                Console.WriteLine("net: done. ({0} read)", byteCounter);
            });
        }

        internal void Start() {
            if (State != StreamTaskState.Queued)
                return;

            Stopwatch resolveTimer = Stopwatch.StartNew();

            while (resolveTimer.ElapsedMilliseconds < 8000) {
                if (bufferingStream != null)
                    break;
                Thread.Sleep(50);
            }

            if (bufferingStream == null) {
                Console.WriteLine("Buffering stream was not set! Can't play track!");
                streamTask = new StreamTask(client, this, null);
                streamTask.CancelStreaming();
                return;
            }

            streamTask = new StreamTask(client, this, bufferingStream);

            VoiceChannel = GetVoiceChannelForUser(User);
            if (VoiceChannel == null) {
                Channel.SendMessage($":warning: {User.Mention} `I can't find you in any voice channel. Join one, then try again...`");
                streamTask.CancelStreaming(); // just to set the state to done
                return;
            }

            // Go!
            streamTask.StartStreaming();
        }

        internal void Cancel() {
            if (State == StreamTaskState.Completed)
                return;

            if (streamTask == null)
                streamTask = new StreamTask(client, this, bufferingStream);
            streamTask.CancelStreaming();
        }


        Channel GetVoiceChannelForUser(User user) {
            return client.Servers.SelectMany(s => s.VoiceChannels).FirstOrDefault(c => c.Users.Any(u => u.Id == user.Id));
        }

        public static TimeSpan GetFileLength(string fileName) {
            try {
                var startInfo = new ProcessStartInfo("ffprobe", $"-i \"{fileName}\" -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1");
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                using (var process = Process.Start(startInfo)) {
                    var lengthLine = process.StandardOutput.ReadLine();
                    double result;
                    if (double.TryParse(lengthLine, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result)) {
                        int integerPart = (int)Math.Round(result);
                        return TimeSpan.FromSeconds(integerPart);
                    } else
                        return TimeSpan.Zero;
                }
            } catch (Exception) {
                Console.WriteLine("Exception while determining file play-time");
                return TimeSpan.Zero;
            }
        }
    }
    class TranscodingTask {
        readonly StreamRequest streamRequest;
        readonly Stream bufferingStream;

        public long BytesSentToTranscoder { get; private set; }
        public DualStream PCMOutput { get; private set; }
        public long ReadyBytesLeft => PCMOutput?.writePos - PCMOutput?.readPos ?? 0;

        readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        Task transcoderTask;
        Task outputTask;

        public TranscodingTask(StreamRequest streamRequest, Stream bufferingStream) {
            this.streamRequest = streamRequest;
            this.bufferingStream = bufferingStream;
        }

        public void Start() {
            Task.Run(async () => {
                // Wait for some data to arrive
                while (true) {
                    if (streamRequest.NetworkDone)
                        break;
                    if (bufferingStream.Length > 1024 * 3)
                        break;
                    await Task.Delay(100);
                }

                Stream input, pcmOutput;
                var ffmpegProcess = GetTranscoderStreams(out input, out pcmOutput);

                PCMOutput = new DualStream();

                // Keep pumping network stuff into the transcoder
                transcoderTask = Task.Run(() => TranscoderFunc(bufferingStream, input, tokenSource.Token), tokenSource.Token);

                // Keep pumping transcoder output into the PCMOutput stream
                outputTask = Task.Run(() => OutputFunc(pcmOutput, PCMOutput, tokenSource.Token), tokenSource.Token);

                // Wait until network stuff is all done
                while (!streamRequest.NetworkDone)
                    await Task.Delay(200);

                // Then wait until we sent everything to the transcoder
                while (BytesSentToTranscoder < streamRequest.TotalSourceBytes)
                    await Task.Delay(200);

                // Then wait some more until it did everything and kill it
                await Task.Delay(5000);

                try {
                    tokenSource.Cancel();
                    bufferingStream.Close();

                    Console.WriteLine("Killing transcoder...");
                    ffmpegProcess.Kill();
                } catch {
                }
            });
        }

        async Task TranscoderFunc(Stream sourceStream, Stream transcoderInput, CancellationToken cancellationToken) {
            try {
                byte[] buffer = new byte[1024];
                while (true) {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    if (BytesSentToTranscoder >= streamRequest.TotalSourceBytes)
                        break;

                    // When there is new stuff available on the network we want to get it instantly
                    long available = streamRequest.TotalSourceBytes - BytesSentToTranscoder;

                    double availableRingSpace = PCMOutput.Length / (double)PCMOutput.Capacity;

                    // How much data is in the final output buffer?
                    // We dont want to transcode too much in advance
                    if (available > 0) {
                        int read = await sourceStream.ReadAsync(buffer, 0, (int)Math.Min(available, buffer.LongLength), cancellationToken);
                        if (read > 0) {
                            // Write to transcoder
                            transcoderInput.Write(buffer, 0, read);
                            BytesSentToTranscoder += read;
                        }
                    } else {
                        // We have enough data transcoded already. Stall a bit so we dont do too much!
                        await Task.Delay(100);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("TranscoderFunc stopped");
        }

        static async Task OutputFunc(Stream sourceStream, DualStream targetBuffer, CancellationToken cancellationToken) {
            try {
                byte[] buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested) {
                    // When there is new stuff available on the network we want to get it instantly
                    int read = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (read > 0) {
                        targetBuffer.Write(buffer, 0, read);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("OutputFunc stopped");
        }

        internal static Process GetTranscoderStreams(out Stream input, out Stream pcmOutput) {
            Process p = null;
            Exception ex = null;

            try {
                p = Process.Start(new ProcessStartInfo {
                    FileName = "ffmpeg",
                    Arguments = "-i pipe:0 -f s16le -ar 48000 -ac 2 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardError = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                });
            } catch (Exception exInner) {
                ex = exInner;
            }

            if (p == null || ex != null) {
                input = null;
                pcmOutput = null;
                Console.WriteLine("Could not start ffmpeg: " + (ex?.Message ?? "<no exception>"));
                return null;
            }

            pcmOutput = p.StandardOutput.BaseStream;
            input = p.StandardInput.BaseStream;
            return p;
        }

        public void Cancel() {
            tokenSource.Cancel();
            BytesSentToTranscoder = streamRequest.TotalSourceBytes;
        }
    }
    public class DualStream : MemoryStream {
        public long readPos;
        public long writePos;
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
    class StreamTask {
        readonly DiscordClient client;
        readonly StreamRequest streamRequest;
        readonly Stream bufferingStream;

        CancellationTokenSource tokenSource;
        Task audioTask;

        public StreamTaskState State { get; private set; }

        public StreamTask(DiscordClient client, StreamRequest streamRequest, Stream bufferingStream) {
            this.streamRequest = streamRequest;
            this.bufferingStream = bufferingStream;
            this.client = client;

            State = StreamTaskState.Queued;
        }

        public void StartStreaming() {
            if (State != StreamTaskState.Queued)
                return;

            State = StreamTaskState.Playing;
            tokenSource = new CancellationTokenSource();
            audioTask = Task.Run(StreamFunc, tokenSource.Token);
        }

        public void CancelStreaming() {
            if (State != StreamTaskState.Queued && State != StreamTaskState.Playing)
                return;

            tokenSource?.Cancel(false);
            audioTask?.Wait();
            State = StreamTaskState.Completed;
        }

        async Task StreamFunc() {
            CancellationToken cancellationToken = tokenSource.Token;
            IAudioClient voiceClient = null;
            TranscodingTask streamer = null;
            try {
                uint byteCounter = 0;

                // Download and read audio from the url
                streamer = new TranscodingTask(streamRequest, bufferingStream);
                streamer.Start();

                // Wait until we have at least a few kb transcoded or network stream done
                while (true) {
                    if (streamRequest.NetworkDone) {
                        await Task.Delay(600);
                        break;
                    }
                    if (streamer.ReadyBytesLeft > 5 * 1024)
                        break;
                    await Task.Delay(200);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                // Start streaming to voice
                 await streamRequest.Channel.SendMessage($"Playing **{streamRequest.Title}** [{streamRequest.Length}]");

                var audioService = client.Audio();
                voiceClient = await audioService.Join(streamRequest.VoiceChannel);

                int blockSize = 1920 * audioService.Config.Channels;
                byte[] voiceBuffer = new byte[blockSize];
                var ringBuffer = streamer.PCMOutput;

                Stopwatch timeout = Stopwatch.StartNew();
                while (true) {
                    var readCount = ringBuffer.Read(voiceBuffer, 0, voiceBuffer.Length);

                    if (readCount == 0) {
                        if (timeout.ElapsedMilliseconds > 1500) {
                            Console.WriteLine("Audio stream timed out. Disconnecting.");
                            break;
                        }

                        await Task.Delay(200);
                        continue;
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    timeout.Restart();

                    byteCounter += (uint)voiceBuffer.Length;
                    voiceClient.Send(voiceBuffer, 0, voiceBuffer.Length);
                }

                streamer.Cancel();

                voiceClient.Wait();
            } catch (Exception ex) {
                await streamRequest.Channel.SendMessage($":musical_note: {streamRequest.User.Mention} Something went wrong, please report this. :angry: :anger:");
                Console.WriteLine("Exception while playing music: " + ex);
            } finally {
                if (voiceClient != null) {
					await streamRequest.Channel.SendMessage($"Finished playing **{streamRequest.Title}**");
                    State = StreamTaskState.Completed;
                    streamer?.Cancel();
                    await voiceClient.Disconnect();
                    await Task.Delay(500);
                }
            }
        }
    }

}
