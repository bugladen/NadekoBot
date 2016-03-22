using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.Music;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Classes.Permissions;
using Timer = System.Timers.Timer;

namespace NadekoBot.Modules {
    internal class Music : DiscordModule {

        public static ConcurrentDictionary<Server, MusicPlayer> MusicPlayers = new ConcurrentDictionary<Server, MusicPlayer>();
        public static ConcurrentDictionary<ulong, float> DefaultMusicVolumes = new ConcurrentDictionary<ulong, float>();

        private readonly Timer setgameTimer = new Timer();

        private bool setgameEnabled = false;

        public Music() {

            setgameTimer.Interval = 20000;
            setgameTimer.Elapsed += (s, e) => {
                try {
                    var num = MusicPlayers.Count(kvp => kvp.Value.CurrentSong != null);
                    NadekoBot.Client.SetGame($"{num} songs".SnPl(num) + $", {MusicPlayers.Sum(kvp => kvp.Value.Playlist.Count())} queued");
                } catch { }
            };

        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Music;

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.Client;

            manager.CreateCommands(Prefix, cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("n")
                    .Alias("next")
                    .Description("Goes to the next song in the queue.")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        musicPlayer.Next();
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Stops the music and clears the playlist. Stays in the channel.")
                    .Do(async e => {
                        await Task.Run(() => {
                            MusicPlayer musicPlayer;
                            if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                            musicPlayer.Stop();
                        });
                    });

                cgb.CreateCommand("d")
                    .Alias("destroy")
                    .Description("Completely stops the music and unbinds the bot from the channel. (may cause weird behaviour)")
                    .Do(async e => {
                        await Task.Run(() => {
                            MusicPlayer musicPlayer;
                            if (!MusicPlayers.TryRemove(e.Server, out musicPlayer)) return;
                            musicPlayer.Destroy();
                        });
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses or Unpauses the song.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        musicPlayer.TogglePause();
                        if (musicPlayer.Paused)
                            await e.Channel.SendMessage("🎵`Music Player paused.`");
                        else
                            await e.Channel.SendMessage("🎵`Music Player unpaused.`");
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using keywords or a link. Bot will join your voice channel. **You must be in a voice channel**.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e => {
                        await QueueSong(e.Channel, e.User.VoiceChannel, e.GetArg("query"));
                    });

                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 15 currently queued songs.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) {
                            await e.Channel.SendMessage("🎵 No active music player.");
                            return;
                        }
                        var toSend = "🎵 **" + musicPlayer.Playlist.Count + "** `tracks currently queued.` ";
                        if (musicPlayer.Playlist.Count >= MusicPlayer.MaximumPlaylistSize)
                            toSend += "**Song queue is full!**\n";
                        else
                            toSend += "\n";
                        var number = 1;
                        await e.Channel.SendMessage(toSend + string.Join("\n", musicPlayer.Playlist.Take(15).Select(v => $"`{number++}.` {v.PrettyName}")));
                    });

                cgb.CreateCommand("np")
                    .Alias("playing")
                    .Description("Shows the song currently playing.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var currentSong = musicPlayer.CurrentSong;
                        if (currentSong != null)
                            await e.Channel.SendMessage($"🎵`Now Playing` {currentSong.PrettyName} " +
                                                        $"{currentSong.PrettyCurrentTime()}");
                    });

