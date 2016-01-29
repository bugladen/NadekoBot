using System;
using System.Linq;
using Discord.Modules;
using Discord.Commands;
using Discord;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using NadekoBot.Classes.Music;
using Timer = System.Timers.Timer;

namespace NadekoBot.Modules {
    class Music : DiscordModule {

        public static ConcurrentDictionary<Server, MusicControls> musicPlayers = new ConcurrentDictionary<Server, MusicControls>();

        internal static void CleanMusicPlayers() {
            foreach (var mp in musicPlayers
                                .Where(kvp => kvp.Value.CurrentSong == null
                                && kvp.Value.SongQueue.Count == 0)) {
                var val = mp.Value;
                (musicPlayers as System.Collections.IDictionary).Remove(mp.Key);
            }
        }

        internal static string GetMusicStats() {
            var servers = 0;
            var queued = 0;
            musicPlayers.ForEach(kvp => {
                var mp = kvp.Value;
                if (mp.SongQueue.Count > 0 || mp.CurrentSong != null)
                    queued += mp.SongQueue.Count + 1;
                servers++;
            });

            return $"Playing {queued} songs across {servers} servers.";
        }

        public Music() : base() {
            Timer cleaner = new Timer();
            cleaner.Elapsed += (s, e) => System.Threading.Tasks.Task.Run(() => CleanMusicPlayers());
            cleaner.Interval = 10000;
            cleaner.Start();
            /*
            Timer statPrinter = new Timer();
            NadekoBot.client.Connected += (s, e) => {
                if (statPrinter.Enabled) return;
                statPrinter.Elapsed += (se, ev) => { Console.WriteLine($"<<--Music-->> {musicPlayers.Count} songs playing."); musicPlayers.ForEach(kvp => Console.WriteLine(kvp.Value?.CurrentSong?.PrintStats())); Console.WriteLine("<<--Music END-->>"); };
                statPrinter.Interval = 5000;
                statPrinter.Start();
            };
            */
        }

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
                        musicPlayers[e.Server].CurrentSong.Cancel();
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music and unbinds the bot from the channel and cleanes up files.")
                    .Do(e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        var player = musicPlayers[e.Server];
                        player.RemoveAllSongs();
                        if (player.CurrentSong != null) {
                            player.CurrentSong.Cancel();
                        }
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses the song")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        await e.Send("This feature is coming tomorrow.");
                        /*
                        if (musicPlayers[e.Server].Pause())
                            if (musicPlayers[e.Server].IsPaused)
                                await e.Send("Music player Paused");
                            else
                                await e.Send("Music player unpaused.");
                                */
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using keywords or link. **You must be in a voice channel**.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false)
                            if (musicPlayers.Count > 25) {
                                await e.Send($"{e.User.Mention}, playlist supports up to 25 songs. If you think this is not enough, contact the owner.:warning:");
                                return;
                                } 
                            else
                                (musicPlayers as System.Collections.IDictionary).Add(e.Server, new MusicControls(e.User.VoiceChannel));

                        var player = musicPlayers[e.Server];
                        try {
                            var sr = player.CreateStreamRequest(e, e.GetArg("query"), player.VoiceChannel);
                            Message msg = null;
                            sr.OnQueued += async() => {
                                msg = await e.Send($":musical_note:**Queued** {sr.Title}");
                            };
                            sr.OnCompleted += async () => {
                                await e.Send($":musical_note:**Finished playing** {sr.Title}");
                            };
                            sr.OnStarted += async () => {
                                if (msg == null)
                                    await e.Send($":musical_note:**Starting playback of** {sr.Title}");
                                else
                                    await msg.Edit($":musical_note:**Starting playback of** {sr.Title}");
                            };
                            sr.OnBuffering += async () => {
                                if (msg != null)
                                    msg = await e.Send($":musical_note:**Buffering the song**...{sr.Title}");
                            };
                            lock (player.SongQueue) {
                                player.SongQueue.Add(sr);
                            }
                        } catch (Exception ex) {
                            Console.WriteLine();
                            await e.Send($"Error. :anger:\n{ex.Message}");
                            return;
                        }
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
    }    
}
