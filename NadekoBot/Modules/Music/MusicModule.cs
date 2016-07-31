using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Music.Classes;
using NadekoBot.Modules.Permissions.Classes;
using Newtonsoft.Json.Linq;
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

        public const string MusicDataPath = "data/musicdata";

        public MusicModule()
        {
            //it can fail if its currenctly opened or doesn't exist. Either way i don't care
            try { Directory.Delete(MusicDataPath, true); } catch { }

            Directory.CreateDirectory(MusicDataPath);
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Music;

        public override void Install(ModuleManager manager)
        {
            var client = NadekoBot.Client;

            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "next")
                    .Alias(Prefix + "n")
                    .Alias(Prefix + "skip")
                    .Description($"Goes to the next song in the queue. You have to be in the same voice channel as the bot. | `{Prefix}n`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        if (musicPlayer.PlaybackVoiceChannel == e.User.VoiceChannel)
                            musicPlayer.Next();
                    });

                cgb.CreateCommand(Prefix + "stop")
                    .Alias(Prefix + "s")
                    .Description($"Stops the music and clears the playlist. Stays in the channel. | `{Prefix}s`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer)) return;
                        if (e.User.VoiceChannel == musicPlayer.PlaybackVoiceChannel)
                        {
                            musicPlayer.Autoplay = false;
                            musicPlayer.Stop();
                        }
                    });

                cgb.CreateCommand(Prefix + "destroy")
                    .Alias(Prefix + "d")
                    .Description("Completely stops the music and unbinds the bot from the channel. " +
                                 $"(may cause weird behaviour) | `{Prefix}d`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryRemove(e.Server, out musicPlayer)) return;
                        if (e.User.VoiceChannel == musicPlayer.PlaybackVoiceChannel)
                            musicPlayer.Destroy();
                    });

                cgb.CreateCommand(Prefix + "pause")
                    .Alias(Prefix + "p")
                    .Description($"Pauses or Unpauses the song. | `{Prefix}p`")
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

                cgb.CreateCommand(Prefix + "queue")
                    .Alias(Prefix + "q")
                    .Alias(Prefix + "yq")
                    .Description("Queue a song using keywords or a link. Bot will join your voice channel." +
                                 $"**You must be in a voice channel**. | `{Prefix}q Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await QueueSong(e.User, e.Channel, e.User.VoiceChannel, e.GetArg("query")).ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "soundcloudqueue")
                    .Alias(Prefix + "sq")
                    .Description("Queue a soundcloud song using keywords. Bot will join your voice channel." +
                                 $"**You must be in a voice channel**. | `{Prefix}sq Dream Of Venice`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await QueueSong(e.User, e.Channel, e.User.VoiceChannel, e.GetArg("query"), musicType: MusicType.Soundcloud).ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "listqueue")
                    .Alias(Prefix + "lq")
                    .Description($"Lists 15 currently queued songs per page. Default page is 1. | `{Prefix}lq` or `{Prefix}lq 2`")
                    .Parameter("page", ParameterType.Optional)
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                        {
                            await e.Channel.SendMessage("🎵 No active music player.").ConfigureAwait(false);
                            return;
                        }

                        int page;
                        if (!int.TryParse(e.GetArg("page"), out page) || page <= 0)
                        {
                            page = 1;
                        }

                        var currentSong = musicPlayer.CurrentSong;
                        if (currentSong == null)
                            return;
                        var toSend = $"🎵`Now Playing` {currentSong.PrettyName} " + $"{currentSong.PrettyCurrentTime()}\n";
                        if (musicPlayer.RepeatSong)
                            toSend += "🔂";
                        else if (musicPlayer.RepeatPlaylist)
                            toSend += "🔁";
                        toSend += $" **{musicPlayer.Playlist.Count}** `tracks currently queued. Showing page {page}` ";
                        if (musicPlayer.MaxQueueSize != 0 && musicPlayer.Playlist.Count >= musicPlayer.MaxQueueSize)
                            toSend += "**Song queue is full!**\n";
                        else
                            toSend += "\n";
                        const int itemsPerPage = 15;
                        int startAt = itemsPerPage * (page - 1);
                        var number = 1 + startAt;
                        await e.Channel.SendMessage(toSend + string.Join("\n", musicPlayer.Playlist.Skip(startAt).Take(15).Select(v => $"`{number++}.` {v.PrettyName}"))).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "nowplaying")
                    .Alias(Prefix + "np")
                    .Description($"Shows the song currently playing. | `{Prefix}np`")
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

                cgb.CreateCommand(Prefix + "volume")
                    .Alias(Prefix + "vol")
                    .Description($"Sets the music volume 0-100% | `{Prefix}vol 50`")
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

                cgb.CreateCommand(Prefix + "defvol")
                    .Alias(Prefix + "dv")
                    .Description("Sets the default music volume when music playback is started (0-100)." +
                                 $" Persists through restarts. | `{Prefix}dv 80`")
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
                        var conf = SpecificConfigurations.Default.Of(e.Server.Id);
                        conf.DefaultMusicVolume = volume / 100;
                        await e.Channel.SendMessage($"🎵 `Default volume set to {volume}%`").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "mute")
                    .Alias(Prefix + "min")
                    .Description($"Sets the music volume to 0% | `{Prefix}min`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.SetVolume(0);
                    });

                cgb.CreateCommand(Prefix + "max")
                    .Description($"Sets the music volume to 100%. | `{Prefix}max`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.SetVolume(100);
                    });

                cgb.CreateCommand(Prefix + "half")
                    .Description($"Sets the music volume to 50%. | `{Prefix}half`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        if (e.User.VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                            return;
                        musicPlayer.SetVolume(50);
                    });

                cgb.CreateCommand(Prefix + "shuffle")
                    .Alias(Prefix + "sh")
                    .Description($"Shuffles the current playlist. | `{Prefix}sh`")
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

                cgb.CreateCommand(Prefix + "playlist")
                    .Alias(Prefix + "pl")
                    .Description($"Queues up to 500 songs from a youtube playlist specified by a link, or keywords. | `{Prefix}pl playlist link or name`")
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
                            await e.Channel.SendMessage($"🎵 `Failed to find any songs.`").ConfigureAwait(false);
                            return;
                        }
                        var idArray = ids as string[] ?? ids.ToArray();
                        var count = idArray.Length;
                        var msg =
                            await e.Channel.SendMessage($"🎵 `Attempting to queue {count} songs".SnPl(count) + "...`").ConfigureAwait(false);
                        foreach (var id in idArray)
                        {
                            try
                            {
                                await QueueSong(e.User, e.Channel, e.User.VoiceChannel, id, true).ConfigureAwait(false);
                            }
                            catch (PlaylistFullException)
                            { break; }
                            catch { }
                        }
                        await msg.Edit("🎵 `Playlist queue complete.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "soundcloudpl")
                    .Alias(Prefix + "scpl")
                    .Description($"Queue a soundcloud playlist using a link. | `{Prefix}scpl https://soundcloud.com/saratology/sets/symphony`")
                    .Parameter("pl", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var pl = e.GetArg("pl")?.Trim();

                        if (string.IsNullOrWhiteSpace(pl))
                            return;

                        var scvids = JObject.Parse(await SearchHelper.GetResponseStringAsync($"http://api.soundcloud.com/resolve?url={pl}&client_id={NadekoBot.Creds.SoundCloudClientID}").ConfigureAwait(false))["tracks"].ToObject<SoundCloudVideo[]>();
                        await QueueSong(e.User, e.Channel, e.User.VoiceChannel, scvids[0].TrackLink).ConfigureAwait(false);

                        MusicPlayer mp;
                        if (!MusicPlayers.TryGetValue(e.Server, out mp))
                            return;

                        foreach (var svideo in scvids.Skip(1))
                        {
                            try
                            {
                                mp.AddSong(new Song(new Classes.SongInfo
                                {
                                    Title = svideo.FullName,
                                    Provider = "SoundCloud",
                                    Uri = svideo.StreamLink,
                                    ProviderType = MusicType.Normal,
                                    Query = svideo.TrackLink,
                                }), e.User.Name);
                            }
                            catch (PlaylistFullException) { break; }
                        }
                    });

                cgb.CreateCommand(Prefix + "localplaylst")
                    .Alias(Prefix + "lopl")
                    .Description($"Queues all songs from a directory. **Bot Owner Only!** | `{Prefix}lopl C:/music/classical`")
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
                                try
                                {
                                    await QueueSong(e.User, e.Channel, e.User.VoiceChannel, file.FullName, true, MusicType.Local).ConfigureAwait(false);
                                }
                                catch (PlaylistFullException)
                                {
                                    break;
                                }
                                catch { }
                            }
                            await e.Channel.SendMessage("🎵 `Directory queue complete.`").ConfigureAwait(false);
                        }
                        catch { }
                    });

                cgb.CreateCommand(Prefix + "radio").Alias(Prefix + "ra")
                    .Description($"Queues a radio stream from a link. It can be a direct mp3 radio stream, .m3u, .pls .asx or .xspf (Usage Video: <https://streamable.com/al54>) | `{Prefix}ra radio link here`")
                    .Parameter("radio_link", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.VoiceChannel?.Server != e.Server)
                        {
                            await e.Channel.SendMessage("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.").ConfigureAwait(false);
                            return;
                        }
                        await QueueSong(e.User, e.Channel, e.User.VoiceChannel, e.GetArg("radio_link"), musicType: MusicType.Radio).ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "local")
                    .Alias(Prefix + "lo")
                    .Description($"Queues a local file by specifying a full path. **Bot Owner Only!** | `{Prefix}lo C:/music/mysong.mp3`")
                    .Parameter("path", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        var arg = e.GetArg("path");
                        if (string.IsNullOrWhiteSpace(arg))
                            return;
                        await QueueSong(e.User, e.Channel, e.User.VoiceChannel, e.GetArg("path"), musicType: MusicType.Local).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "move")
                    .Alias(Prefix + "mv")
                    .Description($"Moves the bot to your voice channel. (works only if music is already playing) | `{Prefix}mv`")
                    .Do(e =>
                    {
                        MusicPlayer musicPlayer;
                        var voiceChannel = e.User.VoiceChannel;
                        if (voiceChannel == null || voiceChannel.Server != e.Server || !MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        musicPlayer.MoveToVoiceChannel(voiceChannel);
                    });

                cgb.CreateCommand(Prefix + "remove")
                    .Alias(Prefix + "rm")
                    .Description($"Remove a song by its # in the queue, or 'all' to remove whole queue. | `{Prefix}rm 5`")
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

                //var msRegex = new Regex(@"(?<n1>\d+)>(?<n2>\d+)", RegexOptions.Compiled);
                cgb.CreateCommand(Prefix + "movesong")
                    .Alias(Prefix + "ms")
                    .Description($"Moves a song from one position to another. | `{Prefix} ms` 5>3")
                    .Parameter("fromto")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                        {
                            return;
                        }
                        var fromto = e.GetArg("fromto").Trim();
                        var fromtoArr = fromto.Split('>');

                        int n1;
                        int n2;

                        var playlist = musicPlayer.Playlist as List<Song> ?? musicPlayer.Playlist.ToList();

                        if (fromtoArr.Length != 2 || !int.TryParse(fromtoArr[0], out n1) ||
                            !int.TryParse(fromtoArr[1], out n2) || n1 < 1 || n2 < 1 || n1 == n2 ||
                            n1 > playlist.Count || n2 > playlist.Count)
                        {
                            await e.Channel.SendMessage("`Invalid input.`").ConfigureAwait(false);
                            return;
                        }

                        var s = playlist[n1 - 1];
                        playlist.Insert(n2 - 1, s);
                        var nn1 = n2 < n1 ? n1 : n1 - 1;
                        playlist.RemoveAt(nn1);

                        await e.Channel.SendMessage($"🎵`Moved` {s.PrettyName} `from #{n1} to #{n2}`").ConfigureAwait(false);

                    });

                cgb.CreateCommand(Prefix + "setmaxqueue")
                    .Alias(Prefix + "smq")
                    .Description($"Sets a maximum queue size. Supply 0 or no argument to have no limit.  | `{Prefix}smq` 50 or `{Prefix}smq`")
                    .Parameter("size", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                        {
                            return;
                        }

                        var sizeStr = e.GetArg("size")?.Trim();
                        uint size = 0;
                        if (string.IsNullOrWhiteSpace(sizeStr) || !uint.TryParse(sizeStr, out size))
                        {
                            size = 0;
                        }

                        musicPlayer.MaxQueueSize = size;
                        await e.Channel.SendMessage($"🎵 `Max queue set to {(size == 0 ? ("unlimited") : size + " tracks")}`");
                    });

                cgb.CreateCommand(Prefix + "cleanup")
                    .Description($"Cleans up hanging voice connections. **Bot Owner Only!** | `{Prefix}cleanup`")
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

                cgb.CreateCommand(Prefix + "reptcursong")
                    .Alias(Prefix + "rcs")
                    .Description($"Toggles repeat of current song. | `{Prefix}rcs`")
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

                cgb.CreateCommand(Prefix + "rpeatplaylst")
                    .Alias(Prefix + "rpl")
                    .Description($"Toggles repeat of all songs in the queue (every song that finishes is added to the end of the queue). | `{Prefix}rpl`")
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        var currentValue = musicPlayer.ToggleRepeatPlaylist();
                        await e.Channel.SendMessage($"🎵🔁`Repeat playlist {(currentValue ? "enabled" : "disabled")}`").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "save")
                    .Description($"Saves a playlist under a certain name. Name must be no longer than 20 characters and mustn't contain dashes. | `{Prefix}save classical1`")
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
                        DbHandler.Instance.Connection.InsertAll(songInfos.Select(s => new PlaylistSongInfo
                        {
                            PlaylistId = playlist.Id.Value,
                            SongInfoId = s.Id.Value
                        }), typeof(PlaylistSongInfo));

                        await e.Channel.SendMessage($"🎵 `Saved playlist as {name}-{playlist.Id}`").ConfigureAwait(false);

                    });

                cgb.CreateCommand(Prefix + "load")
                    .Description($"Loads a playlist under a certain name.  | `{Prefix}load classical-1`")
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
                                await QueueSong(e.User, textCh, voiceCh, si.Query, true, (MusicType)si.ProviderType).ConfigureAwait(false);
                            }
                            catch (PlaylistFullException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed QueueSong in load playlist. {ex}");
                            }
                        }
                    });

                cgb.CreateCommand(Prefix + "playlists")
                    .Alias(Prefix + "pls")
                    .Description($"Lists all playlists. Paginated. 20 per page. Default page is 0. |`{Prefix}pls 1`")
                    .Parameter("num", ParameterType.Optional)
                    .Do(e =>
                    {
                        int num = 0;
                        int.TryParse(e.GetArg("num"), out num);
                        if (num < 0)
                            return;
                        var result = DbHandler.Instance.GetPlaylistData(num);
                        if (result.Count == 0)
                            e.Channel.SendMessage($"`No saved playlists found on page {num}`").ConfigureAwait(false);
                        else
                            e.Channel.SendMessage($"```js\n--- List of saved playlists ---\n\n" + string.Join("\n", result.Select(r => $"'{r.Name}-{r.Id}' by {r.Creator} ({r.SongCnt} songs)")) + $"\n\n        --- Page {num} ---```").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "deleteplaylist")
                    .Alias(Prefix + "delpls")
                    .Description($"Deletes a saved playlist. Only if you made it or if you are the bot owner. | `{Prefix}delpls animu-5`")
                    .Parameter("pl", ParameterType.Required)
                    .Do(async e =>
                    {
                        var pl = e.GetArg("pl").Trim().Split('-')[1];
                        if (string.IsNullOrWhiteSpace(pl))
                            return;
                        var plnum = int.Parse(pl);
                        if (NadekoBot.IsOwner(e.User.Id))
                            DbHandler.Instance.Delete<MusicPlaylist>(plnum);
                        else
                            DbHandler.Instance.DeleteWhere<MusicPlaylist>(mp => mp.Id == plnum && (long)e.User.Id == mp.CreatorId);
                        await e.Channel.SendMessage("`Ok.` :ok:").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "goto")
                    .Description($"Goes to a specific time in seconds in a song. | `{Prefix}goto 30`")
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

                cgb.CreateCommand(Prefix + "getlink")
                    .Alias(Prefix + "gl")
                    .Description("Shows a link to the song in the queue by index, or the currently playing song by default.")
                    .Parameter("index", ParameterType.Optional)
                    .Do(async e =>
                    {
                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;
                        int index;
                        string arg = e.GetArg("index")?.Trim();
                        if (!string.IsNullOrEmpty(arg) && int.TryParse(arg, out index))
                        {

                            var selSong = musicPlayer.Playlist.DefaultIfEmpty(null).ElementAtOrDefault(index - 1);
                            if (selSong == null)
                            {
                                await e.Channel.SendMessage("Could not select song, likely wrong index");

                            }
                            else
                            {
                                await e.Channel.SendMessage($"🎶`Selected song {selSong.SongInfo.Title}:` <{selSong.SongInfo.Query}>").ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            var curSong = musicPlayer.CurrentSong;
                            if (curSong == null)
                                return;
                            await e.Channel.SendMessage($"🎶`Current song:` <{curSong.SongInfo.Query}>").ConfigureAwait(false);
                        }

                    });

                cgb.CreateCommand(Prefix + "autoplay")
                    .Alias(Prefix + "ap")
                    .Description("Toggles autoplay - When the song is finished, automatically queue a related youtube song. (Works only for youtube songs and when queue is empty)")
                    .Do(async e =>
                    {

                        MusicPlayer musicPlayer;
                        if (!MusicPlayers.TryGetValue(e.Server, out musicPlayer))
                            return;

                        if (!musicPlayer.ToggleAutoplay())
                            await e.Channel.SendMessage("🎶`Autoplay disabled.`").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("🎶`Autoplay enabled.`").ConfigureAwait(false);
                    });
            });
        }

        public static async Task QueueSong(User queuer, Channel textCh, Channel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
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
                float vol = SpecificConfigurations.Default.Of(server.Id).DefaultMusicVolume;
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
                            if (mp.Autoplay && mp.Playlist.Count == 0 && song.SongInfo.Provider == "YouTube")
                            {
                                await QueueSong(queuer.Server.CurrentUser, textCh, voiceCh, (await SearchHelper.GetRelatedVideoIds(song.SongInfo.Query, 4)).ToList().Shuffle().FirstOrDefault(), silent, musicType).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
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
            Song resolvedSong;
            try
            {
                musicPlayer.ThrowIfQueueFull();
                resolvedSong = await Song.ResolveSong(query, musicType).ConfigureAwait(false);

                musicPlayer.AddSong(resolvedSong, queuer.Name);
            }
            catch (PlaylistFullException)
            {
                await textCh.SendMessage($"🎵 `Queue is full at {musicPlayer.MaxQueueSize}/{musicPlayer.MaxQueueSize}.` ");
                throw;
            }
            if (!silent)
            {
                var queuedMessage = await textCh.SendMessage($"🎵`Queued`{resolvedSong.PrettyName} **at** `#{musicPlayer.Playlist.Count + 1}`").ConfigureAwait(false);
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
