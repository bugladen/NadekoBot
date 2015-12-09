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

namespace NadekoBot.Modules
{
    class Music : DiscordModule
    {
        private static bool exit = true;

        public static bool NextSong = false;
        public static Discord.Audio.DiscordAudioClient Voice;
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
                                await client.SendMessage(e.Channel, "Pausing. Run the command again to resume.");
                            }
                            else
                            {
                                await client.SendMessage(e.Channel, "Resuming...");
                            }
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
                            await client.SendMessage(e.Channel, "**Queued** " + video.FullName);
                        }
                    });
                
                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 10 currently queued songs.")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, SongQueue.Count + " videos currently queued.");
                        await client.SendMessage(e.Channel, string.Join("\n", SongQueue.Select(v => v.FullName).Take(10)));
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.")
                    .Do(async e =>
                    {
                        if (SongQueue.Count < 2)
                        {
                            await client.SendMessage(e.Channel, "Not enough songs in order to perform the shuffle.");
                            return;
                        }

                        SongQueue.Shuffle();
                        await client.SendMessage(e.Channel, "Songs shuffled!");
                    });

                cgb.CreateCommand("radio")
                    .Alias("music")
                    .Description("Binds to a voice and text channel in order to play music.")
                    .Parameter("ChannelName", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                    if (Voice != null) return;
                    VoiceChannel = client.FindChannels(e.Server, e.GetArg("ChannelName").Trim(), ChannelType.Voice).FirstOrDefault();
                    //Voice = await client.JoinVoiceServer(VoiceChannel);
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
                                        await client.SendMessage(e.Channel, "Exiting...");
                                        return;
                                    }
                                    int blockSize = 1920;
                                    byte[] buffer = new byte[1920];
                                    //float multiplier = 1.0f / 48000 / 2;
                                    
                                    var msg = await client.SendMessage(e.Channel, "Playing " + Music.CurrentSong.FullName + " [00:00]");
                                    int counter = 0;
                                    int byteCount;
                                    using (var stream = GetAudioFileStream(Music.CurrentSong.Uri))
                                    {
                                        while ((byteCount = stream.Read(buffer, 0, blockSize)) > 0)
                                        {
                                          //  Voice.SendVoicePCM(buffer, byteCount);
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
                         //   await Voice.WaitVoice();
                        }
                        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                       // await client.LeaveVoiceServer(VoiceChannel.Server);
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
                Arguments = "-i \"" + Uri.EscapeUriString(file) + "\" -f s16le -ar 48000 -af volume=1 -ac 1 pipe:1 ",
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
