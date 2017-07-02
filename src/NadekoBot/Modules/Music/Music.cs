using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using Discord;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System;
using System.Linq;
using NadekoBot.Extensions;
using System.Collections.Generic;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Music;
using NadekoBot.DataStructures;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace NadekoBot.Modules.Music
{
    [NoPublicBot]
    public class Music : NadekoTopLevelModule 
    {
        private static MusicService _music;
        private readonly DiscordSocketClient _client;
        private readonly IBotCredentials _creds;
        private readonly IGoogleApiService _google;
        private readonly DbService _db;

        public Music(DiscordSocketClient client, IBotCredentials creds, IGoogleApiService google,
            DbService db, MusicService music)
        {
            _client = client;
            _creds = creds;
            _google = google;
            _db = db;
            _music = music;

            //_client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
        }

        //todo when someone drags nadeko from one voice channel to another

        //private Task Client_UserVoiceStateUpdated(SocketUser iusr, SocketVoiceState oldState, SocketVoiceState newState)
        //{
        //    var usr = iusr as SocketGuildUser;
        //    if (usr == null ||
        //        oldState.VoiceChannel == newState.VoiceChannel)
        //        return Task.CompletedTask;

        //    MusicPlayer player;
        //    if ((player = _music.GetPlayer(usr.Guild.Id)) == null)
        //        return Task.CompletedTask;

        //    try
        //    {
        //        //if bot moved
        //        if ((player.PlaybackVoiceChannel == oldState.VoiceChannel) &&
        //                usr.Id == _client.CurrentUser.Id)
        //        {
        //            if (player.Paused && newState.VoiceChannel.Users.Count > 1) //unpause if there are people in the new channel
        //                player.TogglePause();
        //            else if (!player.Paused && newState.VoiceChannel.Users.Count <= 1) // pause if there are no users in the new channel
        //                player.TogglePause();

        //            return Task.CompletedTask;
        //        }


        //        //if some other user moved
        //        if ((player.PlaybackVoiceChannel == newState.VoiceChannel && //if joined first, and player paused, unpause 
        //                player.Paused &&
        //                newState.VoiceChannel.Users.Count == 2) ||  // keep in mind bot is in the channel (+1)
        //            (player.PlaybackVoiceChannel == oldState.VoiceChannel && // if left last, and player unpaused, pause
        //                !player.Paused &&
        //                oldState.VoiceChannel.Users.Count == 1))
        //        {
        //            player.TogglePause();
        //            return Task.CompletedTask;
        //        }

        //    }
        //    catch
        //    {
        //        // ignored
        //    }
        //    return Task.CompletedTask;
        //}

        private async Task InternalQueue(MusicPlayer mp, SongInfo songInfo, bool silent)
        {
            if (songInfo == null)
            {
                if(!silent)
                    await ReplyErrorLocalized("song_not_found").ConfigureAwait(false);
                return;
            }

            (bool Success, int Index) qData;
            try
            {
                qData = mp.Enqueue(songInfo);
            }
            catch (QueueFullException)
            {
                await ReplyErrorLocalized("queue_full", mp.MaxQueueSize).ConfigureAwait(false);
                throw;
            }
            if (qData.Success)
            {
                if (!silent)
                {
                    try
                    {
                        //var queuedMessage = await textCh.SendConfirmAsync($"🎵 Queued **{resolvedSong.SongInfo.Title}** at `#{musicPlayer.Playlist.Count + 1}`").ConfigureAwait(false);
                        var queuedMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                                .WithAuthor(eab => eab.WithName(GetText("queued_song") + " #" + (qData.Index)).WithMusicIcon())
                                                                .WithDescription($"{songInfo.PrettyName}\n{GetText("queue")} ")
                                                                .WithThumbnailUrl(songInfo.Thumbnail)
                                                                .WithFooter(ef => ef.WithText(songInfo.PrettyProvider)))
                                                                .ConfigureAwait(false);
                        if (mp.Stopped)
                        {
                            (await ReplyErrorLocalized("queue_stopped", Format.Code(Prefix + "play")).ConfigureAwait(false)).DeleteAfter(10);
                        }
                        queuedMessage?.DeleteAfter(10);
                    }
                    catch
                    {
                        // ignored
                    } 
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Play([Remainder]string query = null)
        {
            if (!string.IsNullOrWhiteSpace(query))
                try { return Queue(query); } catch (QueueFullException) { return Task.CompletedTask; }
            else
                return Next();
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Queue([Remainder] string query)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var songInfo = await _music.ResolveSong(query, Context.User.ToString());
            
            try { await InternalQueue(mp, songInfo, false); } catch (QueueFullException) { return; }

            if ((await Context.Guild.GetCurrentUserAsync()).GetPermissions((IGuildChannel)Context.Channel).ManageMessages)
            {
                Context.Message.DeleteAfter(10);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task QueueSearch([Remainder] string query)
        {
            var videos = (await _google.GetVideoInfosByKeywordAsync(query, 5))
                .ToArray();

            if (!videos.Any())
            {
                await ReplyErrorLocalized("song_not_found").ConfigureAwait(false);
                return;
            }

            var msg = await Context.Channel.SendConfirmAsync(string.Join("\n", videos.Select((x, i) => $"`{i + 1}.`\n\t{Format.Bold(x.Name)}\n\t{x.Url}")));

            try
            {
                var input = await GetUserInputAsync(Context.User.Id, Context.Channel.Id);
                if (input == null
                    || !int.TryParse(input, out var index)
                    || (index -= 1) < 0
                    || index >= videos.Length)
                {
                    try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    return;
                }

                query = videos[index].Url;

                await Queue(query).ConfigureAwait(false);
            }
            finally
            {
                try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
            }
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue(int page = 0)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var (current, songs) = mp.QueueArray();

            if (!songs.Any())
            {
                await ReplyErrorLocalized("no_player").ConfigureAwait(false);
                return;
            }
            
            if (--page < -1)
                return;
            //try { await musicPlayer.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            const int itemsPerPage = 10;

            if (page == -1)
                page = current / itemsPerPage;

            //if page is 0 (-1 after this decrement) that means default to the page current song is playing from

            //var total = musicPlayer.TotalPlaytime;
            //var totalStr = total == TimeSpan.MaxValue ? "∞" : GetText("time_format",
            //    (int)total.TotalHours,
            //    total.Minutes,
            //    total.Seconds);
            //var maxPlaytime = musicPlayer.MaxPlaytimeSeconds;
            var lastPage = songs.Length / itemsPerPage;
            Func<int, EmbedBuilder> printAction = curPage =>
            {
                var startAt = itemsPerPage * curPage;
                var number = 0 + startAt;
                var desc = string.Join("\n", songs
                        .Skip(startAt)
                        .Take(itemsPerPage)
                        .Select(v =>
                        {
                            if(number++ == current)
                                return $"**⇒**`{number}.` {v.PrettyFullName}";
                            else
                                return $"`{number}.` {v.PrettyFullName}";
                        }));

                desc = $"`🔊` {songs[current].PrettyFullName}\n\n" + desc;

                var add = "";
                if (mp.Stopped)
                    add += Format.Bold(GetText("queue_stopped", Format.Code(Prefix + "play"))) + "\n";
                if (mp.RepeatCurrentSong)
                    add += "🔂 " + GetText("repeating_cur_song") + "\n";
                else if (mp.Shuffle)
                    add += "🔀 " + GetText("shuffling_playlist") + "\n";
                else
                {
                    if (mp.RepeatPlaylist)
                        add += "🔁 " + GetText("repeating_playlist") + "\n";
                    if (mp.Autoplay)
                        add += "↪ " + GetText("autoplaying") + "\n";
                }

                if (!string.IsNullOrWhiteSpace(add))
                    desc += add + "\n";
                
                var embed = new EmbedBuilder()
                    .WithAuthor(eab => eab.WithName(GetText("player_queue", curPage + 1, lastPage + 1))
                        .WithMusicIcon())
                    .WithDescription(desc)
                    //.WithFooter(ef => ef.WithText($"{musicPlayer.PrettyVolume} | {musicPlayer.Playlist.Count} " +
                    //                              $"{("tracks".SnPl(musicPlayer.Playlist.Count))} | {totalStr} | " +
                    //                              (musicPlayer.FairPlay
                    //                                  ? "✔️" + GetText("fairplay")
                    //                                  : "✖️" + GetText("fairplay")) + " | " +
                    //                              (maxPlaytime == 0 ? "unlimited" : GetText("play_limit", maxPlaytime))))
                    .WithOkColor();

                return embed;
            };
            await Context.Channel.SendPaginatedConfirmAsync(_client, page, printAction, lastPage, false).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Next(int skipCount = 1)
        {
            if (skipCount < 1)
                return;
            
            var mp = await _music.GetOrCreatePlayer(Context);

            mp.Next(skipCount);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Stop()
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            mp.Stop();
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Destroy()
        {
            await _music.DestroyPlayer(Context.Guild.Id);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Pause()
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            mp.TogglePause();
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(int val)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            if (val < 0 || val > 100)
            {
                await ReplyErrorLocalized("volume_input_invalid").ConfigureAwait(false);
                return;
            }
            mp.SetVolume(val);
            await ReplyConfirmLocalized("volume_set", val).ConfigureAwait(false);
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
            using (var uow = _db.UnitOfWork)
            {
                uow.GuildConfigs.For(Context.Guild.Id, set => set).DefaultMusicVolume = val / 100.0f;
                uow.Complete();
            }
            await ReplyConfirmLocalized("defvol_set", val).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task SongRemove(int index)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            try
            {
                var song = mp.RemoveAt(index - 1);
                var embed = new EmbedBuilder()
                            .WithAuthor(eab => eab.WithName(GetText("removed_song") + " #" + (index)).WithMusicIcon())
                            .WithDescription(song.PrettyName)
                            .WithFooter(ef => ef.WithText(song.PrettyInfo))
                            .WithErrorColor();

                await mp.OutputTextChannel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                await ReplyErrorLocalized("removed_song_error").ConfigureAwait(false);
            }
        }

        public enum All { All }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task SongRemove(All all)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            mp.Stop(true);
            await ReplyConfirmLocalized("queue_cleared").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Playlists([Remainder] int num = 1)
        {
            if (num <= 0)
                return;

            List<MusicPlaylist> playlists;

            using (var uow = _db.UnitOfWork)
            {
                playlists = uow.MusicPlaylists.GetPlaylistsOnPage(num);
            }

            var embed = new EmbedBuilder()
                .WithAuthor(eab => eab.WithName(GetText("playlists_page", num)).WithMusicIcon())
                .WithDescription(string.Join("\n", playlists.Select(r =>
                    GetText("playlists", r.Id, r.Name, r.Author, r.Songs.Count))))
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
                using (var uow = _db.UnitOfWork)
                {
                    var pl = uow.MusicPlaylists.Get(id);

                    if (pl != null)
                    {
                        if (_creds.IsOwner(Context.User) || pl.AuthorId == Context.User.Id)
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
        public async Task Save([Remainder] string name)
        {
            var mp = await _music.GetOrCreatePlayer(Context);

            var songs = mp.QueueArray().Songs
                .Select(s => new PlaylistSong()
                {
                    Provider = s.Provider,
                    ProviderType = s.ProviderType,
                    Title = s.Title,
                    Uri = s.Uri,
                    Query = s.Query,
                }).ToList();

            MusicPlaylist playlist;
            using (var uow = _db.UnitOfWork)
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

        private static readonly ConcurrentHashSet<ulong> PlaylistLoadBlacklist = new ConcurrentHashSet<ulong>();

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Load([Remainder] int id)
        {
            if (!PlaylistLoadBlacklist.Add(Context.Guild.Id))
                return;
            try
            {
                var mp = await _music.GetOrCreatePlayer(Context);
                MusicPlaylist mpl;
                using (var uow = _db.UnitOfWork)
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
                    try
                    {
                        await Task.Yield();
                        
                        await Task.WhenAll(Task.Delay(1000), InternalQueue(mp, await _music.ResolveSong(item.Query, Context.User.ToString(), item.ProviderType), true)).ConfigureAwait(false);
                    }
                    catch (SongNotFoundException) { }
                    catch { break; }
                }
                if (msg != null)
                    await msg.ModifyAsync(m => m.Content = GetText("playlist_queue_complete")).ConfigureAwait(false);
            }
            finally
            {
                PlaylistLoadBlacklist.TryRemove(Context.Guild.Id);
            }
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task Fairplay()
        //{
        //    var channel = (ITextChannel)Context.Channel;
        //    MusicPlayer musicPlayer;
        //    if ((musicPlayer = _music.GetPlayer(Context.Guild.Id)) == null)
        //        return;
        //    if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
        //        return;
        //    var val = musicPlayer.FairPlay = !musicPlayer.FairPlay;

        //    if (val)
        //    {
        //        await ReplyConfirmLocalized("fp_enabled").ConfigureAwait(false);
        //    }
        //    else
        //    {
        //        await ReplyConfirmLocalized("fp_disabled").ConfigureAwait(false);
        //    }
        //}

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SoundCloudQueue([Remainder] string query)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var song = await _music.ResolveSong(query, Context.User.ToString(), MusicType.Soundcloud);
            await InternalQueue(mp, song, false).ConfigureAwait(false);
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SoundCloudPl([Remainder] string pl)
        {
            pl = pl?.Trim();

            if (string.IsNullOrWhiteSpace(pl))
                return;

            var mp = await _music.GetOrCreatePlayer(Context);

            using (var http = new HttpClient())
            {
                var scvids = JObject.Parse(await http.GetStringAsync($"https://scapi.nadekobot.me/resolve?url={pl}").ConfigureAwait(false))["tracks"].ToObject<SoundCloudVideo[]>();
                IUserMessage msg = null;
                try { msg = await Context.Channel.SendMessageAsync(GetText("attempting_to_queue", Format.Bold(scvids.Length.ToString()))).ConfigureAwait(false); } catch { }
                foreach (var svideo in scvids)
                {
                    try
                    {
                        await Task.Yield();
                        await InternalQueue(mp, await _music.SongInfoFromSVideo(svideo, Context.User.ToString()), true);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                        break;
                    }
                }
                if (msg != null)
                    await msg.ModifyAsync(m => m.Content = GetText("playlist_queue_complete")).ConfigureAwait(false);
            }
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlaying()
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var (_, currentSong) = mp.Current;
            if (currentSong == null)
                return;
            //try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            var embed = new EmbedBuilder().WithOkColor()
                            .WithAuthor(eab => eab.WithName(GetText("now_playing")).WithMusicIcon())
                            .WithDescription(currentSong.PrettyName)
                            .WithThumbnailUrl(currentSong.Thumbnail)
                            .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + /*currentSong.PrettyFullTime  +*/ $" | {currentSong.PrettyProvider} | {currentSong.QueuerName}"));

            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ShufflePlaylist()
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var val = mp.ToggleShuffle();
            if(val)
                await ReplyConfirmLocalized("songs_shuffle_enable").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("songs_shuffle_disable").ConfigureAwait(false);
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task Playlist([Remainder] string playlist)
        //{

        //    var arg = playlist;
        //    if (string.IsNullOrWhiteSpace(arg))
        //        return;
        //    if (((IGuildUser)Context.User).VoiceChannel?.Guild != Context.Guild)
        //    {
        //        await ReplyErrorLocalized("must_be_in_voice").ConfigureAwait(false);
        //        return;
        //    }
        //    var plId = (await _google.GetPlaylistIdsByKeywordsAsync(arg).ConfigureAwait(false)).FirstOrDefault();
        //    if (plId == null)
        //    {
        //        await ReplyErrorLocalized("no_search_results").ConfigureAwait(false);
        //        return;
        //    }
        //    var ids = await _google.GetPlaylistTracksAsync(plId, 500).ConfigureAwait(false);
        //    if (!ids.Any())
        //    {
        //        await ReplyErrorLocalized("no_search_results").ConfigureAwait(false);
        //        return;
        //    }
        //    var count = ids.Count();
        //    var msg = await Context.Channel.SendMessageAsync("🎵 " + GetText("attempting_to_queue",
        //        Format.Bold(count.ToString()))).ConfigureAwait(false);

        //    var cancelSource = new CancellationTokenSource();

        //    var gusr = (IGuildUser)Context.User;
        //    while (ids.Any() && !cancelSource.IsCancellationRequested)
        //    {
        //        var tasks = Task.WhenAll(ids.Take(5).Select(async id =>
        //        {
        //            if (cancelSource.Token.IsCancellationRequested)
        //                return;
        //            try
        //            {
        //                await _music.QueueSong(gusr, (ITextChannel)Context.Channel, gusr.VoiceChannel, id, true).ConfigureAwait(false);
        //            }
        //            catch (SongNotFoundException) { }
        //            catch { try { cancelSource.Cancel(); } catch { } }
        //        }));

        //        await Task.WhenAny(tasks, Task.Delay(Timeout.Infinite, cancelSource.Token));
        //        ids = ids.Skip(5);
        //    }

        //    await msg.ModifyAsync(m => m.Content = "✅ " + Format.Bold(GetText("playlist_queue_complete"))).ConfigureAwait(false);
        //}


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Radio(string radioLink)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var song = await _music.ResolveSong(radioLink, Context.User.ToString(), MusicType.Radio);
            await InternalQueue(mp, song, false).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Local([Remainder] string path)
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var song = await _music.ResolveSong(path, Context.User.ToString(), MusicType.Local);
            await InternalQueue(mp, song, false).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task LocalPl([Remainder] string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
                return;

            var mp = await _music.GetOrCreatePlayer(Context);

            DirectoryInfo dir;
            try { dir = new DirectoryInfo(dirPath); } catch { return; }
            var fileEnum = dir.GetFiles("*", SearchOption.AllDirectories)
                                .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System) && x.Extension != ".jpg" && x.Extension != ".png");
            foreach (var file in fileEnum)
            {
                try
                {
                    await Task.Yield();
                    var song = await _music.ResolveSong(file.FullName, Context.User.ToString(), MusicType.Local);
                    await InternalQueue(mp, song, true).ConfigureAwait(false);
                }
                catch (QueueFullException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    break;
                }
            }
            await ReplyConfirmLocalized("dir_queue_complete").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public void Move()
        {
            var vch = ((IGuildUser)Context.User).VoiceChannel;

            if (vch == null)
                return;

            var mp = _music.GetPlayerOrDefault(Context.Guild.Id);

            if (mp == null)
                return;

            mp.SetVoiceChannel(vch);
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task MoveSong([Remainder] string fromto)
        //{
        //    if (string.IsNullOrWhiteSpace(fromto))
        //        return;

        //    MusicPlayer musicPlayer;
        //    if ((musicPlayer = _music.GetPlayer(Context.Guild.Id)) == null)
        //        return;

        //    fromto = fromto?.Trim();
        //    var fromtoArr = fromto.Split('>');

        //    int n1;
        //    int n2;

        //    var playlist = musicPlayer.Playlist as List<Song> ?? musicPlayer.Playlist.ToList();

        //    if (fromtoArr.Length != 2 || !int.TryParse(fromtoArr[0], out n1) ||
        //        !int.TryParse(fromtoArr[1], out n2) || n1 < 1 || n2 < 1 || n1 == n2 ||
        //        n1 > playlist.Count || n2 > playlist.Count)
        //    {
        //        await ReplyConfirmLocalized("invalid_input").ConfigureAwait(false);
        //        return;
        //    }

        //    var s = playlist[n1 - 1];
        //    playlist.Insert(n2 - 1, s);
        //    var nn1 = n2 < n1 ? n1 : n1 - 1;
        //    playlist.RemoveAt(nn1);

        //    var embed = new EmbedBuilder()
        //        .WithTitle($"{s.SongInfo.Title.TrimTo(70)}")
        //        .WithUrl(s.SongUrl)
        //    .WithAuthor(eab => eab.WithName(GetText("song_moved")).WithIconUrl("https://cdn.discordapp.com/attachments/155726317222887425/258605269972549642/music1.png"))
        //    .AddField(fb => fb.WithName(GetText("from_position")).WithValue($"#{n1}").WithIsInline(true))
        //    .AddField(fb => fb.WithName(GetText("to_position")).WithValue($"#{n2}").WithIsInline(true))
        //    .WithColor(NadekoBot.OkColor);
        //    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);

        //    //await channel.SendConfirmAsync($"🎵Moved {s.PrettyName} `from #{n1} to #{n2}`").ConfigureAwait(false);
        //}
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SetMaxQueue(uint size = 0)
        {
            if (size < 0)
                return;
            var mp = await _music.GetOrCreatePlayer(Context);

            mp.MaxQueueSize = size;

            if (size == 0)
                await ReplyConfirmLocalized("max_queue_unlimited").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("max_queue_x", size).ConfigureAwait(false);
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task SetMaxPlaytime(uint seconds)
        //{
        //    if (seconds < 15 && seconds != 0)
        //        return;

        //    var channel = (ITextChannel)Context.Channel;
        //    MusicPlayer musicPlayer;
        //    if ((musicPlayer = _music.GetPlayer(Context.Guild.Id)) == null)
        //        return;
        //    musicPlayer.MaxPlaytimeSeconds = seconds;
        //    if (seconds == 0)
        //        await ReplyConfirmLocalized("max_playtime_none").ConfigureAwait(false);
        //    else
        //        await ReplyConfirmLocalized("max_playtime_set", seconds).ConfigureAwait(false);
        //}
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ReptCurSong()
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var (_, currentSong) = mp.Current;
            if (currentSong == null)
                return;
            var currentValue = mp.ToggleRepeatSong();

            if (currentValue)
                await Context.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithMusicIcon().WithName("🔂 " + GetText("repeating_track")))
                    .WithDescription(currentSong.PrettyName)
                    .WithFooter(ef => ef.WithText(currentSong.PrettyInfo))).ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync("🔂 " + GetText("repeating_track_stopped"))
                                            .ConfigureAwait(false);
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RepeatPl()
        {
            var mp = await _music.GetOrCreatePlayer(Context);
            var currentValue = mp.ToggleRepeatPlaylist();
            if (currentValue)
                await ReplyConfirmLocalized("rpl_enabled").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("rpl_disabled").ConfigureAwait(false);
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task Goto(int time)
        //{
        //    MusicPlayer musicPlayer;
        //    if ((musicPlayer = _music.GetPlayer(Context.Guild.Id)) == null)
        //        return;
        //    if (((IGuildUser)Context.User).VoiceChannel != musicPlayer.PlaybackVoiceChannel)
        //        return;

        //    if (time < 0)
        //        return;

        //    var currentSong = musicPlayer.CurrentSong;

        //    if (currentSong == null)
        //        return;

        //    //currentSong.PrintStatusMessage = false;
        //    var gotoSong = currentSong.Clone();
        //    gotoSong.SkipTo = time;
        //    musicPlayer.AddSong(gotoSong, 0);
        //    musicPlayer.Next();

        //    var minutes = (time / 60).ToString();
        //    var seconds = (time % 60).ToString();

        //    if (minutes.Length == 1)
        //        minutes = "0" + minutes;
        //    if (seconds.Length == 1)
        //        seconds = "0" + seconds;

        //    await ReplyConfirmLocalized("skipped_to", minutes, seconds).ConfigureAwait(false);
        //}

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Autoplay()
        {
            var mp = await _music.GetOrCreatePlayer(Context);

            if (!mp.ToggleAutoplay())
                await ReplyConfirmLocalized("autoplay_disabled").ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("autoplay_enabled").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetMusicChannel()
        {
            var mp = await _music.GetOrCreatePlayer(Context);

            mp.OutputTextChannel = (ITextChannel)Context.Channel;

            await ReplyConfirmLocalized("set_music_channel").ConfigureAwait(false);
        }
    }
}