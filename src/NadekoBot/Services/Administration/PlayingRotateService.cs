using Discord.WebSocket;
using NadekoBot.DataStructures.Replacements;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Music;
using NLog;
using System;
using System.Linq;
using System.Threading;

namespace NadekoBot.Services.Administration
{
    public class PlayingRotateService
    {
        private readonly Timer _t;
        private readonly DiscordSocketClient _client;
        private readonly MusicService _music;
        private readonly Logger _log;
        private readonly Replacer _rep;
        private readonly DbService _db;
        public BotConfig BotConfig { get; private set; } //todo load whole botconifg, not just for this service when you have the time

        private class TimerState
        {
            public int Index { get; set; }
        }

        public PlayingRotateService(DiscordSocketClient client, BotConfig bc, MusicService music, DbService db)
        {
            _client = client;
            BotConfig = bc;
            _music = music;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _rep = new ReplacementBuilder()
                .WithClient(client)
                .WithStats(client)
                //.WithMusic(music)
                .Build();

            _t = new Timer(async (objState) =>
            {
                try
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        BotConfig = uow.BotConfig.GetOrCreate();
                    }
                    var state = (TimerState)objState;
                    if (!BotConfig.RotatingStatuses)
                        return;
                    if (state.Index >= BotConfig.RotatingStatusMessages.Count)
                        state.Index = 0;

                    if (!BotConfig.RotatingStatusMessages.Any())
                        return;
                    var status = BotConfig.RotatingStatusMessages[state.Index++].Status;
                    if (string.IsNullOrWhiteSpace(status))
                        return;

                    status = _rep.Replace(status);

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
    }
}
