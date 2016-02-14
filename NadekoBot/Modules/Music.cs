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
using NadekoBot.Classes;
using Discord.Audio;

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

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

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
                    .Description("Pauses or Unpauses the song")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) return;
                        if (musicPlayers[e.Server].TogglePause())
                            await e.Send("🎵`Music player paused.`");
                        else
                            await e.Send("🎵`Music player unpaused.`");
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using keywords or link. Bot will join your voice channel. **You must be in a voice channel**.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e => await QueueSong(e,e.GetArg("query")));

                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 10 currently queued songs.")
                    .Do(async e => {
                        if (musicPlayers.ContainsKey(e.Server) == false) {
                            await e.Send("🎵 No active music player.");
                            return;
                        }
                        var player = musicPlayers[e.Server];
                        string toSend = "🎵 **" + player.SongQueue.Count + "** `videos currently queued.` ";
                        if (player.SongQueue.Count >= 25)
                            toSend += "**Song queue is full!**\n";
                        await e.Send(toSend);
                        int number = 1;
                        await e.Send(string.Join("\n", player.SongQueue.Take(10).Select(v => $"`{number++}.` {v.FullPrettyName}")));
                    });

                cgb.CreateCommand("np")
                 .Alias("playing")
                 .Description("Shows the song currently playing.")
                 .Do(async e => {
                     if (musicPlayers.ContainsKey(e.Server) == false) return;
                     var player = musicPlayers[e.Server];
                     await e.Send($"🎵`Now Playing` {player.CurrentSong.FullPrettyName}");
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
                      volume = player.SetVolume(volume);
                      await e.Send($"🎵 `Volume set to {volume}%`");
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
                        await e.Send("🎵 `Songs shuffled.`");
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

                        await e.Send("`Music status " + (setgameEnabled ? "enabled`" : "disabled`"));
                    });

                cgb.CreateCommand("pl")
                    .Description("Queues up to 25 songs from a youtube playlist specified by a link, or keywords.")
                    .Parameter("playlist", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.VoiceChannel?.Server != e.Server) {
                            await e.Send("💢 You need to be in the voice channel on this server.");
                            return;
                        }
                        var ids = await SearchHelper.GetVideoIDs(await SearchHelper.GetPlaylistIdByKeyword(e.GetArg("playlist")));
                        //todo TEMPORARY SOLUTION, USE RESOLVE QUEUE IN THE FUTURE
                        var msg = await e.Send($"🎵 Attempting to queue **{ids.Count}** songs".SnPl(ids.Count));
                        foreach (var id in ids) {
                            Task.Run(async () => await QueueSong(e, id, true)).ConfigureAwait(false);
                            await Task.Delay(150);
                        }
                        msg?.Edit("🎵 `Playlist queue complete.`");
                    });

                cgb.CreateCommand("radio").Alias("ra")
                    .Description("Queues a direct radio stream from a link.")
                    .Parameter("radio_link", ParameterType.Required)
                    .Do(async e => {
                        if (e.User.VoiceChannel?.Server != e.Server) {
                            await e.Send("💢 You need to be in the voice channel on this server.");
                            return;
                        }
                        await QueueSong(e, e.GetArg("radio_link"), radio: true);
                    });

                cgb.CreateCommand("mv")
                  .Description("Moves the bot to your voice channel. (works only if music is already playing)")
                  .Do(async e => {
                      MusicControls mc;
                      if (e.User.VoiceChannel == null || e.User.VoiceChannel.Server != e.Server || !musicPlayers.TryGetValue(e.Server,out mc))
                          return;
                      mc.VoiceChannel = e.User.VoiceChannel;
                      mc.VoiceClient = await mc.VoiceChannel.JoinAudio();
                  });

                cgb.CreateCommand("debug")
                    .Description("Writes some music data to console. **BOT OWNER ONLY**")
                    .Do(e => {
                        if (NadekoBot.OwnerID != e.User.Id)
                            return;
                        var output = "SERVER_NAME---SERVER_ID-----USERCOUNT----QUEUED\n" +
                            string.Join("\n", musicPlayers.Select(kvp => kvp.Key.Name + "--" + kvp.Key.Id + " --" + kvp.Key.Users.Count() + "--" + kvp.Value.SongQueue.Count));
                        Console.WriteLine(output);
                    });
            });
        }

        private async Task QueueSong(CommandEventArgs e, string query, bool silent = false, bool radio = false) {
            if (e.User.VoiceChannel?.Server != e.Server) {
                await e.Send("💢 You need to be in the voice channel on this server.");
                return;
            }
            if (musicPlayers.ContainsKey(e.Server) == false)
                if (!musicPlayers.TryAdd(e.Server, new MusicControls(e.User.VoiceChannel, e))) {
                    await e.Send("Failed to create a music player for this server.");
                    return;
                }
            if (query == null || query.Length < 4)
                return;

            var player = musicPlayers[e.Server];

            if (player.SongQueue.Count >= 25) return;

            try {
                var sr = await Task.Run(() => new StreamRequest(e, query, player, radio));

                if (sr == null)
                    throw new NullReferenceException("StreamRequest is null.");

                Message qmsg = null;
                Message msg = null;
                if (!silent) {
                    qmsg = await e.Channel.SendMessage("🎵 `Searching...`");
                    sr.OnResolving += async () => {
                        await qmsg.Edit($"🎵 `Resolving`... \"{query}\"");
                    };
                    sr.OnResolvingFailed += async (err) => {
                        await qmsg.Edit($"💢 🎵 `Resolving failed` for **{query}**");
                    };
                    sr.OnQueued += async () => {
                        await qmsg.Edit($"🎵`Queued`{sr.FullPrettyName}");
                    };
                }
                sr.OnCompleted += async () => {
                    MusicControls mc;
                    if (musicPlayers.TryGetValue(e.Server, out mc)) {
                        if (mc.SongQueue.Count == 0)
                            mc.Stop();
                    }
                    await e.Send($"🎵`Finished`{sr.FullPrettyName}");
                };
                sr.OnStarted += async () => {
                    var msgTxt = $"🎵`Playing`{sr.FullPrettyName} `Vol: {(int)(player.Volume * 100)}%`";
                    if (msg == null)
                        await e.Send(msgTxt);
                    else
                        await msg.Edit(msgTxt);
                    qmsg?.Delete();
                };
                sr.OnBuffering += async () => {
                    msg = await e.Send($"🎵`Buffering...`{sr.FullPrettyName}");
                };
            } catch (Exception ex) {
                Console.WriteLine();
                await e.Send($"💢 {ex.Message}");
                return;
            }
        }
    }
}
