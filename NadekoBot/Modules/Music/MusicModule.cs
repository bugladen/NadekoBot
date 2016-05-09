using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Music.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Music
{
    internal class MusicModule : DiscordModule
    {

        public static ConcurrentDictionary<Server, MusicPlayer> MusicPlayers = new ConcurrentDictionary<Server, MusicPlayer>();
        public static ConcurrentDictionary<ulong, float> DefaultMusicVolumes = new ConcurrentDictionary<ulong, float>();

        public MusicModule()
        {
            // ready for 1.0
            //NadekoBot.Client.UserUpdated += (s, e) =>
            //{
            //    try
            //    {
            //        if (e.Before.VoiceChannel != e.After.VoiceChannel &&
            //           e.Before.VoiceChannel.Members.Count() == 0)
            //        {
            //            MusicPlayer musicPlayer;
            //            if (!MusicPlayers.TryRemove(e.Server, out musicPlayer)) return;
            //            musicPlayer.Destroy();
            //        }
            //    }
            //    catch { }
            //};

        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Music;

        public override void Install(ModuleManager manager)
        {
            var client = NadekoBot.Client;

            manager.CreateCommands(Prefix, cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("n")
                    .Alias("next")
                    .Alias("skip")
                    .Description("Goes to the next song in the queue. You have to be in the same voice channel as the bot.\n**Usage**: `!m n`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        if (musicPlayer.PlaybackVoiceChannel == e.User.VoiceChannel)
                            musicPlayer.Next();
                    });

                cgb.CreateCommand("s")
                    .Alias("stop")
                    .Description("Stops the music and clears the playlist. Stays in the channel.\n**Usage**: `!m s`")
                    .Do(async e =>
                    {
                        await Task.Run(() =>
                        {
                            MusicPlayer musicPlayer;
                            if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                            if (e.User.VoiceChannel == musicPlayer.PlaybackVoiceChannel)
                                musicPlayer.Stop();
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand("d")
                    .Alias("destroy")
                    .Description("Completely stops the music and unbinds the bot from the channel. " +
                                 "(may cause weird behaviour)\n**Usage**: `!m d`")
                    .Do(async e =>
                    {
                        await Task.Run(() =>
                        {
                            MusicPlayer musicPlayer;
                            if (!MusicPlayers.TryRemove(e.Server, out musicPlayer)) return;
                            if (e.User.VoiceChannel == musicPlayer.PlaybackVoiceChannel)
                                musicPlayer.Destroy();
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand("p")
                    .Alias("pause")
                    .Description("Pauses or Unpauses the song.\n**Usage**: `!m p`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.TogglePause();
                        if (musicPlayer.Paused)
                            await e.Channel.SendMessage("🎵`Music Player paused.`").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("🎵`Music Player unpaused.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("q")
                    .Alias("yq")
                    .Description("Queue a song using keywords or a link. Bot will join your voice channel." +
                                 "**You must be in a voice channel**.\n**Usage**: `!m q Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await QueueSong(e.Channel, e.User.VoiceChannel, e.GetArg("query")).ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("lq")
                    .Alias("ls").Alias("lp")
                    .Description("Lists up to 15 currently queued songs.\n**Usage**: `!m lq`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                        {
                            await e.Channel.SendMessage("🎵 No active music player.").ConfigureAwait(false);
                            return;
                        }
                        var currentSong = musicPlayer.CurrentSong;
                        if (currentSong == null)
                            return;
                        var toSend = $"🎵`Now Playing` {currentSong.PrettyName} " + $"{currentSong.PrettyCurrentTime()}\n";
                        if (musicPlayer.RepeatSong)
                            toSend += "🔂";
                        else if (musicPlayer.RepeatPlaylist)
                            toSend += "🔁";
                        toSend += $" **{musicPlayer.Playlist.Count}** `tracks currently queued.` ";
                        if (musicPlayer.Playlist.Count >= MusicPlayer.MaximumPlaylistSize)
                            toSend += "**Song queue is full!**\n";
                        else
                            toSend += "\n";
                        var number = 1;
                        await e.Channel.SendMessage(toSend + string.Join("\n", musicPlayer.Playlist.Take(15).Select(v => $"`{number++}.` {v.PrettyName}"))).ConfigureAwait(false);
                    });

                cgb.CreateCommand("np")
                    .Alias("playing")
                    .Description("Shows the song currently playing.\n**Usage**: `!m np`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var currentSong = musicPlayer.CurrentSong;
                        if (currentSong == null)
                            return;
                        await e.Channel.SendMessage($"🎵`Now Playing` {currentSong.PrettyName} " +
                                                    $"{currentSong.PrettyCurrentTime()}").ConfigureAwait(false);
                    });

                cgb.CreateCommand("vol")
                    .Description("Sets the music volume 0-100%\n**Usage**: `!m vol 50`")
                    .Parameter("val", ParameterType.Required)
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        var arg = e.GetArg("val");
                        int volume;
                        if (!int.TryParse(arg, out volume))
                        {
                            await e.Channel.SendMessage("Volume number invalid.").ConfigureAwait(false);
                            return;
                        }
                        volume = musicPlayer.SetVolume(volume);
                        await e.Channel.SendMessage($"🎵 `Volume set to {volume}%`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("dv")
                    .Alias("defvol")
                    .Description("Sets the default music volume when music playback is started (0-100)." +
                                 " Does not persist through restarts.\n**Usage**: `!m dv 80`")
                    .Parameter("val", ParameterType.Required)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("val");
                        float volume;
                        if (!float.TryParse(arg, out volume) || volume < 0 || volume > 100)
                        {
                            await e.Channel.SendMessage("Volume number invalid.").ConfigureAwait(false);
                            return;
                        }
                        DefaultMusicVolumes.AddOrUpdate(e.Server.Id, volume / 100, (key, newval) => volume / 100);
                        await e.Channel.SendMessage($"🎵 `Default volume set to {volume}%`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("min").Alias("mute")
                    .Description("Sets the music volume to 0%\n**Usage**: `!m min`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.SetVolume(0);
                    });

                cgb.CreateCommand("max")
                    .Description("Sets the music volume to 100% (real max is actually 150%).\n**Usage**: `!m max`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.SetVolume(100);
                    });

                cgb.CreateCommand("half")
                    .Description("Sets the music volume to 50%.\n**Usage**: `!m half`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.SetVolume(50);
                    });

                cgb.CreateCommand("sh")
                    .Description("Shuffles the current playlist.\n**Usage**: `!m sh`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        if (musicPlayer.Playlist.Count < 2)
                        {
                            await e.Channel.SendMessage("💢 Not enough songs in order to perform the shuffle.").ConfigureAwait(false);
                            return;
                        }

                        musicPlayer.Shuffle();
                        await e.Channel.SendMessage("🎵 `Songs shuffled.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("pl")
                    .Description("Queues up to 50 songs from a youtube playlist specified by a link, or keywords.\n**Usage**: `!m pl playlist link or name`")
                    .Parameter("playlist", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("playlist");
                        if (string.IsNullOrWhiteSpace(arg))
                            return;
                        if (e.User.VoiceChannel?.Server != e.Server)
                        {
                            await e.Channel.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.").ConfigureAwait(false);
                            return;
                        }
                        var plId = await SearchHelper.GetPlaylistIdByKeyword(arg).ConfigureAwait(false);
                        if (plId == null)
                        {
                            await e.Channel.SendMessage("No search results for that query.");
                            return;
                        }
                        var ids = await SearchHelper.GetVideoIDs(plId, 500).ConfigureAwait(false);
                        if (ids == null || ids.Count == 0)
                        {
                            await e.Channel.SendMessage($"🎵`Failed to find any songs.`");
                            return;
                        }
                        //todo TEMPORARY SOLUTION, USE RESOLVE QUEUE IN THE FUTURE
                        var idArray = ids as string[] ?? ids.ToArray();
                        var count = idArray.Length;
                        var msg =
                            await e.Channel.SendMessage($"🎵 `Attempting to queue {count} songs".SnPl(count) + "...`").ConfigureAwait(false);
                        foreach (var id in idArray)
                        {
                            try
                            {
                                await QueueSong(e.Channel, e.User.VoiceChannel, id, true).ConfigureAwait(false);
                            }
                            catch { }
                        }
                        await msg.Edit("🎵 `Playlist queue complete.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("lopl")
                    .Description("Queues all songs from a directory. **Owner Only!**\n**Usage**: `!m lopl C:/music/classical`")
                    .Parameter("directory", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        var arg = e.GetArg("directory");
                        if (string.IsNullOrWhiteSpace(arg))
                            return;
                        try
                        {
                            var fileEnum = new DirectoryInfo(arg).GetFiles()
                                                .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System));
                            foreach (var file in fileEnum)
                            {
                                await QueueSong(e.Channel, e.User.VoiceChannel, file.FullName, true, MusicType.Local).ConfigureAwait(false);
                            }
                            await e.Channel.SendMessage("🎵 `Directory queue complete.`").ConfigureAwait(false);
                        }
                        catch { }
                    });

                cgb.CreateCommand("radio").Alias("ra")
                    .Description("Queues a radio stream from a link. It can be a direct mp3 radio stream, .m3u, .pls .asx or .xspf\n**Usage**: `!m ra radio link here`")
                    .Parameter("radio_link", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.VoiceChannel?.Server != e.Server)
                        {
                            await e.Channel.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.").ConfigureAwait(false);
                            return;
                        }
                        await QueueSong(e.Channel, e.User.VoiceChannel, e.GetArg("radio_link"), musicType: MusicType.Radio).ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("lo")
                    .Description("Queues a local file by specifying a full path. **Owner Only!**\n**Usage**: `!m lo C:/music/mysong.mp3`")
                    .Parameter("path", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        var arg = e.GetArg("path");
                        if (string.IsNullOrWhiteSpace(arg))
                            return;
                        await QueueSong(e.Channel, e.User.VoiceChannel, e.GetArg("path"), musicType: MusicType.Local).ConfigureAwait(false);
                    });

                cgb.CreateCommand("mv")
                    .Description("Moves the bot to your voice channel. (works only if music is already playing)\n**Usage**: `!m mv`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        var voiceChannel = e.User.VoiceChannel;
                        if (voiceChannel == null || voiceChannel.Server != e.Server || !MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.MoveToVoiceChannel(voiceChannel);
                    });

                cgb.CreateCommand("rm")
                    .Description("Remove a song by its # in the queue, or 'all' to remove whole queue.\n**Usage**: `!m rm 5`")
                    .Parameter("num", ParameterType.Required)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("num");
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                        {
                            return;
                        }
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        if (arg?.ToLower() == "all")
                        {
                            musicPlayer.ClearQueue();
                            await e.Channel.SendMessage($"🎵`Queue cleared!`").ConfigureAwait(false);
                            return;
                        }
                        int num;
                        if (!int.TryParse(arg, out num))
                        {
                            return;
                        }
                        if (num <= 0 || num > musicPlayer.Playlist.Count)
                            return;
                        var song = (musicPlayer.Playlist as List<Song>)?[num - 1];
                        musicPlayer.RemoveSongAt(num - 1);
                        await e.Channel.SendMessage($"🎵**Track {song.PrettyName} at position `#{num}` has been removed.**").ConfigureAwait(false);
                    });

                cgb.CreateCommand("cleanup")
                    .Description("Cleans up hanging voice connections. **Owner Only!**\n**Usage**: `!m cleanup`")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(e =>
                    {
                        foreach (var kvp in MusicPlayers)
                        {
                            var songs = kvp.Value.Playlist;
                            var currentSong = kvp.Value.CurrentSong;
                            if (songs.Count == 0 && currentSong == null)
                            {
                                MusicPlayer throwaway;
                                MusicPlayers.TryRemove(kvp.Key, out throwaway);
                                throwaway.Destroy();
                            }
                        }
                    });

                cgb.CreateCommand("rcs")
                    .Alias("repeatcurrentsong")
                    .Description("Toggles repeat of current song.\n**Usage**: `!m rcs`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var currentSong = musicPlayer.CurrentSong;
                        if (currentSong == null)
                            return;
                        var currentValue = musicPlayer.ToggleRepeatSong();
                        await e.Channel.SendMessage(currentValue ?
                                                    $"🎵🔂`Repeating track:`{currentSong.PrettyName}" :
                                                    $"🎵🔂`Current track repeat stopped.`")
                                                        .ConfigureAwait(false);
                    });

                cgb.CreateCommand("rpl")
                    .Alias("repeatplaylist")
                    .Description("Toggles repeat of all songs in the queue (every song that finishes is added to the end of the queue).\n**Usage**: `!m rpl`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var currentValue = musicPlayer.ToggleRepeatPlaylist();
                        await e.Channel.SendMessage($"🎵🔁`Repeat playlist {(currentValue ? "enabled" : "disabled")}`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("save")
                    .Description("Saves a playlist under a certain name. Name must be no longer than 20 characters and mustn't contain dashes.\n**Usage**: `!m save classical1`")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var name = e.GetArg("name")?.Trim();

                        if (string.IsNullOrWhiteSpace(name) ||
                            name.Length > 20 ||
                            name.Contains("-"))
                            return;

                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;

                        //to avoid concurrency issues
                        var currentPlaylist = new List<Song>(musicPlayer.Playlist);
                        var curSong = musicPlayer.CurrentSong;
                        if (curSong != null)
                            currentPlaylist.Insert(0, curSong);

                        if (!currentPlaylist.Any())
                            return;


                        var songInfos = currentPlaylist.Select(s => new DataModels.SongInfo
                        {
                            Provider = s.SongInfo.Provider,
                            ProviderType = (int)s.SongInfo.ProviderType,
                            Title = s.SongInfo.Title,
                            Uri = s.SongInfo.Uri,
                            Query = s.SongInfo.Query,
                        }).ToList();

                        var playlist = new MusicPlaylist
                        {
                            CreatorId = (long)e.User.Id,
                            CreatorName = e.User.Name,
                            Name = name.ToLowerInvariant(),
                        };
                        DbHandler.Instance.SaveAll(songInfos);
                        DbHandler.Instance.Save(playlist);
                        DbHandler.Instance.InsertMany(songInfos.Select(s => new PlaylistSongInfo
                        {
                            PlaylistId = playlist.Id.Value,
                            SongInfoId = s.Id.Value
                        }));

                        await e.Channel.SendMessage($"🎵 `Saved playlist as {name}-{playlist.Id}`").ConfigureAwait(false);

                    });

                cgb.CreateCommand("load")
                    .Description("Loads a playlist under a certain name. \n**Usage**: `!m load classical-1`")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var voiceCh = e.User.VoiceChannel;
                        var textCh = e.Channel;
                        if (voiceCh == null || voiceCh.Server != textCh.Server)
                        {
                            await textCh.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining.").ConfigureAwait(false);
                            return;
                        }
                        var name = e.GetArg("name")?.Trim().ToLowerInvariant();

                        if (string.IsNullOrWhiteSpace(name))
                            return;

                        var parts = name.Split('-');
                        if (parts.Length != 2)
                            return;
                        var playlistName = parts[0];

                        int playlistNumber;
                        if (!int.TryParse(parts[1], out playlistNumber))
                            return;

                        var playlist = DbHandler.Instance.FindOne<MusicPlaylist>(
                            p => p.Id == playlistNumber);

                        if (playlist == null)
                        {
                            await e.Channel.SendMessage("Can't find playlist under that name.").ConfigureAwait(false);
                            return;
                        }

                        var psis = DbHandler.Instance.FindAll<PlaylistSongInfo>(psi =>
                            psi.PlaylistId == playlist.Id);

                        var songInfos = psis.Select(psi => DbHandler.Instance
                            .FindOne<DataModels.SongInfo>(si => si.Id == psi.SongInfoId));

                        await e.Channel.SendMessage($"`Attempting to load {songInfos.Count()} songs`").ConfigureAwait(false);
                        foreach (var si in songInfos)
                        {
                            try
                            {
                                await QueueSong(textCh, voiceCh, si.Query, true, (MusicType)si.ProviderType).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed QueueSong in load playlist. {ex}");
                            }
                        }
                    });

                cgb.CreateCommand("playlists")
                    .Alias("pls")
                    .Description("Lists all playlists. Paginated. 20 per page. Default page is 0.\n**Usage**:`!m pls 1`")
                    .Parameter("num", ParameterType.Optional)
                    .Do(e =>
                    {
                        int num = 0;
                        int.TryParse(e.GetArg("num"), out num);
                        if (num < 0)
                            return;
                        var result = DbHandler.Instance.GetPlaylistData(num);
                        if (result.Count == 0)
                            e.Channel.SendMessage($"`No saved playlists found on page {num}`");
                        else
                            e.Channel.SendMessage($"```js\n--- List of saved playlists ---\n\n" + string.Join("\n", result.Select(r => $"'{r.Name}-{r.Id}' by {r.Creator} ({r.SongCnt} songs)")) + $"\n\n        --- Page {num} ---```");
                    });

                cgb.CreateCommand("goto")
                    .Description("Goes to a specific time in seconds in a song.")
                    .Parameter("time")
                    .Do(async e =>
                    {
                        var skipToStr = e.GetArg("time")?.Trim();
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        int skipTo;
                        if (!int.TryParse(skipToStr, out skipTo) || skipTo < 0)
                            return;

                        var currentSong = musicPlayer.CurrentSong;

                        if (currentSong == null)
                            return;

                        //currentSong.PrintStatusMessage = false;
                        var gotoSong = currentSong.Clone();
                        gotoSong.SkipTo = skipTo;
                        musicPlayer.AddSong(gotoSong, 0);
                        musicPlayer.Next();

                        var minutes = (skipTo / 60).ToString();
                        var seconds = (skipTo % 60).ToString();

                        if (minutes.Length == 1)
                            minutes = "0" + minutes;
                        if (seconds.Length == 1)
                            seconds = "0" + seconds;

                        await e.Channel.SendMessage($"`Skipped to {minutes}:{seconds}`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("getlink")
                    .Alias("gl")
                    .Description("Shows a link to the currently playing song.")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var curSong = musicPlayer.CurrentSong;
                        if (curSong == null)
                            return;
                        await e.Channel.SendMessage($"🎶`Current song:` <{curSong.SongInfo.Query}>");
                    });
            });
        }

        private async Task QueueSong(Channel textCh, Channel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
        {
            if (voiceCh == null || voiceCh.Server != textCh.Server)
            {
                if (!silent)
                    await textCh.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining.").ConfigureAwait(false);
                throw new ArgumentNullException(nameof(voiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("💢 Invalid query for queue song.", nameof(query));

            var musicPlayer = MusicPlayers.GetOrAdd(textCh.Server, server =>
            {
                float? vol = null;
                float throwAway;
                if (DefaultMusicVolumes.TryGetValue(server.Id, out throwAway))
                    vol = throwAway;
                var mp = new MusicPlayer(voiceCh, vol);


                Message playingMessage = null;
                Message lastFinishedMessage = null;
                mp.OnCompleted += async (s, song) =>
                {
                    if (song.PrintStatusMessage)
                    {
                        try
                        {
                            if (lastFinishedMessage != null)
                                await lastFinishedMessage.Delete().ConfigureAwait(false);
                            if (playingMessage != null)
                                await playingMessage.Delete().ConfigureAwait(false);
                            lastFinishedMessage = await textCh.SendMessage($"🎵`Finished`{song.PrettyName}").ConfigureAwait(false);
                        }
                        catch { }
                    }
                };
                mp.OnStarted += async (s, song) =>
                {
                    if (song.PrintStatusMessage)
                    {
                        var sender = s as MusicPlayer;
                        if (sender == null)
                            return;

                        try
                        {

                            var msgTxt = $"🎵`Playing`{song.PrettyName} `Vol: {(int)(sender.Volume * 100)}%`";
                            playingMessage = await textCh.SendMessage(msgTxt).ConfigureAwait(false);
                        }
                        catch { }
                    }
                };
                return mp;
            });
            var resolvedSong = await Song.ResolveSong(query, musicType).ConfigureAwait(false);
            resolvedSong.MusicPlayer = musicPlayer;

            musicPlayer.AddSong(resolvedSong);
            if (!silent)
            {
                var queuedMessage = await textCh.SendMessage($"🎵`Queued`{resolvedSong.PrettyName} **at** `#{musicPlayer.Playlist.Count}`").ConfigureAwait(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
                {
                    await Task.Delay(10000).ConfigureAwait(false);
                    try
                    {
                        await queuedMessage.Delete().ConfigureAwait(false);
                    }
                    catch { }
                }).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
    }
}
