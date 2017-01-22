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
using System.Text.RegularExpressions;
using System.Threading;

namespace NadekoBot.Modules.Music
{
    [NadekoModule("Music", "!!")]
    [DontAutoLoad]
    public partial class Music : DiscordModule
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
            catch { }
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

            await channel.SendConfirmAsync("Fair play " + (val ? "enabled" : "disabled") + ".").ConfigureAwait(false);
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

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
            {
                await Context.Channel.SendErrorAsync("🎵 No active music player.").ConfigureAwait(false);
                return;
            }
            if (page <= 0)
                return;

            var currentSong = musicPlayer.CurrentSong;
            if (currentSong == null)
            {
                await Context.Channel.SendErrorAsync("🎵 No active music player.").ConfigureAwait(false);
                return;
            }

            try { await musicPlayer.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            const int itemsPerPage = 10;

            var total = musicPlayer.TotalPlaytime;
            var totalStr = total == TimeSpan.MaxValue ? "∞" : $"{(int)total.TotalHours}h {total.Minutes}m {total.Seconds}s";
            var maxPlaytime = musicPlayer.MaxPlaytimeSeconds;
            var lastPage = musicPlayer.Playlist.Count / itemsPerPage;
            Func<int, EmbedBuilder> printAction = (curPage) =>
            {
                int startAt = itemsPerPage * (curPage - 1);
                var number = 0 + startAt;
                var embed = new EmbedBuilder()
                    .WithAuthor(eab => eab.WithName($"Player Queue - Page {curPage}/{lastPage + 1}")
                                          .WithMusicIcon())
                    .WithDescription(string.Join("\n", musicPlayer.Playlist
                        .Skip(startAt)
                        .Take(itemsPerPage)
                        .Select(v => $"`{++number}.` {v.PrettyFullName}")))
                    .WithFooter(ef => ef.WithText($"{musicPlayer.PrettyVolume} | {musicPlayer.Playlist.Count} " +
    $"{("tracks".SnPl(musicPlayer.Playlist.Count))} | {totalStr} | " +
    (musicPlayer.FairPlay ? "✔️fairplay" : "✖️fairplay") + $" | " + (maxPlaytime == 0 ? "unlimited" : $"{maxPlaytime}s limit")))
                    .WithOkColor();

                if (musicPlayer.RepeatSong)
                {
                    embed.WithTitle($"🔂 Repeating Song: {currentSong.SongInfo.Title} | {currentSong.PrettyFullTime}");
                }
                else if (musicPlayer.RepeatPlaylist)
                {
                    embed.WithTitle("🔁 Repeating Playlist");
                }
                if (musicPlayer.MaxQueueSize != 0 && musicPlayer.Playlist.Count >= musicPlayer.MaxQueueSize)
                {
                    embed.WithTitle("🎵 Song queue is full!");
                }
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
                            .WithAuthor(eab => eab.WithName("Now Playing").WithMusicIcon())
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
            if (val < 0)
                return;
            var volume = musicPlayer.SetVolume(val);
            await Context.Channel.SendConfirmAsync($"🎵 Volume set to {volume}%").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Defvol([Remainder] int val)
        {


            if (val < 0 || val > 100)
            {
                await Context.Channel.SendErrorAsync("Volume number invalid. Must be between 0 and 100").ConfigureAwait(false);
                return;
            }
            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(Context.Guild.Id, set => set).DefaultMusicVolume = val / 100.0f;
                uow.Complete();
            }
            await Context.Channel.SendConfirmAsync($"🎵 Default volume set to {val}%").ConfigureAwait(false);
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
            {
                await Context.Channel.SendErrorAsync("💢 Not enough songs in order to perform the shuffle.").ConfigureAwait(false);
                return;
            }

            musicPlayer.Shuffle();
            await Context.Channel.SendConfirmAsync("🎵 Songs shuffled.").ConfigureAwait(false);
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
                await Context.Channel.SendErrorAsync($"💢 You need to be in a **voice channel** on this server.").ConfigureAwait(false);
                return;
            }
            var plId = (await NadekoBot.Google.GetPlaylistIdsByKeywordsAsync(arg).ConfigureAwait(false)).FirstOrDefault();
            if (plId == null)
            {
                await Context.Channel.SendErrorAsync("No search results for that query.");
                return;
            }
            var ids = await NadekoBot.Google.GetPlaylistTracksAsync(plId, 500).ConfigureAwait(false);
            if (!ids.Any())
            {
                await Context.Channel.SendErrorAsync($"🎵 Failed to find any songs.").ConfigureAwait(false);
                return;
            }
            var count = ids.Count();

            var msg = await Context.Channel.SendMessageAsync($"🎵 Attempting to queue **{count}** songs".SnPl(count) + "...").ConfigureAwait(false);

            var cancelSource = new CancellationTokenSource();

            var gusr = (IGuildUser)Context.User;

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

            await msg.ModifyAsync(m => m.Content = "✅ Playlist queue complete.").ConfigureAwait(false);
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
                        mp.AddSong(new Song(new Classes.SongInfo
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
            try
            {
                var dir = new DirectoryInfo(arg);
                var fileEnum = dir.GetFiles("*", SearchOption.AllDirectories)
                                    .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System));
                var gusr = (IGuildUser)Context.User;
                foreach (var file in fileEnum)
                {
                    try
                    {
                        await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, file.FullName, true, MusicType.Local).ConfigureAwait(false);
                    }
                    catch (PlaylistFullException)
                    {
                        break;
                    }
                    catch { }
                }
                await Context.Channel.SendConfirmAsync("🎵 Directory queue complete.").ConfigureAwait(false);
            }
            catch { }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Radio(string radio_link)
        {

            if (((IGuildUser)Context.User).VoiceChannel?.Guild != Context.Guild)
            {
                await Context.Channel.SendErrorAsync("💢 You need to be in a voice channel on this server.\n If you are already in a voice (ITextChannel)Context.Channel, try rejoining it.").ConfigureAwait(false);
                return;
            }
            await QueueSong(((IGuildUser)Context.User), (ITextChannel)Context.Channel, ((IGuildUser)Context.User).VoiceChannel, radio_link, musicType: MusicType.Radio).ConfigureAwait(false);
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
            await Context.Channel.SendConfirmAsync($"🎵 Queue cleared!").ConfigureAwait(false);
            return;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MoveSong([Remainder] string fromto)
        {

            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
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
                await Context.Channel.SendErrorAsync("Invalid input.").ConfigureAwait(false);
                return;
            }

            var s = playlist[n1 - 1];
            playlist.Insert(n2 - 1, s);
            var nn1 = n2 < n1 ? n1 : n1 - 1;
            playlist.RemoveAt(nn1);

            var embed = new EmbedBuilder()
                .WithTitle($"{s.SongInfo.Title.TrimTo(70)}")
            .WithUrl($"{s.SongInfo.Query}")
            .WithAuthor(eab => eab.WithName("Song Moved").WithIconUrl("https://cdn.discordapp.com/attachments/155726317222887425/258605269972549642/music1.png"))
            .AddField(fb => fb.WithName("**From Position**").WithValue($"#{n1}").WithIsInline(true))
            .AddField(fb => fb.WithName("**To Position**").WithValue($"#{n2}").WithIsInline(true))
            .WithColor(NadekoBot.OkColor);
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);

