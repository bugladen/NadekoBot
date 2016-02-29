using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.Music;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace NadekoBot.Modules {
    class Music : DiscordModule {

        public static ConcurrentDictionary<Server, MusicPlayer> musicPlayers = new ConcurrentDictionary<Server, MusicPlayer>();
        public static ConcurrentDictionary<ulong, float> defaultMusicVolumes = new ConcurrentDictionary<ulong, float>();

        Timer setgameTimer => new Timer();

        bool setgameEnabled = false;

        public Music() : base() {

            setgameTimer.Interval = 20000;
            setgameTimer.Elapsed += (s, e) => {
                try {
                    int num = musicPlayers.Where(kvp => kvp.Value.CurrentSong != null).Count();
                    NadekoBot.client.SetGame($"{num} songs".SnPl(num) + $", {musicPlayers.Sum(kvp => kvp.Value.Playlist.Count())} queued");
                }
                catch { }
            };

        }

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;

            manager.CreateCommands("!m", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("n")
                    .Alias("next")
                    .Description("Goes to the next song in the queue.")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        musicPlayer.Next();
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Completely stops the music, unbinds the bot from the channel, and cleans up files.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        musicPlayer.Stop();
                        var msg = await e.Channel.SendMessage("⚠Due to music issues, NadekoBot is unable to leave voice channels at this moment.\nIf this presents inconvenience, you can use `!m mv` command to make her join your current voice channel.");
                        await Task.Delay(5000);
                        try {
                            await msg.Delete();
                        }
                        catch { }
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses or Unpauses the song.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        musicPlayer.TogglePause();
                        if (musicPlayer.Paused)
                            await e.Channel.SendMessage("🎵`Music musicPlayer paused.`");
                        else
                            await e.Channel.SendMessage("🎵`Music musicPlayer unpaused.`");
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
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer)) {
                            await e.Channel.SendMessage("🎵 No active music musicPlayer.");
                            return;
                        }
                        string toSend = "🎵 **" + musicPlayer.Playlist.Count + "** `videos currently queued.` ";
                        if (musicPlayer.Playlist.Count >= MusicPlayer.MaximumPlaylistSize)
                            toSend += "**Song queue is full!**\n";
                        else
                            toSend += "\n";
                        int number = 1;
                        await e.Channel.SendMessage(toSend + string.Join("\n", musicPlayer.Playlist.Take(15).Select(v => $"`{number++}.` {v.PrettyName}")));
                    });

                cgb.CreateCommand("np")
                    .Alias("playing")
                    .Description("Shows the song currently playing.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        await e.Channel.SendMessage($"🎵`Now Playing` {musicPlayer.CurrentSong.PrettyName}");
                    });

                cgb.CreateCommand("vol")
                    .Description("Sets the music volume 0-150%")
                    .Parameter("val", ParameterType.Required)
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer))
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
                        defaultMusicVolumes.AddOrUpdate(e.Server.Id, volume / 100, (key, newval) => volume / 100);
                        await e.Channel.SendMessage($"🎵 `Default volume set to {volume}%`");
                    });

                cgb.CreateCommand("min").Alias("mute")
                    .Description("Sets the music volume to 0%")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.SetVolume(0);
                    });

                cgb.CreateCommand("max")
                    .Description("Sets the music volume to 100% (real max is actually 150%).")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.SetVolume(100);
                    });

                cgb.CreateCommand("half")
                    .Description("Sets the music volume to 50%.")
                    .Do(e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.SetVolume(50);
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.")
                    .Do(async e => {
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (musicPlayer.Playlist.Count < 2) {
                            await e.Channel.SendMessage("💢 Not enough songs in order to perform the shuffle.");
                            return;
                        }

                        musicPlayer.Shuffle();
                        await e.Channel.SendMessage("🎵 `Songs shuffled.`");
                    });

                cgb.CreateCommand("setgame")
                    .Description("Sets the game of the bot to the number of songs playing.**Owner only**")
                    .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                    .Do(async e => {
                        setgameEnabled = !setgameEnabled;
                        if (setgameEnabled)
                            setgameTimer.Start();
                        else
                            setgameTimer.Stop();

                        await e.Channel.SendMessage("`Music status " + (setgameEnabled ? "enabled`" : "disabled`"));
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
                        var msg = await e.Channel.SendMessage($"🎵 `Attempting to queue {ids.Count} songs".SnPl(ids.Count) + "...`");
                        foreach (var id in ids) {
                            await QueueSong(e.Channel, e.User.VoiceChannel, id, true);
                        }
                        await msg.Edit("🎵 `Playlist queue complete.`");
                    });

                cgb.CreateCommand("lopl")
                    .Description("Queues up to 50 songs from a directory.")
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
                        }
                        catch { }
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
                    .Description("Queues a local file by specifying a full path. BOT OWNER ONLY.")
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
                        if (voiceChannel == null || voiceChannel.Server != e.Server || !musicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.MoveToVoiceChannel(voiceChannel);
                    });

                cgb.CreateCommand("rm")
                    .Description("Remove a song by its # in the queue, or 'all' to remove whole queue.")
                    .Parameter("num", ParameterType.Required)
                    .Do(async e => {
                        var arg = e.GetArg("num");
                        MusicPlayer musicPlayer;
                        if (!musicPlayers.TryGetValue(e.Server, out musicPlayer)) {
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

                //cgb.CreateCommand("debug")
                //    .Description("Does something magical. **BOT OWNER ONLY**")
                //    .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                //    .Do(e => {
                //        var inactivePlayers = 
                //        Console.WriteLine("");
                //    });
            });
        }

        private async Task QueueSong(Channel TextCh, Channel VoiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal) {
            if (VoiceCh == null || VoiceCh.Server != TextCh.Server) {
                if(!silent)
                    await TextCh.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining.");
                throw new ArgumentNullException(nameof(VoiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("💢 Invalid query for queue song.", nameof(query));
            MusicPlayer musicPlayer = null;
            if (!musicPlayers.TryGetValue(TextCh.Server, out musicPlayer)) {
                float? vol = null;
                float throwAway;
                if (defaultMusicVolumes.TryGetValue(TextCh.Server.Id, out throwAway))
                    vol = throwAway;
                musicPlayer = new MusicPlayer(VoiceCh, vol) {
                    OnCompleted = async (song) => {
                        try {
                            await TextCh.SendMessage($"🎵`Finished`{song.PrettyName}");
                        }
                        catch { }
                    },
                    OnStarted = async (song) => {
                        try {
                            var msgTxt = $"🎵`Playing`{song.PrettyName} `Vol: {(int)(musicPlayer.Volume * 100)}%`";
                            await TextCh.SendMessage(msgTxt);
                        }
                        catch { }
                    },
                };
                musicPlayers.TryAdd(TextCh.Server, musicPlayer);
            }
            var resolvedSong = await Song.ResolveSong(query, musicType);
            resolvedSong.MusicPlayer = musicPlayer;
            if(!silent)
                await TextCh.Send($"🎵`Queued`{resolvedSong.PrettyName}");
            musicPlayer.AddSong(resolvedSong);
        }
    }
}
