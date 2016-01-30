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

        internal static string GetMusicStats() {
            var servers = 0;
            var queued = 0;
            var stats = musicPlayers.Where(kvp => kvp.Value?.SongQueue.Count > 0 || kvp.Value?.CurrentSong != null);

            return $"Playing {stats.Count()} songs, {stats.Sum(kvp => kvp.Value?.SongQueue?.Count ?? 0)} queued.";
        }

        public Music() : base() {
            /*Timer cleaner = new Timer();
            cleaner.Elapsed += (s, e) => System.Threading.Tasks.Task.Run(() => CleanMusicPlayers());
            cleaner.Interval = 10000;
            cleaner.Start();
            
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
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        musicPlayers[e.Server].LoadNextSong();
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music and unbinds the bot from the channel and cleanes up files.")
                    .Do(e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        var player = musicPlayers[e.Server];
                        MusicControls throwAwayValue;
                        musicPlayers.TryRemove(e.Server, out throwAwayValue);
                        player.Stop();
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses the song")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        if (musicPlayers[e.Server].TogglePause())
                            await e.Send("Music player paused.");
                        else
                            await e.Send("Music player unpaused.");
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using keywords or link. **You must be in a voice channel**.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false)
                            if (!musicPlayers.TryAdd(e.Server, new MusicControls(e.User.VoiceChannel))) {
                                await e.Send("Failed to create a music player for this server");
                                return;
                            }
                        if (e.GetArg("query") == null || e.GetArg("query").Length < 5)
                            return;

                        var player = musicPlayers[e.Server];

                        if (player.SongQueue.Count > 25) {
                            await e.Send("Music player supports up to 25 songs atm. Contant the owner if you think this is not enough :warning:");
                        }

                        try {
                            var sr = await player.CreateStreamRequest(e, e.GetArg("query"), player.VoiceChannel);
                            if (sr == null)
                                throw new NullReferenceException("StreamRequest is null.");
                            Message msg = null;
                            Message qmsg = null;
                            sr.OnResolving += async () => {
                                qmsg = await e.Send($":musical_note: **Resolving**... \"{e.GetArg("query")}\"");
                            };
                            sr.OnResolvingFailed += async (err) => {
                                qmsg = await e.Send($":anger: :musical_note: **Resolving failed** for `{e.GetArg("query")}`");
                            };
                            sr.OnQueued += async () => {
                                await qmsg.Edit($":musical_note:**Queued** {sr.Title.TrimTo(55)}");
                            };
                            sr.OnCompleted += async () => {
                                await e.Send($":musical_note:**Finished playing** {sr.Title.TrimTo(55)}");
                            };
                            sr.OnStarted += async () => {
                                if (msg == null)
                                    await e.Send($":musical_note:**Playing ** {sr.Title.TrimTo(55)}");
                                else
                                    await msg.Edit($":musical_note:**Playing ** {sr.Title.TrimTo(55)}");
                                qmsg?.Delete();
                            };
                            sr.OnBuffering += async () => {
                                msg = await e.Send($":musical_note:**Buffering...** {sr.Title.TrimTo(55)}");
                            };
                        } catch (Exception ex) {
                            Console.WriteLine();
                            await e.Send($":anger: {ex.Message}");
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
                        int number = 1;
                        await e.Send(string.Join("\n", player.SongQueue.Select(v => $"**#{number++}** {v.Title.TrimTo(60)}").Take(10)));
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
