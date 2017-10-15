using System;
using System.Linq;
using System.Threading;
using Discord.WebSocket;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;
using NadekoBot.Modules.Music.Services;

namespace NadekoBot.Modules.Administration.Services
{
    public class PlayingRotateService : INService
    {
        private readonly Timer _t;
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;
        private readonly IDataCache _cache;
        private readonly Replacer _rep;
        private readonly DbService _db;
        private readonly IBotConfigProvider _bcp;

        public BotConfig BotConfig => _bcp.BotConfig;

        private class TimerState
        {
            public int Index { get; set; }
        }

        public PlayingRotateService(DiscordSocketClient client, IBotConfigProvider bcp,
            DbService db, IDataCache cache, NadekoBot bot, MusicService music)
        {
            _client = client;
            _bcp = bcp;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _cache = cache;

            if (client.ShardId == 0)
            {

                _rep = new ReplacementBuilder()
                    .WithClient(client)
                    .WithStats(client)
                    .WithMusic(music)
                    .Build();

                _t = new Timer(async (objState) =>
                {
                    try
                    {
                        bcp.Reload();

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

                        try
                        {
                            await bot.SetGameAsync(status).ConfigureAwait(false);
                        }
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
}
