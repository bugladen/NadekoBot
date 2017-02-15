using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PlayingRotateCommands : NadekoSubmodule
        {
            public static List<PlayingStatus> RotatingStatusMessages { get; }
            public static volatile bool RotatingStatuses;
            private readonly object _locker = new object();
            private new static Logger _log { get; }
            private static readonly Timer _t;

            private class TimerState
            {
                public int Index { get; set; }
            }

            static PlayingRotateCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                RotatingStatusMessages = NadekoBot.BotConfig.RotatingStatusMessages;
                RotatingStatuses = NadekoBot.BotConfig.RotatingStatuses;

                _t = new Timer(async (objState) =>
                {
                    try
                    {
                        var state = (TimerState)objState;
                        if (!RotatingStatuses)
                            return;
                        if (state.Index >= RotatingStatusMessages.Count)
                            state.Index = 0;

                        if (!RotatingStatusMessages.Any())
                            return;
                        var status = RotatingStatusMessages[state.Index++].Status;
                        if (string.IsNullOrWhiteSpace(status))
                            return;
                        PlayingPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value()));
                        var shards = NadekoBot.Client.Shards;
                        for (int i = 0; i < shards.Count; i++)
                        {
                            var curShard = shards.ElementAt(i);
                            ShardSpecificPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value(curShard)));
                            try { await shards.ElementAt(i).SetGameAsync(status).ConfigureAwait(false); }
                            catch (Exception ex)
                            {
                                _log.Warn(ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Rotating playing status errored.\n" + ex);
                    }
                }, new TimerState(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }

            public static Dictionary<string, Func<string>> PlayingPlaceholders { get; } =
                new Dictionary<string, Func<string>> {
                    { "%servers%", () => NadekoBot.Client.GetGuildCount().ToString()},
                    { "%users%", () => NadekoBot.Client.GetGuilds().Sum(s => s.Users.Count).ToString()},
                    { "%playing%", () => {
                            var cnt = Music.Music.MusicPlayers.Count(kvp => kvp.Value.CurrentSong != null);
                            if (cnt != 1) return cnt.ToString();
                            try {
                                var mp = Music.Music.MusicPlayers.FirstOrDefault();
                                return mp.Value.CurrentSong.SongInfo.Title;
                            }
                            catch {
                                return "No songs";
                            }
                        }
                    },
                    { "%queued%", () => Music.Music.MusicPlayers.Sum(kvp => kvp.Value.Playlist.Count).ToString()},
                    { "%time%", () => DateTime.Now.ToString("HH:mm " + TimeZoneInfo.Local.StandardName.GetInitials()) },
                    { "%shardcount%", () => NadekoBot.Client.Shards.Count.ToString() },
                };

            public static Dictionary<string, Func<DiscordSocketClient, string>> ShardSpecificPlaceholders { get; } =
                new Dictionary<string, Func<DiscordSocketClient, string>> {
                    { "%shardid%", (client) => client.ShardId.ToString()},
                    { "%shardguilds%", (client) => client.Guilds.Count.ToString()},
                };

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RotatePlaying()
            {
                lock (_locker)
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var config = uow.BotConfig.GetOrCreate();

                        RotatingStatuses = config.RotatingStatuses = !config.RotatingStatuses;
                        uow.Complete();
                    }
                }
                if (RotatingStatuses)
                    await ReplyConfirmLocalized("ropl_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("ropl_disabled").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task AddPlaying([Remainder] string status)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    var toAdd = new PlayingStatus { Status = status };
                    config.RotatingStatusMessages.Add(toAdd);
                    RotatingStatusMessages.Add(toAdd);
                    await uow.CompleteAsync();
                }

                await ReplyConfirmLocalized("ropl_added").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                if (!RotatingStatusMessages.Any())
                    await ReplyErrorLocalized("ropl_not_set").ConfigureAwait(false);
                else
                {
                    var i = 1;
                    await ReplyConfirmLocalized("ropl_list",
                            string.Join("\n\t", RotatingStatusMessages.Select(rs => $"`{i++}.` {rs.Status}")))
                        .ConfigureAwait(false);
                }

            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                index -= 1;

                string msg;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    if (index >= config.RotatingStatusMessages.Count)
                        return;
                    msg = config.RotatingStatusMessages[index].Status;
                    config.RotatingStatusMessages.RemoveAt(index);
                    RotatingStatusMessages.RemoveAt(index);
                    await uow.CompleteAsync();
                }
                await ReplyConfirmLocalized("reprm", msg).ConfigureAwait(false);
            }
        }
    }
}