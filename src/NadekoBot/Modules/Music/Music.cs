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
using System.Threading;

namespace NadekoBot.Modules.Music
{
    [NadekoModule("Music", "!!")]
    [DontAutoLoad]
    public class Music : NadekoTopLevelModule
    {
        public static ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public const string MusicDataPath = "data/musicdata";

        static Music()
        {
            //it can fail if its currenctly opened or doesn't exist. Either way i don't care
            try { Directory.Delete(MusicDataPath, true); } catch { }

            NadekoBot.Client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

            Directory.CreateDirectory(MusicDataPath);
        }

        private static Task Client_UserVoiceStateUpdated(SocketUser iusr, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var usr = iusr as SocketGuildUser;
            if (usr == null ||
                oldState.VoiceChannel == newState.VoiceChannel)
                return Task.CompletedTask;

            MusicPlayer player;
            if (!MusicPlayers.TryGetValue(usr.Guild.Id, out player))
                return Task.CompletedTask;

            try
            {


                //if bot moved
                if ((player.PlaybackVoiceChannel == oldState.VoiceChannel) &&
                        usr.Id == NadekoBot.Client.CurrentUser.Id)
                {
                    if (player.Paused && newState.VoiceChannel.Users.Count > 1) //unpause if there are people in the new channel
                        player.TogglePause();
                    else if (!player.Paused && newState.VoiceChannel.Users.Count <= 1) // pause if there are no users in the new channel
                        player.TogglePause();

                    return Task.CompletedTask;
                }


                //if some other user moved
                if ((player.PlaybackVoiceChannel == newState.VoiceChannel && //if joined first, and player paused, unpause 
                        player.Paused &&
                        newState.VoiceChannel.Users.Count == 2) ||  // keep in mind bot is in the channel (+1)
                    (player.PlaybackVoiceChannel == oldState.VoiceChannel && // if left last, and player unpaused, pause
                        !player.Paused &&
                        oldState.VoiceChannel.Users.Count == 1))
                {
                    player.TogglePause();
                    return Task.CompletedTask;
                }

            }
            catch
            {
                // ignored
            }
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Next(int skipCount = 1)
        {
            if (skipCount < 1)
                return Task.CompletedTask;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (musicPlayer.PlaybackVoiceChannel == ((IGuildUser)Context.User).VoiceChannel)
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
        public Task Stop()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (((IGuildUser)Context.User).VoiceChannel == musicPlayer.PlaybackVoiceChannel)
            {
                musicPlayer.Autoplay = false;
                musicPlayer.Stop();
            }
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Destroy()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (((IGuildUser)Context.User).VoiceChannel == musicPlayer.PlaybackVoiceChannel)
                if (MusicPlayers.TryRemove(Context.Guild.Id, out musicPlayer))
                    musicPlayer.Destroy();

            return Task.CompletedTask;

        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Pause()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer)) return Task.CompletedTask;
            if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return Task.CompletedTask;
            musicPlayer.TogglePause();
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Fairplay()
        {
            var channel = (ITextChannel)Context.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer)) return;
            if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            var val = musicPlayer.FairPlay = !musicPlayer.FairPlay;