                cgb.CreateCommand("vol")
                    .Description("Sets the music volume 0-150%")
                    .Parameter("val", ParameterType.Required)
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var arg = e.GetArg("val");
                        int volume;
                        if (!int.TryParse(arg, out volume)) {
                            await e.Channel.SendMessage("Volume number invalid.");
                            return;
                        }
                        volume = musicPlayer.SetVolume(volume);
                        await e.Channel.SendMessage($"🎵 `Volume set to {volume}%`");
                    });

                cgb.CreateCommand("dv")
                    .Alias("defvol")
                    .Description("Sets the default music volume when music playback is started (0-100). Does not persist through restarts.\n**Usage**: !m dv 80")
                    .Parameter("val", ParameterType.Required)
                    .Do(async e => {
                        var arg = e.GetArg("val");
                        float volume;
                        if (!float.TryParse(arg, out volume) || volume < 0 || volume > 100) {
                            await e.Channel.SendMessage("Volume number invalid.");
                            return;
                        }
                        DefaultMusicVolumes.AddOrUpdate(e.Server.Id, volume / 100, (key, newval) => volume / 100);
                        await e.Channel.SendMessage($"🎵 `Default volume set to {volume}%`");
                    });

                cgb.CreateCommand("min").Alias("mute")
                    .Description("Sets the music volume to 0%")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.SetVolume(0);
                    });

                cgb.CreateCommand("max")
                    .Description("Sets the music volume to 100% (real max is actually 150%).")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.SetVolume(100);
                    });

                cgb.CreateCommand("half")
                    .Description("Sets the music volume to 50%.")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.SetVolume(50);
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (musicPlayer.Playlist.Count < 2) {
                            await e.Channel.SendMessage("💢 Not enough songs in order to perform the shuffle.");
                            return;
                        }

                        musicPlayer.Shuffle();
                        await e.Channel.SendMessage("🎵 `Songs shuffled.`");
                    });

                cgb.CreateCommand("setgame")
                    .Description("Sets the game of the bot to the number of songs playing. **Owner only**")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e => {
                        await e.Channel.SendMessage("❗This command is deprecated. " +
                                                    "Use:\n `.ropl`\n `.adpl %playing% songs, %queued% queued.` instead.\n " +
                                                    "It even persists through restarts.");
                    });

                cgb.CreateCommand("pl")
                    .Description("Queues up to 25 songs from a youtube playlist specified by a link, or keywords.")
                    .Parameter("playlist", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.VoiceChannel?.Server != e.Server) {
                            await e.Channel.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.");
                            return;
                        }
                        var ids = await SearchHelper.GetVideoIDs(await SearchHelper.GetPlaylistIdByKeyword(e.GetArg("playlist")));
                        //todo TEMPORARY SOLUTION, USE RESOLVE QUEUE IN THE FUTURE
                        var idArray = ids as string[] ?? ids.ToArray();
                        var count = idArray.Count();
                        var msg =
                            await e.Channel.SendMessage($"🎵 `Attempting to queue {count} songs".SnPl(count) + "...`");
                        foreach (var id in idArray) {
                            try {
                                await QueueSong(e.Channel, e.User.VoiceChannel, id, true);
                            } catch { }
                        }
                        await msg.Edit("🎵 `Playlist queue complete.`");
                    });

                cgb.CreateCommand("lopl")
                    .Description("Queues up to 50 songs from a directory. **Owner Only!**")
                    .Parameter("directory", ParameterType.Unparsed)
                    .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                    .Do(async e => {
                        var arg = e.GetArg("directory");
                        if (string.IsNullOrWhiteSpace(e.GetArg("directory")))
                            return;
                        try {
                            var fileEnum = System.IO.Directory.EnumerateFiles(e.GetArg("directory")).Take(50);
                            foreach (var file in fileEnum) {
                                await QueueSong(e.Channel, e.User.VoiceChannel, file, true, MusicType.Local);
                            }
                            await e.Channel.SendMessage("🎵 `Directory queue complete.`");
                        } catch { }
                    });

                cgb.CreateCommand("radio").Alias("ra")
                    .Description("Queues a direct radio stream from a link.")
                    .Parameter("radio_link", ParameterType.Required)
                    .Do(async e => {
                        if (e.User.VoiceChannel?.Server != e.Server) {
                            await e.Channel.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.");
                            return;
                        }
                        await QueueSong(e.Channel, e.User.VoiceChannel, e.GetArg("radio_link"), musicType: MusicType.Radio);
                    });

                cgb.CreateCommand("lo")
                    .Description("Queues a local file by specifying a full path. **Owner Only!**")
                    .Parameter("path", ParameterType.Unparsed)
                    .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                    .Do(async e => {
                        var arg = e.GetArg("path");
                        if (string.IsNullOrWhiteSpace(arg))
                            return;
                        await QueueSong(e.Channel, e.User.VoiceChannel, e.GetArg("path"), musicType: MusicType.Local);
                    });

                cgb.CreateCommand("mv")
                    .Description("Moves the bot to your voice channel. (works only if music is already playing)")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        var voiceChannel = e.User.VoiceChannel;
                        if (voiceChannel == null || voiceChannel.Server != e.Server || !MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.MoveToVoiceChannel(voiceChannel);
                    });

                cgb.CreateCommand("rm")
                    .Description("Remove a song by its # in the queue, or 'all' to remove whole queue.")
                    .Parameter("num", ParameterType.Required)
                    .Do(async e => {
                        var arg = e.GetArg("num");
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) {
                            return;
                        }
                        if (arg?.ToLower() == "all") {
                            musicPlayer.ClearQueue();
                            await e.Channel.SendMessage($"🎵`Queue cleared!`");
                            return;
                        }
                        int num;
                        if (!int.TryParse(arg, out num)) {
                            return;
                        }
                        if (num <= 0 || num > musicPlayer.Playlist.Count)
                            return;

                        musicPlayer.RemoveSongAt(num - 1);
                        await e.Channel.SendMessage($"🎵**Track at position `#{num}` has been removed.**");
                    });

                cgb.CreateCommand("cleanup")
                    .Description("Cleans up hanging voice connections. **Owner Only!**")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(e => {
                        foreach (var kvp in MusicPlayers) {
                            var songs = kvp.Value.Playlist;
                            var currentSong = kvp.Value.CurrentSong;
                            if (songs.Count == 0 && currentSong == null) {
                                MusicPlayer throwaway;
                                MusicPlayers.TryRemove(kvp.Key, out throwaway);
                                throwaway.Destroy();
                            }
                        }
                    });

                //cgb.CreateCommand("debug")
                //    .Description("Does something magical. **BOT OWNER ONLY**")
                //    .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                //    .Do(e => {
                //        var inactivePlayers = 
                //        Console.WriteLine("");
                //    });
            });
        }

        private async Task QueueSong(Channel textCh, Channel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal) {
            if (voiceCh == null || voiceCh.Server != textCh.Server) {
                if (!silent)
                    await textCh.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining.");
                throw new ArgumentNullException(nameof(voiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("💢 Invalid query for queue song.", nameof(query));

            var musicPlayer = MusicPlayers.GetOrAdd(textCh.Server, server => {
                float? vol = null;
                float throwAway;
                if (DefaultMusicVolumes.TryGetValue(server.Id, out throwAway))
                    vol = throwAway;
                var mp = new MusicPlayer(voiceCh, vol);
                mp.OnCompleted += async (s, song) => {
                    try {
                        await textCh.SendMessage($"🎵`Finished`{song.PrettyName}");
                    } catch { }
                };
                mp.OnStarted += async (s, song) => {
                    var sender = s as MusicPlayer;
                    if (sender == null)
                        return;
                    try {
                        var msgTxt = $"🎵`Playing`{song.PrettyName} `Vol: {(int)(sender.Volume * 100)}%`";
                        await textCh.SendMessage(msgTxt);
                    } catch { }
                };
                return mp;
            });
            var resolvedSong = await Song.ResolveSong(query, musicType);
            resolvedSong.MusicPlayer = musicPlayer;
            if (!silent)
                await textCh.Send($"🎵`Queued`{resolvedSong.PrettyName}");
            musicPlayer.AddSong(resolvedSong);
        }
    }
}
