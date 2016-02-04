using System;
using System.Linq;
using Discord.Modules;
using Discord.Commands;
using Discord;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using NadekoBot.Classes.Music;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;

namespace NadekoBot.Modules {
    class Music : DiscordModule {

        public static ConcurrentDictionary<Server, MusicControls> musicPlayers = new ConcurrentDictionary<Server, MusicControls>();

        internal static string GetMusicStats() {
            var stats = musicPlayers.Where(kvp => kvp.Value?.SongQueue.Count > 0 || kvp.Value?.CurrentSong != null);
            int cnt;
            return $"Playing {cnt = stats.Count()} songs".SnPl(cnt) + $", {stats.Sum(kvp => kvp.Value?.SongQueue?.Count ?? 0)} queued.";
        }

        public Music() : base() {
        }

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;

            manager.CreateCommands("!m", cgb => {
                //queue all more complex commands
                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("n")
                    .Alias("next")
                    .Description("Goes to the next song in the queue.")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        await musicPlayers[e.Server].LoadNextSong();
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music and unbinds the bot from the channel and cleanes up files.")
                    .Do(e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        musicPlayers[e.Server].Stop();
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
                    .Do(async e => await QueueSong(e,e.GetArg("query")));

                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 10 currently queued songs.")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) await e.Send(":musical_note: No active music player.");
                        var player = musicPlayers[e.Server];
                        string toSend = ":musical_note: " + player.SongQueue.Count + " videos currently queued. ";
                        if (player.SongQueue.Count >= 25)
                            toSend += "**Song queue is full!**\n";
                        await e.Send(toSend);
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

                cgb.CreateCommand("vol")
                  .Description("Sets the music volume 0-150%")
                  .Parameter("val", ParameterType.Required)
                  .Do(async e => {
                      if (musicPlayers.ContainsKey(e.Server) == false) return;
                      var player = musicPlayers[e.Server];
                      var arg = e.GetArg("val");
                      int volume;
                      if (!int.TryParse(arg, out volume)) {
                          await e.Send("Volume number invalid.");
                          return;
                      }
                      player.SetVolume(volume);
                  });

                cgb.CreateCommand("min").Alias("mute")
                  .Description("Sets the music volume to 0%")
                  .Do(e => {
                      if (musicPlayers.ContainsKey(e.Server) == false) return;
                      var player = musicPlayers[e.Server];
                      player.SetVolume(0);
                  });

                cgb.CreateCommand("max")
                  .Description("Sets the music volume to 100% (real max is actually 150%).")
                  .Do(e => {
                      if (musicPlayers.ContainsKey(e.Server) == false) return;
                      var player = musicPlayers[e.Server];
                      player.SetVolume(100);
                  });

                cgb.CreateCommand("half")
                  .Description("Sets the music volume to 50%.")
                  .Do(e => {
                      if (musicPlayers.ContainsKey(e.Server) == false) return;
                      var player = musicPlayers[e.Server];
                      player.SetVolume(50);
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

                bool setgameEnabled = false;
                Timer setgameTimer = new Timer();
                setgameTimer.Interval = 20000;
                setgameTimer.Elapsed += (s, e) => {
                    int num = musicPlayers.Where(kvp => kvp.Value.CurrentSong != null).Count();
                    NadekoBot.client.SetGame($"{num} songs".SnPl(num) + $", {musicPlayers.Sum(kvp => kvp.Value.SongQueue.Count())} queued");
                };
                cgb.CreateCommand("setgame")
                    .Description("Sets the game of the bot to the number of songs playing.**Owner only**")
                    .Do(async e => {
                        if (NadekoBot.OwnerID != e.User.Id)
                            return;
                        setgameEnabled = !setgameEnabled;
                        if (setgameEnabled)
                            setgameTimer.Start();
                        else
                            setgameTimer.Stop();

                        await e.Send("Music status " + (setgameEnabled ? "enabled" : "disabled"));
                    });

                cgb.CreateCommand("pl")
                    .Description("Queues up to 25 songs from a youtube playlist")
                    .Parameter("playlist", ParameterType.Unparsed)
                    .Do(async e => {
                        var ids = await Searches.GetVideoIDs(await Searches.GetPlaylistIdByKeyword(e.GetArg("playlist")));
                        //todo TEMPORARY SOLUTION, USE RESOLVE QUEUE IN THE FUTURE
                        await e.Send($"Attempting to queue {ids.Count} songs".SnPl(ids.Count));
                        foreach (var id in ids) {
                            Task.Run(async () => await QueueSong(e, id, true)).ConfigureAwait(false);
                            await Task.Delay(150);
                        }
                    });

                cgb.CreateCommand("debug")
                    .Description("Writes some music data to console. **BOT OWNER ONLY**")
                    .Do(e => {
                        var output = "SERVER_NAME---SERVER_ID-----USERCOUNT----QUEUED\n" +
                            string.Join("\n", musicPlayers.Select(kvp => kvp.Key.Name + "--" + kvp.Key.Id + " --" + kvp.Key.Users.Count() + "--" + kvp.Value.SongQueue.Count));
                        Console.WriteLine(output);
                    });
            });
        }

        private async Task QueueSong(CommandEventArgs e, string query, bool silent = false) {
            if (e.User.VoiceChannel?.Server != e.Server) {
                await e.Send(":anger: You need to be in the voice channel on this server.");
                return;
            }
            if (musicPlayers.ContainsKey(e.Server) == false)
                if (!musicPlayers.TryAdd(e.Server, new MusicControls(e.User.VoiceChannel, e))) {
                    await e.Send("Failed to create a music player for this server");
                    return;
                }
            if (query == null || query.Length < 4)
                return;

            var player = musicPlayers[e.Server];

            if (player.SongQueue.Count >= 25) return;

            try {
                var sr = await Task.Run(() => new StreamRequest(e, query, player));

                if (sr == null)
                    throw new NullReferenceException("StreamRequest is null.");

                Message qmsg = null;
                Message msg = null;
                if (!silent) {
                    qmsg = await e.Channel.SendMessage(":musical_note: **Searching...**");
                    sr.OnResolving += async () => {
                        await qmsg.Edit($":musical_note: **Resolving**... \"{query}\"");
                    };
                    sr.OnResolvingFailed += async (err) => {
                        await qmsg.Edit($":anger: :musical_note: **Resolving failed** for `{query}`");
                    };
                    sr.OnQueued += async () => {
                        await qmsg.Edit($":musical_note:**Queued** {sr.Title.TrimTo(55)}");
                    };
                }
                sr.OnCompleted += async () => {
                    MusicControls mc;
                    if (musicPlayers.TryGetValue(e.Server, out mc)) {
                        if (mc.SongQueue.Count == 0)
                            mc.Stop();
                    }
                    await e.Send($":musical_note:**Finished playing** {sr.Title.TrimTo(55)}");
                };
                sr.OnStarted += async () => {
                    if (msg == null)
                        await e.Send($":musical_note:**Playing ** {sr.Title.TrimTo(55)} **Volume:** {(int)(player.Volume * 100)}%");
                    else
                        await msg.Edit($":musical_note:**Playing ** {sr.Title.TrimTo(55)} **Volume:** {(int)(player.Volume * 100)}%");
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
        }
    }
}