            //await channel.SendConfirmAsync($"🎵Moved {s.PrettyName} `from #{n1} to #{n2}`").ConfigureAwait(false);


        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task SetMaxQueue(uint size)
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;

            musicPlayer.MaxQueueSize = size;
            await Context.Channel.SendConfirmAsync($"🎵 Max queue set to {(size == 0 ? ("unlimited") : size + " tracks")}.");
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
                await channel.SendConfirmAsync($"🎵 Max playtime has no limit now.");
            else
                await channel.SendConfirmAsync($"🎵 Max playtime set to {seconds} seconds.");
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
                    .WithAuthor(eab => eab.WithMusicIcon().WithName("🔂 Repeating track"))
                    .WithDescription(currentSong.PrettyName)
                    .WithFooter(ef => ef.WithText(currentSong.PrettyInfo))).ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync($"🔂 Current track repeat stopped.")
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
            await Context.Channel.SendConfirmAsync($"🔁 Repeat playlist {(currentValue ? "**enabled**." : "**disabled**.")}").ConfigureAwait(false);
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

            await Context.Channel.SendConfirmAsync(($"🎵 Saved playlist as **{name}**, ID: {playlist.Id}.")).ConfigureAwait(false);
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
                await Context.Channel.SendErrorAsync("Can't find playlist with that ID.").ConfigureAwait(false);
                return;
            }
            IUserMessage msg = null;
            try { msg = await Context.Channel.SendMessageAsync($"🎶 Attempting to load **{mpl.Songs.Count}** songs...").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                await msg.ModifyAsync(m => m.Content = $"✅ Done loading playlist **{mpl.Name}**.").ConfigureAwait(false);
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
                .WithAuthor(eab => eab.WithName($"Page {num} of Saved Playlists").WithMusicIcon())
                .WithDescription(string.Join("\n", playlists.Select(r => $"`#{r.Id}` - **{r.Name}** by *{r.Author}* ({r.Songs.Count} songs)")))
                .WithOkColor();
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);

        }

        //todo only author or owner
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task DeletePlaylist([Remainder] int id)
        {


            bool success = false;
            MusicPlaylist pl = null;
            try
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    pl = uow.MusicPlaylists.Get(id);

                    if (pl != null)
                    {
                        if (NadekoBot.Credentials.IsOwner(Context.User) || pl.AuthorId == Context.User.Id)
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
                    await Context.Channel.SendErrorAsync("Failed to delete that playlist. It either doesn't exist, or you are not its author.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("🗑 Playlist successfully **deleted**.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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

            await Context.Channel.SendConfirmAsync($"Skipped to `{minutes}:{seconds}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Autoplay()
        {
            MusicPlayer musicPlayer;
            if (!MusicPlayers.TryGetValue(Context.Guild.Id, out musicPlayer))
                return;

            if (!musicPlayer.ToggleAutoplay())
                await Context.Channel.SendConfirmAsync("❌ Autoplay disabled.").ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync("✅ Autoplay enabled.").ConfigureAwait(false);
        }

        public static async Task QueueSong(IGuildUser queuer, ITextChannel textCh, IVoiceChannel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
        {
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (!silent)
                    await textCh.SendErrorAsync($"💢 You need to be in a voice channel on this server.").ConfigureAwait(false);
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
                    try
                    {
                        if (lastFinishedMessage != null)
                            lastFinishedMessage.DeleteAfter(0);

                        lastFinishedMessage = await textCh.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                  .WithAuthor(eab => eab.WithName("Finished Song").WithMusicIcon())
                                                  .WithDescription(song.PrettyName)
                                                  .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                                    .ConfigureAwait(false);

                        if (mp.Autoplay && mp.Playlist.Count == 0 && song.SongInfo.ProviderType == MusicType.Normal)
                        {
                            await QueueSong(await queuer.Guild.GetCurrentUserAsync(), textCh, voiceCh, (await NadekoBot.Google.GetRelatedVideosAsync(song.SongInfo.Query, 4)).ToList().Shuffle().FirstOrDefault(), silent, musicType).ConfigureAwait(false);
                        }
                    }
                    catch { }
                };

                mp.OnStarted += async (player, song) =>
                {
                    try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }
                    var sender = player as MusicPlayer;
                    if (sender == null)
                        return;
                    try
                    {
                        if (playingMessage != null)
                            playingMessage.DeleteAfter(0);

                        playingMessage = await textCh.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                    .WithAuthor(eab => eab.WithName("Playing Song").WithMusicIcon())
                                                    .WithDescription(song.PrettyName)
                                                    .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                                    .ConfigureAwait(false);
                    }
                    catch { }
                };
                mp.OnPauseChanged += async (paused) =>
                        {
                            try
                            {
                                IUserMessage msg;
                                if (paused)
                                    msg = await textCh.SendConfirmAsync("🎵 Music playback **paused**.").ConfigureAwait(false);
                                else
                                    msg = await textCh.SendConfirmAsync("🎵 Music playback **resumed**.").ConfigureAwait(false);

                                if (msg != null)
                                    msg.DeleteAfter(10);
                            }
                            catch { }
                        };


                mp.SongRemoved += async (song, index) =>
                {
                    try
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eab => eab.WithName("Removed song #" + (index + 1)).WithMusicIcon())
                            .WithDescription(song.PrettyName)
                            .WithFooter(ef => ef.WithText(song.PrettyInfo))
                            .WithErrorColor();

                        await textCh.EmbedAsync(embed).ConfigureAwait(false);

                    }
                    catch { }
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
                try { await textCh.SendConfirmAsync($"🎵 Queue is full at **{musicPlayer.MaxQueueSize}/{musicPlayer.MaxQueueSize}**."); } catch { }
                throw;
            }
            if (!silent)
            {
                try
                {
                    //var queuedMessage = await textCh.SendConfirmAsync($"🎵 Queued **{resolvedSong.SongInfo.Title}** at `#{musicPlayer.Playlist.Count + 1}`").ConfigureAwait(false);
                    var queuedMessage = await textCh.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                            .WithAuthor(eab => eab.WithName("Queued Song #" + (musicPlayer.Playlist.Count + 1)).WithMusicIcon())
                                                            .WithDescription($"{resolvedSong.PrettyName}\nQueue ")
                                                            .WithThumbnailUrl(resolvedSong.Thumbnail)
                                                            .WithFooter(ef => ef.WithText(resolvedSong.PrettyProvider)))
                                                            .ConfigureAwait(false);
                    if (queuedMessage != null)
                        queuedMessage.DeleteAfter(10);
                }
                catch { } // if queued message sending fails, don't attempt to delete it
            }
        }
    }
}