using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Core.Services.Database.Models;
using NLog;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Core.Services;
using NadekoBot.Modules.Music.Common;
using NadekoBot.Modules.Music.Common.Exceptions;
using NadekoBot.Modules.Music.Common.SongResolver;
using NadekoBot.Common.Collections;
using System;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Modules.Music.Services
{
    public class MusicService : INService, IUnloadableService
    {
        public const string MusicDataPath = "data/musicdata";

        private readonly IGoogleApiService _google;
        private readonly NadekoStrings _strings;
        private readonly ILocalization _localization;
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly ConcurrentDictionary<ulong, MusicSettings> _musicSettings;
        private readonly SoundCloudApiService _sc;
        private readonly IBotCredentials _creds;
        private readonly ConcurrentDictionary<ulong, float> _defaultVolumes;

        public ConcurrentHashSet<ulong> AutoDcServers { get; }

        private readonly DiscordSocketClient _client;

        public ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public MusicService(DiscordSocketClient client, IGoogleApiService google,
            NadekoStrings strings, ILocalization localization, DbService db,
            SoundCloudApiService sc, IBotCredentials creds, NadekoBot bot)
        {
            _client = client;
            _google = google;
            _strings = strings;
            _localization = localization;
            _db = db;
            _sc = sc;
            _creds = creds;
            _log = LogManager.GetCurrentClassLogger();
            _musicSettings = bot.AllGuildConfigs.ToDictionary(x => x.GuildId, x => x.MusicSettings)
                .ToConcurrent();

            _client.LeftGuild += _client_LeftGuild;

            try { Directory.Delete(MusicDataPath, true); } catch { }

            _defaultVolumes = new ConcurrentDictionary<ulong, float>(
                bot.AllGuildConfigs
                    .ToDictionary(x => x.GuildId, x => x.DefaultMusicVolume));

            AutoDcServers = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs.Where(x => x.AutoDcFromVc).Select(x => x.GuildId));

            Directory.CreateDirectory(MusicDataPath);
        }

        public Task Unload()
        {
            _client.LeftGuild -= _client_LeftGuild;
            return Task.CompletedTask;
        }

        private Task _client_LeftGuild(SocketGuild arg)
        {
            var t = DestroyPlayer(arg.Id);
            return Task.CompletedTask;
        }

        public float GetDefaultVolume(ulong guildId)
        {
            return _defaultVolumes.GetOrAdd(guildId, (id) =>
            {
                using (var uow = _db.UnitOfWork)
                {
                    return uow.GuildConfigs.For(guildId, set => set).DefaultMusicVolume;
                }
            });
        }

        public Task<MusicPlayer> GetOrCreatePlayer(ICommandContext context)
        {
            var gUsr = (IGuildUser)context.User;
            var txtCh = (ITextChannel)context.Channel;
            var vCh = gUsr.VoiceChannel;
            return GetOrCreatePlayer(context.Guild.Id, vCh, txtCh);
        }

        public async Task<MusicPlayer> GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh)
        {
            string GetText(string text, params object[] replacements) =>
                _strings.GetText(text, _localization.GetCultureInfo(textCh.Guild), "Music".ToLowerInvariant(), replacements);
            
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (textCh != null)
                {
                    await textCh.SendErrorAsync(GetText("must_be_in_voice")).ConfigureAwait(false);
                }
                throw new NotInVoiceChannelException();
            }
            return MusicPlayers.GetOrAdd(guildId, _ =>
            {
                var vol = GetDefaultVolume(guildId);
                if (!_musicSettings.TryGetValue(guildId, out var ms))
                    ms = new MusicSettings();

                var mp = new MusicPlayer(this, ms, _google, voiceCh, textCh, vol);

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

                        var (Index, Current) = mp.Current;
                        if (Current == null
                            && !mp.RepeatCurrentSong
                            && !mp.RepeatPlaylist
                            && !mp.FairPlay
                            && AutoDcServers.Contains(guildId))
                        {
                            await DestroyPlayer(guildId).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnStarted += async (player, song) =>
                {
                    //try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); }
                    //catch
                    //{
                    //    // ignored
                    //}
                    var sender = player;
                    if (sender == null)
                        return;
                    try
                    {
                        playingMessage?.DeleteAfter(0);

                        playingMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                    .WithAuthor(eab => eab.WithName(GetText("playing_song", song.Index + 1)).WithMusicIcon())
                                                    .WithDescription(song.Song.PrettyName)
                                                    .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + song.Song.PrettyInfo)))
                                                    .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnPauseChanged += async (player, paused) =>
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
                _log.Info("Done creating");
                return mp;
            });
        }

        public MusicPlayer GetPlayerOrDefault(ulong guildId)
        {
            if (MusicPlayers.TryGetValue(guildId, out var mp))
                return mp;
            else
                return null;
        }

        public async Task TryQueueRelatedSongAsync(SongInfo song, ITextChannel txtCh, IVoiceChannel vch)
        {
            var related = (await _google.GetRelatedVideosAsync(song.VideoId, 4)).ToArray();
            if (!related.Any())
                return;

            var si = await ResolveSong(related[new NadekoRandom().Next(related.Length)], _client.CurrentUser.ToString(), MusicType.YouTube);
            if (si == null)
                throw new SongNotFoundException();
            var mp = await GetOrCreatePlayer(txtCh.GuildId, vch, txtCh);
            mp.Enqueue(si);
        }

        public async Task<SongInfo> ResolveSong(string query, string queuerName, MusicType? musicType = null)
        {
            query.ThrowIfNull(nameof(query));

            ISongResolverFactory resolverFactory = new SongResolverFactory(_sc);
            var strategy = await resolverFactory.GetResolveStrategy(query, musicType);
            var sinfo = await strategy.ResolveSong(query);

            if (sinfo == null)
                return null;

            sinfo.QueuerName = queuerName;

            return sinfo;
        }

        public async Task DestroyAllPlayers()
        {
            foreach (var key in MusicPlayers.Keys)
            {
                await DestroyPlayer(key);
            }
        }

        public async Task DestroyPlayer(ulong id)
        {
            if (MusicPlayers.TryRemove(id, out var mp))
                await mp.Destroy();
        }

        public bool ToggleAutoDc(ulong id)
        {
            bool val;
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(id, set => set);
                val = gc.AutoDcFromVc = !gc.AutoDcFromVc;
                uow.Complete();
            }

            if (val)
                AutoDcServers.Add(id);
            else
                AutoDcServers.TryRemove(id);

            return val;
        }

        public void UpdateSettings(ulong id, MusicSettings musicSettings)
        {
            _musicSettings.AddOrUpdate(id, musicSettings, delegate { return musicSettings; });
        }

        public void SetMusicChannel(ulong guildId, ulong? cid)
        {
            using (var uow = _db.UnitOfWork)
            {
                var ms = uow.GuildConfigs.For(guildId, set => set.Include(x => x.MusicSettings)).MusicSettings;
                ms.MusicChannelId = cid;
                uow.Complete();
            }
        }

        public void SetSongAutoDelete(ulong guildId, bool val)
        {
            using (var uow = _db.UnitOfWork)
            {
                var ms = uow.GuildConfigs.For(guildId, set => set.Include(x => x.MusicSettings)).MusicSettings;
                ms.SongAutoDelete = val;
                uow.Complete();
            }
        }
    }
}