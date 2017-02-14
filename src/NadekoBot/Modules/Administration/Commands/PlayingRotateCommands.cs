using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            private static Logger _log { get; }
            public static List<PlayingStatus> RotatingStatusMessages { get; }
            public static bool RotatingStatuses { get; private set; } = false;
            private static Timer _t { get; }

            private class TimerState
            {
                public int Index { get; set; } = 0;
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
                        else
                        {
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
                                ShardSpecificPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value(shards.ElementAt(i))));
                                try { await shards.ElementAt(i).SetGameAsync(status).ConfigureAwait(false); }
                                catch (Exception ex)
                                {
                                    _log.Warn(ex);
                                }
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
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    RotatingStatuses = config.RotatingStatuses = !config.RotatingStatuses;
                    await uow.CompleteAsync();
                }
                if (RotatingStatuses)
                    await Context.Channel.SendConfirmAsync("🆗 **Rotating playing status enabled.**").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ **Rotating playing status disabled.**").ConfigureAwait(false);
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

                await Context.Channel.SendConfirmAsync("✅ **Added.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                if (!RotatingStatusMessages.Any())
                    await Context.Channel.SendErrorAsync("❎ **No rotating playing statuses set.**");
                else
                {
                    var i = 1;
                    await Context.Channel.SendConfirmAsync($"ℹ️ {Context.User.Mention} `Here is a list of rotating statuses:`\n\n\t" + string.Join("\n\t", RotatingStatusMessages.Select(rs => $"`{i++}.` {rs.Status}")));
                }

            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                index -= 1;

                string msg = "";
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
                await Context.Channel.SendConfirmAsync($"🗑 **Removed the the playing message:** {msg}").ConfigureAwait(false);
            }
        }
    }
}