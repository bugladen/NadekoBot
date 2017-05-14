using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Modules.Music.Classes;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Modules;

namespace NadekoBot.Services.Music
{
    public class MusicService
    {
        private readonly IGoogleApiService _google;
        public ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public MusicService(IGoogleApiService google)
        {
            _google = google;
        }

        public MusicPlayer GetPlayer(ulong guildId)
        {
            MusicPlayers.TryGetValue(guildId, out var player);
            return player;
        }

        public MusicPlayer GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh)
        {
            string GetText(string text, params object[] replacements) =>
                NadekoTopLevelModule.GetTextStatic(text, NadekoBot.Localization.GetCultureInfo(textCh.Guild), nameof(Modules.Music.Music).ToLowerInvariant(), replacements);

            return MusicPlayers.GetOrAdd(guildId, server =>
            {
                float vol;// SpecificConfigurations.Default.Of(server.Id).DefaultMusicVolume;
                using (var uow = DbHandler.UnitOfWork())
                {
                    //todo move to cached variable
                    vol = uow.GuildConfigs.For(guildId, set => set).DefaultMusicVolume;
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
                            if (relatedVideos.Count > 0)
                                await QueueSong(await textCh.Guild.GetCurrentUserAsync(),
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
        }


        public async Task QueueSong(IGuildUser queuer, ITextChannel textCh, IVoiceChannel voiceCh, string query, bool silent = false, MusicType musicType = MusicType.Normal)
        {
            string GetText(string text, params object[] replacements) => 
                NadekoTopLevelModule.GetTextStatic(text, NadekoBot.Localization.GetCultureInfo(textCh.Guild), nameof(Modules.Music.Music).ToLowerInvariant(), replacements);

            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (!silent)
                    await textCh.SendErrorAsync(GetText("must_be_in_voice")).ConfigureAwait(false);
                throw new ArgumentNullException(nameof(voiceCh));
            }
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                throw new ArgumentException("Invalid song query.", nameof(query));

            var musicPlayer = GetOrCreatePlayer(textCh.Guild.Id, voiceCh, textCh);
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

        internal void DestroyPlayer(ulong id)
        {
            throw new NotImplementedException();
        }


    }
}