            if (val)
            {
                await ReplyConfirmLocalized("fp_enabled").ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("fp_disabled").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Queue([Remainder] string query)
        {
            await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, query).ConfigureAwait(false);
            if ((await Context.Guild.GetCurrentUserAsync()).GetPermissions((IGuildChannel)Context.Channel).ManageMessages)
            {
                Context.Message.DeleteAfter(10);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SoundCloudQueue([Remainder] string query)
        {
            await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, query, musicType: MusicType.Soundcloud).ConfigureAwait(false);
            if ((await Context.Guild.GetCurrentUserAsync()).GetPermissions((IGuildChannel)Context.Channel).ManageMessages)
            {
                Context.Message.DeleteAfter(10);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue(int page = 1)
        {
            Song currentSong;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer) ||
                (currentSong = musicPlayer?.CurrentSong) == null)
            {
                await ReplyErrorLocalized("no_player").ConfigureAwait(false);
                return;
            }
            if (page <= 0)
                return;

            try { await musicPlayer.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            const int itemsPerPage = 10;

            var total = musicPlayer.TotalPlaytime;
            var totalStr = total == TimeSpan.MaxValue ? "∞" : GetText("time_format", 
                (int) total.TotalHours, 
                total.Minutes, 
                total.Seconds);
            var maxPlaytime = musicPlayer.MaxPlaytimeSeconds;
            var lastPage = musicPlayer.Playlist.Count / itemsPerPage;
            Func<int, EmbedBuilder> printAction = curPage =>
            {
                var startAt = itemsPerPage * (curPage - 1);
                var number = 0 + startAt;
                var desc = string.Join("\n", musicPlayer.Playlist
                        .Skip(startAt)
                        .Take(itemsPerPage)
                        .Select(v => $"`{++number}.` {v.PrettyFullName}"));
                
                desc = $"`🔊` {currentSong.PrettyFullName}\n\n" + desc;

                if (musicPlayer.RepeatSong)
                    desc = "🔂 " + GetText("repeating_cur_song") +"\n\n" + desc;
                else if (musicPlayer.RepeatPlaylist)
                    desc = "🔁 " + GetText("repeating_playlist")+"\n\n" + desc;



                var embed = new EmbedBuilder()
                    .WithAuthor(eab => eab.WithName(GetText("player_queue", curPage, lastPage + 1))
                        .WithMusicIcon())
                    .WithDescription(desc)
                    .WithFooter(ef => ef.WithText($"{musicPlayer.PrettyVolume} | {musicPlayer.Playlist.Count} " +
                                                  $"{("tracks".SnPl(musicPlayer.Playlist.Count))} | {totalStr} | " +
                                                  (musicPlayer.FairPlay
                                                      ? "✔️" + GetText("fairplay")
                                                      : "✖️" + GetText("fairplay")) + " | " +
                                                  (maxPlaytime == 0 ? "unlimited" : GetText("play_limit", maxPlaytime))))
                    .WithOkColor();

                return embed;
            };
            await Context.Channel.SendPaginatedConfirmAsync(page, printAction, lastPage, false).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlaying()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            var currentSong = musicPlayer.CurrentSong;
            if (currentSong == null)
                return;
            try { await musicPlayer.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            var embed = new EmbedBuilder().WithOkColor()
                            .WithAuthor(eab => eab.WithName(GetText("now_playing")).WithMusicIcon())
                            .WithDescription(currentSong.PrettyName)
                            .WithThumbnailUrl(currentSong.Thumbnail)
                            .WithFooter(ef => ef.WithText(musicPlayer.PrettyVolume + " | " + currentSong.PrettyFullTime + $" | {currentSong.PrettyProvider} | {currentSong.QueuerName}"));

            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(int val)
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            if (val < 0 || val > 100)
            {
                await ReplyErrorLocalized("volume_input_invalid").ConfigureAwait(false);
                return;
            }
            var volume = musicPlayer.SetVolume(val);
            await ReplyConfirmLocalized("volume_set", volume).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Defvol([Remainder] int val)
        {
            if (val < 0 || val > 100)
            {
                await ReplyErrorLocalized("volume_input_invalid").ConfigureAwait(false);
                return;
            }
            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(Context.Guild.Id, set => set).DefaultMusicVolume = val / 100.0f;
                uow.Complete();
            }
            await ReplyConfirmLocalized("defvol_set", val).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ShufflePlaylist()
        {

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return;
            if (musicPlayer.Playlist.Count < 2)
                return;

            musicPlayer.Shuffle();
            await ReplyConfirmLocalized("songs_shuffled").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Playlist([Remainder] string playlist)
        {

            var arg = playlist;
            if (string.IsNullOrWhiteSpace(arg))
                return;
            if (((IGuildUser)Context.User).VoiceChannel?.Guild != Context.Guild)
            {
                await ReplyErrorLocalized("must_be_in_voice").ConfigureAwait(false);
                return;
            }
            var plId = (await NadekoBot.Google.GetPlaylistIdsByKeywordsAsync(arg).ConfigureAwait(false)).FirstOrDefault();
            if (plId == null)
            {
                await ReplyErrorLocalized("no_search_results").ConfigureAwait(false);
                return;
            }
            var ids = await NadekoBot.Google.GetPlaylistTracksAsync(plId, 500).ConfigureAwait(false);
            if (!ids.Any())
            {
                await ReplyErrorLocalized("no_search_results").ConfigureAwait(false);
                return;
            }
            var count = ids.Count();
            var msg = await Context.Channel.SendMessageAsync("🎵 " + GetText("attempting_to_queue",
                Format.Bold(count.ToString()))).ConfigureAwait(false);

            var cancelSource = new CancellationTokenSource();

            var gusr = (IGuildUser)Context.User;
            //todo use grouping
            while (ids.Any() && !cancelSource.IsCancellationRequested)
            {
                var tasks = Task.WhenAll(ids.Take(5).Select(async id =>
                {
                    if (cancelSource.Token.IsCancellationRequested)
                        return;
                    try
                    {
                        await QueueSong(gusr, (ITextChannel)Context.Channel, gusr.VoiceChannel, id, true).ConfigureAwait(false);
                    }
                    catch (SongNotFoundException) { }
                    catch { try { cancelSource.Cancel(); } catch { } }
                }));

                await Task.WhenAny(tasks, Task.Delay(Timeout.Infinite, cancelSource.Token));
                ids = ids.Skip(5);
            }

            await msg.ModifyAsync(m => m.Content = "✅ " + Format.Bold(GetText("playlist_queue_complete"))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SoundCloudPl([Remainder] string pl)
        {

            pl = pl?.Trim();

            if (string.IsNullOrWhiteSpace(pl))
                return;

            using (var http = new HttpClient())
            {
                var scvids = JObject.Parse(await http.GetStringAsync($"http://api.soundcloud.com/resolve?url={pl}&client_id={NadekoBot.Credentials.SoundCloudClientId}").ConfigureAwait(false))["tracks"].ToObject<SoundCloudVideo[]>();
                await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, scvids[0].TrackLink).ConfigureAwait(false);

                MusicPlayer mp;
                if (!MusicPlayers.TryGetValue(Context.Guild.Id, out mp))
                    return;

                foreach (var svideo in scvids.Skip(1))
                {
                    try
                    {
                        mp.AddSong(new Song(new SongInfo
                        {
                            Title = svideo.FullName,
                            Provider = "SoundCloud",
                            Uri = svideo.StreamLink,
                            ProviderType = MusicType.Normal,
                            Query = svideo.TrackLink,
                        }), ((IGuildUser)Context.User).Username);
                    }
                    catch (PlaylistFullException) { break; }
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task LocalPl([Remainder] string directory)
        {

            var arg = directory;
            if (string.IsNullOrWhiteSpace(arg))
                return;
            var dir = new DirectoryInfo(arg);
            var fileEnum = dir.GetFiles("*", SearchOption.AllDirectories)
                                .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System));
            var gusr = (IGuildUser)Context.User;
            foreach (var file in fileEnum)
            {
                try
                {
                    await QueueSong(gusr, (ITextChannel)Context.Channel, gusr.VoiceChannel, file.FullName, true, MusicType.Local).ConfigureAwait(false);
                }
                catch (PlaylistFullException)
                {
                    break;
                }
                catch
                {
                    // ignored
                }
            }
            await ReplyConfirmLocalized("dir_queue_complete").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Radio(string radioLink)
        {

            if (((IGuildUser)Context.User).VoiceChannel?.Guild != Context.Guild)
            {
                await ReplyErrorLocalized("must_be_in_voice").ConfigureAwait(false);
                return;
            }
            await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, radioLink, musicType: MusicType.Radio).ConfigureAwait(false);
            if ((await Context.Guild.GetCurrentUserAsync()).GetPermissions((IGuildChannel)Context.Channel).ManageMessages)
            {
                Context.Message.DeleteAfter(10);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Local([Remainder] string path)
        {

            var arg = path;
            if (string.IsNullOrWhiteSpace(arg))
                return;
            await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, path, musicType: MusicType.Local).ConfigureAwait(false);

        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task Move()
        //{

        //    MusicPlayer musicPlayer;
        //    var voiceChannel = ((IGuildUser)Context.User).VoiceChannel;
        //    if (voiceChannel == null || voiceChannel.Guild != Context.Guild || !MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
        //        return;
        //    await musicPlayer.MoveToVoiceChannel(voiceChannel);
        //}

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public Task Remove(int num)
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return Task.CompletedTask;
            if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
                return Task.CompletedTask;

            musicPlayer.RemoveSongAt(num - 1);
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Remove(string all)
        {
            if (all.Trim().ToUpperInvariant() != "ALL")
                return;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer)) return;
            musicPlayer.ClearQueue();
            await ReplyConfirmLocalized("queue_cleared").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MoveSong([Remainder] string fromto)
        {
            if (string.IsNullOrWhiteSpace(fromto))
                return;

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            
            fromto = fromto?.Trim();
            var fromtoArr = fromto.Split('>');

            int n1;
            int n2;

            var playlist = musicPlayer.Playlist as List<Song> ?? musicPlayer.Playlist.ToList();

            if (fromtoArr.Length != 2 || !int.TryParse(fromtoArr[0], out n1) ||
                !int.TryParse(fromtoArr[1], out n2) || n1 < 1 || n2 < 1 || n1 == n2 ||
                n1 > playlist.Count || n2 > playlist.Count)
            {
                await ReplyConfirmLocalized("invalid_input").ConfigureAwait(false);
                return;
            }

            var s = playlist[n1 - 1];
            playlist.Insert(n2 - 1, s);
            var nn1 = n2 < n1 ? n1 : n1 - 1;
            playlist.RemoveAt(nn1);

            var embed = new EmbedBuilder()
                .WithTitle($"{s.SongInfo.Title.TrimTo(70)}")
                .WithUrl(s.SongUrl)
            .WithAuthor(eab => eab.WithName(GetText("song_moved")).WithIconUrl("https://cdn.discordapp.com/attachments/155726317222887425/258605269972549642/music1.png"))
            .AddField(fb => fb.WithName(GetText("from_position")).WithValue($"#{n1}").WithIsInline(true))
            .AddField(fb => fb.WithName(GetText("to_position")).WithValue($"#{n2}").WithIsInline(true))
            .WithColor(NadekoBot.OkColor);
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);

            //await channel.SendConfirmAsync($"🎵Moved {s.PrettyName} `from #{n1} to #{n2}`").ConfigureAwait(false);


        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SetMaxQueue(uint size = 0)
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;

            musicPlayer.MaxQueueSize = size;

            if(size == 0)
                await ReplyConfirmLocalized("max_queue_unlimited").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("max_queue_x", size).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SetMaxPlaytime(uint seconds)
        {
            if (seconds < 15 && seconds != 0)
                return;

            var channel = (ITextChannel)Context.Channel;
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(channel.Guild.Id, out musicPlayer))
                return;
            musicPlayer.MaxPlaytimeSeconds = seconds;
            if (seconds == 0)
                await ReplyConfirmLocalized("max_playtime_none").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("max_playtime_set", seconds).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ReptCurSong()
        {

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            var currentSong = musicPlayer.CurrentSong;
            if (currentSong == null)
                return;
            var currentValue = musicPlayer.ToggleRepeatSong();

            if (currentValue)
                await Context.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithMusicIcon().WithName("🔂 " + GetText("repeating_track")))
                    .WithDescription(currentSong.PrettyName)
                    .WithFooter(ef => ef.WithText(currentSong.PrettyInfo))).ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync("🔂 "  + GetText("repeating_track_stopped"))
                                            .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RepeatPl()
        {

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            var currentValue = musicPlayer.ToggleRepeatPlaylist();
            if(currentValue)
                await ReplyConfirmLocalized("rpl_enabled").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("rpl_disabled").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Save([Remainder] string name)
        {

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;

            var curSong = musicPlayer.CurrentSong;
            var songs = musicPlayer.Playlist.Append(curSong)
                                .Select(s => new PlaylistSong()
                                {
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
                    Author = Context.User.Username,
                    AuthorId = Context.User.Id,
                    Songs = songs,
                };
                uow.MusicPlaylists.Add(playlist);
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("playlist_saved"))
                .AddField(efb => efb.WithName(GetText("name")).WithValue(name))
                .AddField(efb => efb.WithName(GetText("id")).WithValue(playlist.Id.ToString())));
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Load([Remainder] int id)
        {
            MusicPlaylist mpl;
            using (var uow = DbHandler.UnitOfWork())
            {
                mpl = uow.MusicPlaylists.GetWithSongs(id);
            }

            if (mpl == null)
            {
                await ReplyErrorLocalized("playlist_id_not_found").ConfigureAwait(false);
                return;
            }
            IUserMessage msg = null;
            try { msg = await Context.Channel.SendMessageAsync(GetText("attempting_to_queue", Format.Bold(mpl.Songs.Count.ToString()))).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
            foreach (var item in mpl.Songs)
            {
                var usr = (IGuildUser)Context.User;
                try
                {
                    await QueueSong(usr, (ITextChannel)Context.Channel, usr.VoiceChannel, item.Query, true, item.ProviderType).ConfigureAwait(false);
                }
                catch (SongNotFoundException) { }
                catch { break; }
            }
            if (msg != null)
                await msg.ModifyAsync(m => m.Content = GetText("playlist_queue_complete")).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Playlists([Remainder] int num = 1)
        {
            if (num <= 0)
                return;

            List<MusicPlaylist> playlists;

            using (var uow = DbHandler.UnitOfWork())
            {
                playlists = uow.MusicPlaylists.GetPlaylistsOnPage(num);
            }

            var embed = new EmbedBuilder()
                .WithAuthor(eab => eab.WithName(GetText("playlists_page", num)).WithMusicIcon())
                .WithDescription(string.Join("\n", playlists.Select(r =>
                    GetText("playlists", "#" + r.Id, r.Name, r.Author, r.Songs.Count))))
                .WithOkColor();
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);

        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task DeletePlaylist([Remainder] int id)
        {
            var success = false;
            try
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var pl = uow.MusicPlaylists.Get(id);

                    if (pl != null)
                    {
                        if (NadekoBot.Credentials.IsOwner(Context.User) || pl.AuthorId == Context.User.Id)
                        {
                            uow.MusicPlaylists.Remove(pl);
                            await uow.CompleteAsync().ConfigureAwait(false);
                            success = true;
                        }
                    }
                }

                if (!success)
                    await ReplyErrorLocalized("playlist_delete_fail").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("playlist_deleted").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Goto(int time)
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;
            if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
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

            await ReplyConfirmLocalized("skipped_to", minutes, seconds).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Autoplay()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;

            if (!musicPlayer.ToggleAutoplay())
                await ReplyConfirmLocalized("autoplay_disabled").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("autoplay_enabled").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetMusicChannel()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
            {
                await ReplyErrorLocalized("no_player").ConfigureAwait(false);
                return;
            }

            musicPlayer.OutputTextChannel = (ITextChannel)Context.Channel;

            await ReplyConfirmLocalized("set_music_channel").ConfigureAwait(false);
        }

        public async Task QueueSong(IGuildUser queuer, ITextChannel textCh, IVoiceChannel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
        {
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (!silent)
                    await textCh.SendErrorAsync(GetText("must_be_in_voice")).ConfigureAwait(false);
                throw new ArgumentNullException(nameof(voiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("Invalid song query.", nameof(query));

            var musicPlayer = MusicPlayers.GetOrAdd(textCh.Guild.Id, server =>
            {
                float vol;// SpecificConfigurations.Default.Of(server.Id).DefaultMusicVolume;
                using (var uow = DbHandler.UnitOfWork())
                {
                    vol = uow.GuildConfigs.For(textCh.Guild.Id, set => set).DefaultMusicVolume;
                }
                var mp = new MusicPlayer(voiceCh, textCh, vol);
                IUserMessage playingMessage = null;
                IUserMessage lastFinishedMessage = null;
                mp.OnCompleted += async (s, song) =>
                {
                    try
                    {
                        lastFinishedMessage?.DeleteAfter(0);

                        try
                        {
                            lastFinishedMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                    .WithAuthor(eab => eab.WithName(GetText("finished_song")).WithMusicIcon())
                                    .WithDescription(song.PrettyName)
                                    .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }

                        if (mp.Autoplay && mp.Playlist.Count == 0 && song.SongInfo.ProviderType == MusicType.Normal)
                        {
                            var relatedVideos = (await NadekoBot.Google.GetRelatedVideosAsync(song.SongInfo.Query, 4)).ToList();
                            if(relatedVideos.Count > 0)
                            await QueueSong(await queuer.Guild.GetCurrentUserAsync(), 
                                textCh, 
                                voiceCh, 
                                relatedVideos[new NadekoRandom().Next(0, relatedVideos.Count)],
                                true).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                };

                mp.OnStarted += async (player, song) =>
                {
                    try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); }
                    catch
                    {
                        // ignored
                    }
                    var sender = player;
                    if (sender == null)
                        return;
                    try
                    {
                        playingMessage?.DeleteAfter(0);

                        playingMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                    .WithAuthor(eab => eab.WithName(GetText("playing_song")).WithMusicIcon())
                                                    .WithDescription(song.PrettyName)
                                                    .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                                    .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnPauseChanged += async (paused) =>
                {
                    try
                    {
                        IUserMessage msg;
                        if (paused)
                            msg = await mp.OutputTextChannel.SendConfirmAsync(GetText("paused")).ConfigureAwait(false);
                        else
                            msg = await mp.OutputTextChannel.SendConfirmAsync(GetText("resumed")).ConfigureAwait(false);

                        msg?.DeleteAfter(10);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                
                mp.SongRemoved += async (song, index) =>
                {
                    try
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eab => eab.WithName(GetText("removed_song") + " #" + (index + 1)).WithMusicIcon())
                            .WithDescription(song.PrettyName)
                            .WithFooter(ef => ef.WithText(song.PrettyInfo))
                            .WithErrorColor();

                        await mp.OutputTextChannel.EmbedAsync(embed).ConfigureAwait(false);

                    }
                    catch
                    {
                        // ignored
                    }
                };
                return mp;
            });
            Song resolvedSong;
            try
            {
                musicPlayer.ThrowIfQueueFull();
                resolvedSong = await SongHandler.ResolveSong(query, musicType).ConfigureAwait(false);

                if (resolvedSong == null)
                    throw new SongNotFoundException();

                musicPlayer.AddSong(resolvedSong, queuer.Username);
            }
            catch (PlaylistFullException)
            {
                try
                {
                    await textCh.SendConfirmAsync(GetText("queue_full", musicPlayer.MaxQueueSize));
                }
                catch
                {
                    // ignored
                }
                throw;
            }
            if (!silent)
            {
                try
                {
                    //var queuedMessage = await textCh.SendConfirmAsync($"🎵 Queued **{resolvedSong.SongInfo.Title}** at `#{musicPlayer.Playlist.Count + 1}`").ConfigureAwait(false);
                    var queuedMessage = await textCh.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                            .WithAuthor(eab => eab.WithName(GetText("queued_song") + " #" + (musicPlayer.Playlist.Count + 1)).WithMusicIcon())
                                                            .WithDescription($"{resolvedSong.PrettyName}\n{GetText("queue")} ")
                                                            .WithThumbnailUrl(resolvedSong.Thumbnail)
                                                            .WithFooter(ef => ef.WithText(resolvedSong.PrettyProvider)))
                                                            .ConfigureAwait(false);
                    queuedMessage?.DeleteAfter(10);
                }
                catch
                {
                    // ignored
                } // if queued message sending fails, don't attempt to delete it
            }
        }
    }
}