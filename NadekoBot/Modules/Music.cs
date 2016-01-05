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

                cgb.CreateCommand("testq")
                    .Description("Queue a song using a multi/single word name.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("Query", ParameterType.Unparsed)
                    .Do(async e => {
                        var youtube = YouTube.Default;
                        var video = youtube.GetAllVideos(e.GetArg("Query"))
                            .Where(v => v.AdaptiveKind == AdaptiveKind.Audio)
                            .OrderByDescending(v => v.AudioBitrate).FirstOrDefault();

                        if (video?.Uri != "" && video.Uri != null) {
                            SongQueue.Add(video);
                            await e.Send("**Queued** " + video.FullName);
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
                                    var m = await e.Send("Downloading song...");

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

    //new stuff 
    class AudioStreamer {
        string sourceUrl; Channel statusTextChannel;
        int totalSourceBytes;
        public bool NetworkDone { get; private set; }
        public int BytesSentToTranscoder { get; private set; }
        public Stream PCMOutput { get; private set; }
        CancellationTokenSource tokenSource1 = new CancellationTokenSource();
        CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        Task transcoderTask; Task outputTask;
        public AudioStreamer(string streamUrl, Channel statusTextChannel = null) {
            sourceUrl = streamUrl;
            this.statusTextChannel = statusTextChannel;
        }
        public void Start() {
            Task.Run(async () => {
                var bufferingStream = GetBufferingStream(sourceUrl);
                Console.WriteLine("Buffering video..."); // Wait for some data to arrive 
                while (bufferingStream.Length < 1000 || NetworkDone) 
                    await Task.Delay(500);
                Console.WriteLine("buf done");
                Stream input, pcmOutput;
                var ffmpegProcess = GetTranscoderStreams(out input, out pcmOutput);
                PCMOutput = new DualStream(); // Keep pumping network stuff into the transcoder 
                transcoderTask = Task.Run(() => TranscoderFunc(bufferingStream, input, tokenSource1.Token));
                // Keep pumping transcoder output into the PCMOutput stream 
                outputTask = Task.Run(() => OutputFunc(pcmOutput, PCMOutput, tokenSource2.Token));
                // Wait until network stuff is all done 
                while (!NetworkDone) await Task.Delay(500);
                // Then wait until we sent everything to the transcoder 
                while (BytesSentToTranscoder < totalSourceBytes) await Task.Delay(500);
                // Then wait some more until it did everything and kill it 
                await Task.Delay(5000);
                try {
                    tokenSource1.Cancel();
                    tokenSource2.Cancel();
                    Console.WriteLine("Killing transcoder...");
                    ffmpegProcess.Kill();
                } catch { }
            });
        }
        async Task TranscoderFunc(Stream sourceStream, Stream targetStream, CancellationToken cancellationToken) {
            byte[] buffer = new byte[0x4000];
            while (!NetworkDone && !cancellationToken.IsCancellationRequested) {
                // When there is new stuff available on the network we want to get it instantly 
                int available = totalSourceBytes - BytesSentToTranscoder;
                if (available > 0) {
                    int read = await sourceStream.ReadAsync(buffer, 0, Math.Min(available, buffer.Length), cancellationToken);
                    if (read > 0) { targetStream.Write(buffer, 0, read);
                        BytesSentToTranscoder += read;
                    }
                } 
                else await Task.Delay(1);
            }
            Console.WriteLine("TranscoderFunc stopped");
        }
        async Task OutputFunc(Stream sourceStream, Stream targetStream, CancellationToken cancellationToken) {
            byte[] buffer = new byte[0x4000];
            while (!cancellationToken.IsCancellationRequested) {
                // When there is new stuff available on the network we want to get it instantly 
                int read = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (read > 0) targetStream.Write(buffer, 0, read);
            }
            Console.WriteLine("OutputFunc stopped");
        }
        internal static Process GetTranscoderStreams(out Stream input, out Stream pcmOutput) {
            Process p = Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = "-i pipe:0 -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            });
            pcmOutput = p.StandardOutput.BaseStream;
            input = p.StandardInput.BaseStream;
            return p;
        }
        Stream GetBufferingStream(string streamUrl) {
            var memoryStream = new DualStream();
            Task.Run(() => {
                int byteCounter = 0;
                try {
                    var webClient = new WebClient();
                    var networkStream = webClient.OpenRead(streamUrl);
                    if (networkStream == null) return;
                    byte[] buffer = new byte[0x1000];
                    while (true) {
                        int read = networkStream.Read(buffer, 0, buffer.Length);
                        if (read <= 0) break;
                        byteCounter += read;
                        totalSourceBytes += read;
                        memoryStream.Write(buffer, 0, read);
                    }
                } catch (Exception ex) {
                    Console.WriteLine("Exception while reading network stream: " + ex);
                }
                NetworkDone = true; Console.WriteLine("net: done. ({0} read)", byteCounter);
            });
            return memoryStream;
        }
        async void Write(string message) {
            Console.WriteLine(message);
        }
        public void Cancel() {
            tokenSource1.Cancel();
            tokenSource2.Cancel();
            NetworkDone = true;
            BytesSentToTranscoder = totalSourceBytes;
        }
    }
    public class DualStream : MemoryStream {
        long readPosition;
        long writePosition;
        public override int Read(byte[] buffer, int offset, int count) {
            int read;
            lock (this) {
                Position = readPosition;
                read = base.Read(buffer, offset, count);
                readPosition = Position;
            }
            return read;
        }
        public override void Write(byte[] buffer, int offset, int count) {
            lock (this) {
                Position = writePosition;
                base.Write(buffer, offset, count);
                writePosition = Position;
            }
        }
    }
}
