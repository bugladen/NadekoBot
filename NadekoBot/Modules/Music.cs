// Thanks to @Bloodskilled for providing most of the music code from his BooBot
// check out his server https://discord.gg/0aMlLYi2e2V7h2Kr
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Modules;
using Discord.Commands;
using System.IO;
using Discord;
using Discord.Audio;
using System.Collections.Concurrent;
using VideoLibrary;
using System.Threading;
using System.Diagnostics;
using Discord.Legacy;
using System.Net;
using System.Globalization;

namespace NadekoBot.Modules {
    class Music : DiscordModule {
        private static bool exit = true;

        public static bool NextSong = false;
        public static IAudioClient Voice;
        public static Channel VoiceChannel;
        public static bool Pause = false;
        public static List<YouTubeVideo> SongQueue = new List<YouTubeVideo>();

        public static YouTubeVideo CurrentSong;

        public static bool Exit {
            get { return exit; }
            set { exit = value; } // if i set this to true, break the song and exit the main loop
        }

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
                        if (Voice != null && Exit == false) {
                            NextSong = true;
                        }
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music and unbinds the bot from the channel.")
                    .Do(e => {
                        if (Voice != null && Exit == false) {
                            Exit = true;
                            SongQueue = new List<YouTubeVideo>();
                        }
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses the song")
                    .Do(async e => {
                        if (Voice != null && Exit == false && CurrentSong != null) {
                            Pause = !Pause;
                            if (Pause) {
                                await e.Send("Pausing. Run the command again to resume.");
                            } else {
                                await e.Send("Resuming...");
                            }
                        }
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using a multi/single word name.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("Query", ParameterType.Unparsed)
                    .Do(async e => {
                        var youtube = YouTube.Default;
                        var video = youtube.GetAllVideos(Searches.FindYoutubeUrlByKeywords(e.Args[0]))
                            .Where(v => v.AdaptiveKind == AdaptiveKind.Audio)
                            .OrderByDescending(v => v.AudioBitrate).FirstOrDefault();
                        
                        if (video?.Uri != "" && video.Uri != null) {
                            SongQueue.Add(video);
                            await e.Send("**Queued** " + video.FullName);
                        } else {
                            await e.Send("Failed to load that song.");
                        }
                    });

                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 10 currently queued songs.")
                    .Do(async e => {
                        await e.Send(SongQueue.Count + " videos currently queued.");
                        await e.Send(string.Join("\n", SongQueue.Select(v => v.FullName).Take(10)));
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.")
                    .Do(async e => {
                        if (SongQueue.Count < 2) {
                            await e.Send("Not enough songs in order to perform the shuffle.");
                            return;
                        }

                        SongQueue.Shuffle();
                        await e.Send("Songs shuffled!");
                    });

                cgb.CreateCommand("radio")
                    .Alias("music")
                    .Description("Binds to a voice and text channel in order to play music.")
                    .Parameter("ChannelName", ParameterType.Unparsed)
                    .Do(async e => {
                        if (Voice != null) return;
                        VoiceChannel = e.Server.FindChannels(e.GetArg("ChannelName").Trim(), ChannelType.Voice).FirstOrDefault();
                        Voice = await client.Audio().Join(VoiceChannel);
                        Exit = false;
                        NextSong = false;
                        Pause = false;
                        try {
                            while (true) {
                                if (Exit) break;
                                if (SongQueue.Count == 0 || Pause) { Thread.Sleep(100); continue; }
                                if (!LoadNextSong()) break;

                                await Task.Run(async () => {
                                    if (Exit) {
                                        Voice = null;
                                        Exit = false;
                                        await e.Send("Exiting...");
                                        return;
                                    }

                                    var streamer = new AudioStreamer(Music.CurrentSong.Uri);
                                    streamer.Start();
                                    while (streamer.BytesSentToTranscoder < 100 * 0x1000 || streamer.NetworkDone)
                                        await Task.Delay(500);

                                    int blockSize = 1920 * client.Audio().Config.Channels;
                                    byte[] buffer = new byte[blockSize];

                                    var msg = await e.Send("Playing " + Music.CurrentSong.FullName + " [00:00]");
                                    int counter = 0;
                                    int byteCount;

                                    while ((byteCount = streamer.PCMOutput.Read(buffer, 0, blockSize)) > 0) {
                                        Voice.Send(buffer, byteCount);
                                        counter += blockSize;
                                        if (NextSong) {
                                            NextSong = false;
                                            break;
                                        }
                                        if (Exit) {
                                            Exit = false;
                                            return;
                                        }
                                        while (Pause) Thread.Sleep(100);
                                    }
                                });
                            }
                            Voice.Wait();
                        } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                        await Voice.Disconnect();
                        Voice = null;
                        VoiceChannel = null;
                    });
            });
        }

        private Stream GetAudioFileStream(string file) {
            Process p = Process.Start(new ProcessStartInfo() {
                FileName = "ffmpeg",
                Arguments = "-i \"" + Uri.EscapeUriString(file) + "\" -f s16le -ar 48000 -af volume=1 -ac 2 pipe:1 ",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            return p.StandardOutput.BaseStream;
        }

        private bool LoadNextSong() {
            if (SongQueue.Count == 0) {
                CurrentSong = null;
                return false;
            }
            CurrentSong = SongQueue[0];
            SongQueue.RemoveAt(0);
            return true;
        }
    }

    enum StreamTaskState {
        Queued,
        Playing,
        Completed
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

        bool linkResolved;
        public string StreamUrl;
        public bool NetworkDone;
        public long TotalSourceBytes;

        Stream bufferingStream;
        StreamTask streamTask;

        public StreamTaskState State => streamTask?.State ?? StreamTaskState.Queued;


        public StreamRequest(DiscordClient client, MessageEventArgs e, string text) {
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
            var url = RequestText;

            if (url.IndexOf("soundcloud", StringComparison.OrdinalIgnoreCase) != -1) {
                var track = Services.SoundcloudService.GetTrackStreamUrl(url, out Title, out StreamUrl);
                Length = TimeSpan.FromMilliseconds(track.Duration);
                Title = track.Title;
                FileName = Uri.EscapeUriString(Title) + ".mp3";

                StartBuffering();
                linkResolved = true;
            } else if (url.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) != -1 || url.IndexOf("youtu.be", StringComparison.OrdinalIgnoreCase) != -1) {
                try {
                    var infos = DownloadUrlResolver
                        .GetDownloadUrls(url.Trim())
                        .Where(i => i.AudioType != AudioType.Unknown)
                        .ToArray();

                    if (infos.Length == 0)
                        throw new Exception("Could not load any video elements");

                    var info = infos
                        .GroupBy(x => x.AudioBitrate)   // Create groups for audio bitrates
                        .OrderByDescending(x => x.Key)  // Group with max bitrate first
                        .Take(1)                        // Only take one group
                        .SelectMany(x => x)             // Unpack group container again
                        .OrderBy(x => x.Resolution)     // take vid with smallest resolution
                        .First();                       // First one

                    StreamUrl = info.DownloadUrl;
                    Title = info.Title;
                    FileName = Uri.EscapeUriString(Title) + ".mp4";

                    StartBuffering();
                    linkResolved = true;
                } catch (Exception) {
                    // Send a message to the guy that queued that
                    Channel.SendMessage(":warning: " + User.Mention + " Cannot load youtube url: `This video is not available in your country` or the url is corrupted somehow...");
                    Console.WriteLine("Cannot parse youtube url: " + url);
                    Cancel();
                }
            } else {
                // Is it a direct link oO ??
                var format = validFormats.FirstOrDefault(f => url.EndsWith(f));
                if (format == null) {
                    Console.WriteLine("Direct link: \"" + url + "\" does not end with a valid extension");
                    return;
                }

                StreamUrl = url;
                Title = url;

                StartBuffering();
                linkResolved = true;
            }
        }

        internal void StartBuffering() {
            var folder = "StreamBuffers";
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, FileName);

            FileStream fileStream;
            FileStream readStream;
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
                AutoResetDelay fileLengthCheckDelay = new AutoResetDelay(500);
                int byteCounter = 0;
                bool fileLengthDetermined = false;

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

                        if (TotalSourceBytes > 1024 * 2 && Length.TotalSeconds < 0.1 && fileLengthCheckDelay.IsReady) {
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

            if (!linkResolved || bufferingStream == null)
                Channel.SendMessage($":musical_note: Resolving link...\r\n:warning: `Keep in mind that other people can 'steal' the bot by just starting a stream command in their own server...`\r\n");

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

        public string GetFormattedTitle() {
            if (Length.TotalSeconds < double.Epsilon)
                Length = GetFileLength(FileName);

            if (Title != DefaultTitle)
                return $"**{Title.Replace('*', '°')}** *({Length.ToString()})*";

            // put into <> when it contains a domain
            if (StreamUrl == null)
                return "<" + RequestText + ">";
            if (StreamUrl.Contains("http:") || StreamUrl.Contains("https:"))
                return "<" + StreamUrl.Trim() + ">";
            return StreamUrl;
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
                    if (available > 0 && availableRingSpace < 1) {
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
}
