using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Music;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NadekoBot.Services.Administration
{
    //todo 99 - Could make a placeholder service, which can work for any module
    //and have replacements which are dependent on the types provided in the constructor
    public class PlayingRotateService
    {
        public List<PlayingStatus> RotatingStatusMessages { get; }
        public volatile bool RotatingStatuses;
        private readonly Timer _t;
        private readonly DiscordSocketClient _client;
        private readonly BotConfig _bc;
        private readonly MusicService _music;
        private readonly Logger _log;

        private class TimerState
        {
            public int Index { get; set; }
        }

        public PlayingRotateService(DiscordSocketClient client, BotConfig bc, MusicService music)
        {
            _client = client;
            _bc = bc;
            _music = music;
            _log = LogManager.GetCurrentClassLogger();

            RotatingStatusMessages = _bc.RotatingStatusMessages;
            RotatingStatuses = _bc.RotatingStatuses;
            
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
                    PlayingPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value(_client, _music)));
                    ShardSpecificPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value(client)));
                    try { await client.SetGameAsync(status).ConfigureAwait(false); }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Rotating playing status errored.\n" + ex);
                }
            }, new TimerState(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public Dictionary<string, Func<DiscordSocketClient, MusicService, string>> PlayingPlaceholders { get; } =
            new Dictionary<string, Func<DiscordSocketClient, MusicService, string>> {
                    { "%servers%", (c, ms) => c.Guilds.Count.ToString()},
                    { "%users%", (c, ms) => c.Guilds.Sum(s => s.Users.Count).ToString()},
                    { "%playing%", (c, ms) => {
                            var cnt = ms.MusicPlayers.Count(kvp => kvp.Value.CurrentSong != null);
                            if (cnt != 1) return cnt.ToString();
                            try {
                                var mp = ms.MusicPlayers.FirstOrDefault();
                                return mp.Value.CurrentSong.SongInfo.Title;
                            }
                            catch {
                                return "No songs";
                            }
                        }
                    },
                    { "%queued%", (c, ms) => ms.MusicPlayers.Sum(kvp => kvp.Value.Playlist.Count).ToString()},
                    { "%time%", (c, ms) => DateTime.Now.ToString("HH:mm " + TimeZoneInfo.Local.StandardName.GetInitials()) },
            };

        public Dictionary<string, Func<DiscordSocketClient, string>> ShardSpecificPlaceholders { get; } =
            new Dictionary<string, Func<DiscordSocketClient, string>> {
                    { "%shardid%", (client) => client.ShardId.ToString()},
                    { "%shardguilds%", (client) => client.Guilds.Count.ToString()},
            };
    }
}
