using Discord.Commands;
using NadekoBot.Modules.Music.Classes;
using System.Collections.Concurrent;
using Discord.WebSocket;
using NadekoBot.Services;
using System.IO;
using Discord;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System;
using System.Linq;
using NadekoBot.Extensions;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Music
{
    [NadekoModule("Music", "!!", AutoLoad = false)]
    public partial class Music : DiscordModule
    {
        public static ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public const string MusicDataPath = "data/musicdata";

        public Music() : base()
        {
            //it can fail if its currenctly opened or doesn't exist. Either way i don't care
            try { Directory.Delete(MusicDataPath, true); } catch { }

            Directory.CreateDirectory(MusicDataPath);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Next(IUserMessage umsg, int skipCount = 1)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (skipCount < 1)
                return Task.CompletedTask;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (musicPlayer.PlaybackVoiceChannel == ((IGuildUser)umsg.Author).VoiceChannel)
            {
                while (--skipCount > 0)
                {
                    musicPlayer.RemoveSongAt(0);
                }
                musicPlayer.Next();
            }
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Stop(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (((IGuildUser)umsg.Author).VoiceChannel == musicPlayer.PlaybackVoiceChannel)
            {
                musicPlayer.Autoplay = false;
                musicPlayer.Stop();
            }
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Destroy(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (((IGuildUser)umsg.Author).VoiceChannel == musicPlayer.PlaybackVoiceChannel)
                if(MusicPlayers.TryRemove(channel.Guild.Id, out musicPlayer))
                    musicPlayer.Destroy();
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Pause(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer)) return;
            if (((IGuildUser)umsg.Author).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            musicPlayer.TogglePause();
            if (musicPlayer.Paused)
                await channel.SendMessageAsync("🎵`Music Player paused.`").ConfigureAwait(false);
            else
                await channel.SendMessageAsync("🎵`Music Player unpaused.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Queue(IUserMessage umsg, [Remainder] string query)
        {
            var channel = (ITextChannel)umsg.Channel;

            await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, query).ConfigureAwait(false);
            if (channel.Guild.GetCurrentUser().GetPermissions(channel).ManageMessages)
            {
                await Task.Delay(10000).ConfigureAwait(false);
                await ((IUserMessage)umsg).DeleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SoundCloudQueue(IUserMessage umsg, [Remainder] string query)
        {
            var channel = (ITextChannel)umsg.Channel;

            await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, query, musicType: MusicType.Soundcloud).ConfigureAwait(false);
            if (channel.Guild.GetCurrentUser().GetPermissions(channel).ManageMessages)
            {
                await Task.Delay(10000).ConfigureAwait(false);
                await ((IUserMessage)umsg).DeleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue(IUserMessage umsg, int page = 1)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
            {
                await channel.SendMessageAsync("🎵 No active music player.").ConfigureAwait(false);
                return;
            }
            if (page <= 0)
                return;

            var currentSong = musicPlayer.CurrentSong;
            if (currentSong == null)
                return;

            if (currentSong.TotalLength == TimeSpan.Zero)
            {
                await musicPlayer.UpdateSongDurationsAsync().ConfigureAwait(false);
            }

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
            await channel.SendMessageAsync(toSend + string.Join("\n", musicPlayer.Playlist.Skip(startAt).Take(15).Select(v => $"`{number++}.` {v.PrettyName}"))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlaying(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            var currentSong = musicPlayer.CurrentSong;
            if (currentSong == null)
                return;

            if (currentSong.TotalLength == TimeSpan.Zero)
            {
                await musicPlayer.UpdateSongDurationsAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"🎵`Now Playing` {currentSong.PrettyName} " +
                                        $"{currentSong.PrettyCurrentTime()}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(IUserMessage umsg, int val)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            if (((IGuildUser)umsg.Author).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            if (val < 0)
                return;
            var volume = musicPlayer.SetVolume(val);
            await channel.SendMessageAsync($"🎵 `Volume set to {volume}%`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Defvol(IUserMessage umsg, [Remainder] int val)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (val < 0 || val > 100)
            {
                await channel.SendMessageAsync("Volume number invalid. Must be between 0 and 100").ConfigureAwait(false);
                return;
            }
            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id, set => set).DefaultMusicVolume = val / 100.0f;
                uow.Complete();
            }
            await channel.SendMessageAsync($"🎵 `Default volume set to {val}%`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ShufflePlaylist(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            if (((IGuildUser)umsg.Author).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            if (musicPlayer.Playlist.Count < 2)
            {
                await channel.SendMessageAsync("💢 Not enough songs in order to perform the shuffle.").ConfigureAwait(false);
                return;
            }

            musicPlayer.Shuffle();
            await channel.SendMessageAsync("🎵 `Songs shuffled.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Playlist(IUserMessage umsg, [Remainder] string playlist)
        {
            var channel = (ITextChannel)umsg.Channel;
            var arg = playlist;
            if (string.IsNullOrWhiteSpace(arg))
                return;
            if (((IGuildUser)umsg.Author).VoiceChannel?.Guild != channel.Guild)
            {
                await channel.SendMessageAsync("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.").ConfigureAwait(false);
                return;
            }
            var plId = (await NadekoBot.Google.GetPlaylistIdsByKeywordsAsync(arg).ConfigureAwait(false)).FirstOrDefault();
            if (plId == null)
            {
                await channel.SendMessageAsync("No search results for that query.");
                return;
            }
            var ids = await NadekoBot.Google.GetPlaylistTracksAsync(plId, 500).ConfigureAwait(false);
            if (!ids.Any())
            {
                await channel.SendMessageAsync($"🎵 `Failed to find any songs.`").ConfigureAwait(false);
                return;
            }
            var idArray = ids as string[] ?? ids.ToArray();
            var count = idArray.Length;
            var msg =
                await channel.SendMessageAsync($"🎵 `Attempting to queue {count} songs".SnPl(count) + "...`").ConfigureAwait(false);
            foreach (var id in idArray)
            {
                try
                {
                    await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, id, true).ConfigureAwait(false);
                }
                catch (SongNotFoundException) { }
                catch { break; }
            }
            await msg.ModifyAsync(m => m.Content = "🎵 `Playlist queue complete.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SoundCloudPl(IUserMessage umsg, [Remainder] string pl)
        {
            var channel = (ITextChannel)umsg.Channel;
            pl = pl?.Trim();

            if (string.IsNullOrWhiteSpace(pl))
                return;

            using (var http = new HttpClient())
            {
                var scvids = JObject.Parse(await http.GetStringAsync($"http://api.soundcloud.com/resolve?url={pl}&client_id={NadekoBot.Credentials.SoundCloudClientId}").ConfigureAwait(false))["tracks"].ToObject<SoundCloudVideo[]>();
                await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, scvids[0].TrackLink).ConfigureAwait(false);

                MusicPlayer mp;
                if (!MusicPlayers.TryGetValue(channel.Guild.Id, out mp))
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
                        }), ((IGuildUser)umsg.Author).Username);
                    }
                    catch (PlaylistFullException) { break; }
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task LocalPl(IUserMessage umsg, [Remainder] string directory)
        {
            var channel = (ITextChannel)umsg.Channel;
            var arg = directory;
            if (string.IsNullOrWhiteSpace(arg))
                return;
            try
            {
                var dir = new DirectoryInfo(arg);
                var fileEnum = dir.GetFiles("*", SearchOption.AllDirectories)
                                    .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System));
                foreach (var file in fileEnum)
                {
                    try
                    {
                        await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, file.FullName, true, MusicType.Local).ConfigureAwait(false);
                    }
                    catch (PlaylistFullException)
                    {
                        break;
                    }
                    catch { }
                }
                await channel.SendMessageAsync("🎵 `Directory queue complete.`").ConfigureAwait(false);
            }
            catch { }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Radio(IUserMessage umsg, string radio_link)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (((IGuildUser)umsg.Author).VoiceChannel?.Guild != channel.Guild)
            {
                await channel.SendMessageAsync("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining it.").ConfigureAwait(false);
                return;
            }
            await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, radio_link, musicType: MusicType.Radio).ConfigureAwait(false);
            if (channel.Guild.GetCurrentUser().GetPermissions(channel).ManageMessages)
            {
                await Task.Delay(10000).ConfigureAwait(false);
                await ((IUserMessage)umsg).DeleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Local(IUserMessage umsg, [Remainder] string path)
        {
            var channel = (ITextChannel)umsg.Channel;
            var arg = path;
            if (string.IsNullOrWhiteSpace(arg))
                return;
            await QueueSong(((IGuildUser)umsg.Author), channel, ((IGuildUser)umsg.Author).VoiceChannel, path, musicType: MusicType.Local).ConfigureAwait(false);

        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Move(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            var voiceChannel = ((IGuildUser)umsg.Author).VoiceChannel;
            if (voiceChannel == null || voiceChannel.Guild != channel.Guild || !MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            await musicPlayer.MoveToVoiceChannel(voiceChannel);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Remove(IUserMessage umsg, int num)
        {
            var channel = (ITextChannel)umsg.Channel;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
            {
                return;
            }
            if (((IGuildUser)umsg.Author).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            if (num <= 0 || num > musicPlayer.Playlist.Count)
                return;
            var song = (musicPlayer.Playlist as List<Song>)?[num - 1];
            musicPlayer.RemoveSongAt(num - 1);
            await channel.SendMessageAsync($"🎵**Track {song.PrettyName} at position `#{num}` has been removed.**").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Remove(IUserMessage umsg, string all)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (all.Trim().ToUpperInvariant() != "ALL")
                return;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer)) return;
            musicPlayer.ClearQueue();
            await channel.SendMessageAsync($"🎵`Queue cleared!`").ConfigureAwait(false);
            return;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MoveSong(IUserMessage umsg, [Remainder] string fromto)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
            {
                return;
            }
            fromto = fromto?.Trim();
            var fromtoArr = fromto.Split('>');

            int n1;
            int n2;

            var playlist = musicPlayer.Playlist as List<Song> ?? musicPlayer.Playlist.ToList();

            if (fromtoArr.Length != 2 || !int.TryParse(fromtoArr[0], out n1) ||
                !int.TryParse(fromtoArr[1], out n2) || n1 < 1 || n2 < 1 || n1 == n2 ||
                n1 > playlist.Count || n2 > playlist.Count)
            {
                await channel.SendMessageAsync("`Invalid input.`").ConfigureAwait(false);
                return;
            }

            var s = playlist[n1 - 1];
            playlist.Insert(n2 - 1, s);
            var nn1 = n2 < n1 ? n1 : n1 - 1;
            playlist.RemoveAt(nn1);

            await channel.SendMessageAsync($"🎵`Moved` {s.PrettyName} `from #{n1} to #{n2}`").ConfigureAwait(false);


        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SetMaxQueue(IUserMessage umsg, uint size)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
            {
                return;
            }
            musicPlayer.MaxQueueSize = size;
            await channel.SendMessageAsync($"🎵 `Max queue set to {(size == 0 ? ("unlimited") : size + " tracks")}`");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ReptCurSong(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            var currentSong = musicPlayer.CurrentSong;
            if (currentSong == null)
                return;
            var currentValue = musicPlayer.ToggleRepeatSong();
            await channel.SendMessageAsync(currentValue ?
                                        $"🎵🔂`Repeating track:`{currentSong.PrettyName}" :
                                        $"🎵🔂`Current track repeat stopped.`")
                                            .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RepeatPl(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            var currentValue = musicPlayer.ToggleRepeatPlaylist();
            await channel.SendMessageAsync($"🎵🔁`Repeat playlist {(currentValue ? "enabled" : "disabled")}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Save(IUserMessage umsg, [Remainder] string name)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;

            var curSong = musicPlayer.CurrentSong;
            var songs = musicPlayer.Playlist.Append(curSong)
                                .Select(s=> new PlaylistSong() {
                                    Provider = s.SongInfo.Provider,
                                    ProviderType = s.SongInfo.ProviderType,
                                    Title = s.SongInfo.Title,
                                    Uri = s.SongInfo.Uri,
                                    Query = s.SongInfo.Query,
                                }).ToList();

            MusicPlaylist playlist;
            using (var uow = DbHandler.UnitOfWork())
            {
                playlist = new MusicPlaylist
                {
                    Name = name,
                    Author = umsg.Author.Username,
                    AuthorId = umsg.Author.Id,
                    Songs = songs,
                };
                uow.MusicPlaylists.Add(playlist);
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            await channel.SendMessageAsync(($"🎵 `Saved playlist as {name}.` `Id: {playlist.Id}`")).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Load(IUserMessage umsg, [Remainder] int id)
        {
            var channel = (ITextChannel)umsg.Channel;

            MusicPlaylist mpl;
            using (var uow = DbHandler.UnitOfWork())
            {
                mpl = uow.MusicPlaylists.GetWithSongs(id);
            }

            if (mpl == null)
            {
                await channel.SendMessageAsync("`Can't find playlist with that ID`").ConfigureAwait(false);
                return;
            }
            IUserMessage msg = null;
            try { msg = await channel.SendMessageAsync($"`Attempting to load {mpl.Songs.Count} songs...`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
            foreach (var item in mpl.Songs)
            {
                var usr = (IGuildUser)umsg.Author;
                try
                {
                    await QueueSong(usr, channel, usr.VoiceChannel, item.Query, true, item.ProviderType).ConfigureAwait(false);
                }
                catch (SongNotFoundException) { }
                catch { break; }
            }
            if (msg != null)
                await msg.ModifyAsync(m => m.Content = $"`Done loading playlist {mpl.Name}.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Playlists(IUserMessage umsg, [Remainder] int num = 1)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (num <= 0)
                return;

            List<MusicPlaylist> playlists;

            using (var uow = DbHandler.UnitOfWork())
            {
                playlists = uow.MusicPlaylists.GetPlaylistsOnPage(num);
            }

            await channel.SendMessageAsync($@"`Page {num} of saved playlists`

" + string.Join("\n", playlists.Select(r => $"`#{r.Id}` - `{r.Name}` by {r.Author} - **{r.Songs.Count}** songs"))).ConfigureAwait(false);
        }

        //todo only author or owner
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task DeletePlaylist(IUserMessage umsg, [Remainder] int id)
        {
            var channel = (ITextChannel)umsg.Channel;

            bool success = false;
            MusicPlaylist pl = null;
            try
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    pl = uow.MusicPlaylists.Get(id);

                    if (pl != null)
                    {
                        if (NadekoBot.Credentials.IsOwner(umsg.Author) || pl.AuthorId == umsg.Author.Id)
                        {
                            uow.MusicPlaylists.Remove(pl);
                            await uow.CompleteAsync().ConfigureAwait(false);
                            success = true;
                        }
                        else
                            success = false;
                    }
                }

                if (!success)
                    await channel.SendMessageAsync("Failed to delete that playlist. It either doesn't exist, or you are not its author.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("`Playlist successfully deleted.`").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Goto(IUserMessage umsg, int time)
        {
            var channel = (ITextChannel)umsg.Channel;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            if (((IGuildUser)umsg.Author).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;

            if (time < 0)
                return;

            var currentSong = musicPlayer.CurrentSong;

            if (currentSong == null)
                return;

            //currentSong.PrintStatusMessage = false;
            var gotoSong = currentSong.Clone();
            gotoSong.SkipTo = time;
            musicPlayer.AddSong(gotoSong, 0);
            musicPlayer.Next();

            var minutes = (time / 60).ToString();
            var seconds = (time % 60).ToString();

            if (minutes.Length == 1)
                minutes = "0" + minutes;
            if (seconds.Length == 1)
                seconds = "0" + seconds;

            await channel.SendMessageAsync($"`Skipped to {minutes}:{seconds}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task GetLink(IUserMessage umsg, int index = 0)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;

            if (index < 0)
                return;

            if (index > 0)
            {

                var selSong = musicPlayer.Playlist.DefaultIfEmpty(null).ElementAtOrDefault(index - 1);
                if (selSong == null)
                {
                    await channel.SendMessageAsync("Could not select song, likely wrong index");

                }
                else
                {
                    await channel.SendMessageAsync($"🎶`Selected song {selSong.SongInfo.Title}:` <{selSong.SongInfo.Query}>").ConfigureAwait(false);
                }
            }
            else
            {
                var curSong = musicPlayer.CurrentSong;
                if (curSong == null)
                    return;
                await channel.SendMessageAsync($"🎶`Current song:` <{curSong.SongInfo.Query}>").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Autoplay(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;

            if (!musicPlayer.ToggleAutoplay())
                await channel.SendMessageAsync("🎶`Autoplay disabled.`").ConfigureAwait(false);
            else
                await channel.SendMessageAsync("🎶`Autoplay enabled.`").ConfigureAwait(false);
        }

        public static async Task QueueSong(IGuildUser queuer, ITextChannel textCh, IVoiceChannel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
        {
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (!silent)
                    await textCh.SendMessageAsync("💢 You need to be in a voice channel on this server.\n If you are already in a voice channel, try rejoining.").ConfigureAwait(false);
                throw new ArgumentNullException(nameof(voiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("💢 Invalid query for queue song.", nameof(query));

            var musicPlayer = MusicPlayers.GetOrAdd(textCh.Guild.Id, server =>
            {
                float vol = 1;// SpecificConfigurations.Default.Of(server.Id).DefaultMusicVolume;
                using (var uow = DbHandler.UnitOfWork())
                {
                    vol = uow.GuildConfigs.For(textCh.Guild.Id, set => set).DefaultMusicVolume;
                }
                var mp = new MusicPlayer(voiceCh, vol);


                IUserMessage playingMessage = null;
                IUserMessage lastFinishedMessage = null;
                mp.OnCompleted += async (s, song) =>
                {
                    if (song.PrintStatusMessage)
                    {
                        try
                        {
                            if (lastFinishedMessage != null)
                                await lastFinishedMessage.DeleteAsync().ConfigureAwait(false);
                            if (playingMessage != null)
                                await playingMessage.DeleteAsync().ConfigureAwait(false);
                            try { lastFinishedMessage = await textCh.SendMessageAsync($"🎵`Finished`{song.PrettyName}").ConfigureAwait(false); } catch { }
                            if (mp.Autoplay && mp.Playlist.Count == 0 && song.SongInfo.Provider == "YouTube")
                            {
                                await QueueSong(queuer.Guild.GetCurrentUser(), textCh, voiceCh, (await NadekoBot.Google.GetRelatedVideosAsync(song.SongInfo.Query, 4)).ToList().Shuffle().FirstOrDefault(), silent, musicType).ConfigureAwait(false);
                            }
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

                            var msgTxt = $"🎵`Playing`{song.PrettyName} `Vol: {(int)(sender.Volume * 100)}%`";
                        try { playingMessage = await textCh.SendMessageAsync(msgTxt).ConfigureAwait(false); } catch { }
                    }
                };
                return mp;
            });
            Song resolvedSong;
            try
            {
                musicPlayer.ThrowIfQueueFull();
                resolvedSong = await Song.ResolveSong(query, musicType).ConfigureAwait(false);

                if (resolvedSong == null)
                    throw new SongNotFoundException();

                musicPlayer.AddSong(resolvedSong, queuer.Username);
            }
            catch (PlaylistFullException)
            {
                try { await textCh.SendMessageAsync($"🎵 `Queue is full at {musicPlayer.MaxQueueSize}/{musicPlayer.MaxQueueSize}.` "); } catch { }
                throw;
            }
            if (!silent)
            {
                try
                {
                    var queuedMessage = await textCh.SendMessageAsync($"🎵`Queued`{resolvedSong.PrettyName} **at** `#{musicPlayer.Playlist.Count + 1}`").ConfigureAwait(false);
                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                        
                            await queuedMessage.DeleteAsync().ConfigureAwait(false);
                        }
                        catch { }
                    }).ConfigureAwait(false);
                }
                catch { } // if queued message sending fails, don't attempt to delete it
            }
        }
    }
}