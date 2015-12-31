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

namespace NadekoBot.Modules
{
    class Music : DiscordModule
    {
        private static bool exit = true;

        public static bool NextSong = false;
        public static IAudioClient Voice;
        public static Channel VoiceChannel;
        public static bool Pause = false;
        public static List<YouTubeVideo> SongQueue = new List<YouTubeVideo>();

        public static YouTubeVideo CurrentSong;

        public static bool Exit
        {
            get { return exit; }
            set { exit = value;} // if i set this to true, break the song and exit the main loop
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

        public override void Install(ModuleManager manager)
        {
            var client = NadekoBot.client;
            manager.CreateCommands("!m", cgb =>
            {
                //queue all more complex commands
                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("n")
                    .Alias("next")
                    .Description("Goes to the next song in the queue.")
                    .Do(e =>
                    {
                        if (Voice != null && Exit == false)
                        {
                            NextSong = true;
                        }
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music and unbinds the bot from the channel.")
                    .Do(e =>
                    {
                        if (Voice != null && Exit == false)
                        {
                            Exit = true;
                            SongQueue = new List<YouTubeVideo>();
                        }
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses the song")
                    .Do(async e =>
                    {
                        if (Voice != null && Exit == false && CurrentSong != null)
                        {
                            Pause = !Pause;
                            if (Pause)
                            {
                                await e.Send( "Pausing. Run the command again to resume.");
                            }
                            else
                            {
                                await e.Send( "Resuming...");
                            }
                        }
                    });

                cgb.CreateCommand("testq")
                    .Description("Queue a song using a multi/single word name.\nUsage: `!m q Dream Of Venice`")
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
                    .Description("Queue a song using a multi/single word name.\nUsage: `!m q Dream Of Venice`")
                    .Parameter("Query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var youtube = YouTube.Default;
                        var video = youtube.GetAllVideos(Searches.FindYoutubeUrlByKeywords(e.Args[0]))
                            .Where(v => v.AdaptiveKind == AdaptiveKind.Audio)
                            .OrderByDescending(v => v.AudioBitrate).FirstOrDefault();

                        if (video?.Uri != "" && video.Uri != null)
                        {
                            SongQueue.Add(video);
                            await e.Send( "**Queued** " + video.FullName);
                        }
                    });
                
                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 10 currently queued songs.")
                    .Do(async e =>
                    {
                        await e.Send( SongQueue.Count + " videos currently queued.");
                        await e.Send( string.Join("\n", SongQueue.Select(v => v.FullName).Take(10)));
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.")
                    .Do(async e =>
                    {
                        if (SongQueue.Count < 2)
                        {
                            await e.Send( "Not enough songs in order to perform the shuffle.");
                            return;
                        }

                        SongQueue.Shuffle();
                        await e.Send( "Songs shuffled!");
                    });

                cgb.CreateCommand("radio")
                    .Alias("music")
                    .Description("Binds to a voice and text channel in order to play music.")
                    .Parameter("ChannelName", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                    if (Voice != null) return;
                    VoiceChannel = e.Server.FindChannels(e.GetArg("ChannelName").Trim(), ChannelType.Voice).FirstOrDefault();
                    Voice = await client.Audio().Join(VoiceChannel);
                    Exit = false;
                    NextSong = false;
                    Pause = false;
                        try
                        {
                            while (true)
                            {
                                if (Exit) break;
                                if (SongQueue.Count == 0 || Pause) { Thread.Sleep(100); continue; }
                                if (!LoadNextSong()) break;

                                await Task.Run(async () =>
                                {
                                    if (Exit)
                                    {
                                        Voice = null;
                                        Exit = false;
                                        await e.Send( "Exiting...");
                                        return;
                                    }
                                    int blockSize = 3840;
                                    byte[] buffer = new byte[3840];
                                    
                                    var msg = await e.Send( "Playing " + Music.CurrentSong.FullName + " [00:00]");
                                    int counter = 0;
                                    int byteCount;
                                    using (var stream = GetAudioFileStream(Music.CurrentSong.Uri))
                                    {
                                        var m = await e.Send("Downloading song...");
                                        var memStream = new MemoryStream();
                                        while (true) {
                                            byte[] buff = new byte[0x4000 * 10];
                                            int read = stream.Read(buff, 0, buff.Length);
                                            if (read <= 0) break;
                                            memStream.Write(buff, 0, read);
                                        }

                                        e.Send("Song downloaded");
                                        memStream.Position = 0;
                                        while ((byteCount = memStream.Read(buffer, 0, blockSize)) > 0)
                                        {
                                            Voice.Send(buffer, byteCount);
                                            counter += blockSize;
                                            if (NextSong)
                                            {
                                                NextSong = false;
                                                break;
                                            }
                                            if (Exit)
                                            {
                                                Exit = false;
                                                return;
                                            }
                                            while (Pause) Thread.Sleep(100);
                                        }
                                    }
                                });
                            }
                        Voice.Wait();
                        }
                        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                        await Voice.Disconnect();
                        Voice = null;
                        VoiceChannel = null;
                    });
            });
        }

        private Stream GetAudioFileStream(string file)
        {
            Process p = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = "-i \"" + Uri.EscapeUriString(file) + "\" -f s16le -ar 48000 -af volume=1 -ac 2 pipe:1 ",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            return p.StandardOutput.BaseStream;
        }

        private bool LoadNextSong()
        {
            if (SongQueue.Count == 0) {
                CurrentSong = null;
                return false;
            }
            CurrentSong = SongQueue[0];
            SongQueue.RemoveAt(0);
            return true;
        }
    }
}
