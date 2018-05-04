using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Services;
using NadekoBot.Modules.Gambling.Common.Connect4;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NadekoBot.Modules.Gambling.Services
{
    public class GamblingService : INService
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly IBotConfigProvider _bc;
        private readonly NadekoBot _bot;
        private readonly Logger _log;

        public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new ConcurrentDictionary<(ulong, ulong), RollDuelGame>();
        public ConcurrentDictionary<ulong, Connect4Game> Connect4Games { get; } = new ConcurrentDictionary<ulong, Connect4Game>();

        private readonly Timer _decayTimer;

        public GamblingService(DbService db, NadekoBot bot, ICurrencyService cs, IBotConfigProvider bc)
        {
            _db = db;
            _cs = cs;
            _bc = bc;
            _bot = bot;
            _log = LogManager.GetCurrentClassLogger();

            if (_bot.Client.ShardId == 0)
            {
                _decayTimer = new Timer(_ =>
                {
                    var decay = _bc.BotConfig.DailyCurrencyDecay;
                    if (decay <= 0)
                        return;

                    using (var uow = _db.UnitOfWork)
                    {
                        var botc = uow.BotConfig.GetOrCreate();
                        //once every 24 hours
                        if (DateTime.UtcNow - _bc.BotConfig.LastCurrencyDecay < TimeSpan.FromHours(24))
                            return;
                        uow.DiscordUsers.CurrencyDecay(decay, _bot.Client.CurrentUser.Id);
                        _cs.AddAsync(_bot.Client.CurrentUser.Id,
                            "Currency Decay",
                            uow.DiscordUsers.GetCurrencyDecayAmount(decay));
                        botc.LastCurrencyDecay = DateTime.UtcNow;
                        uow.Complete();
                    }
                    _bc.Reload();
                }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            }

            //using (var uow = _db.UnitOfWork)
            //{
            //    //refund all of the currency users had at stake in gambling games
            //    //at the time bot was restarted

            //    var stakes = uow._context.Set<Stake>()
            //        .ToArray();

            //    var userIds = stakes.Select(x => x.UserId).ToArray();
            //    var reasons = stakes.Select(x => "Stake-" + x.Source).ToArray();
            //    var amounts = stakes.Select(x => x.Amount).ToArray();

            //    _cs.AddBulkAsync(userIds, reasons, amounts, gamble: true).ConfigureAwait(false);

            //    foreach (var s in stakes)
            //    {
            //        _cs.AddAsync(s.UserId, "Stake-" + s.Source, s.Amount, gamble: true)
            //            .GetAwaiter()
            //            .GetResult();
            //    }

            //    uow._context.Set<Stake>().RemoveRange(stakes);
            //    uow.Complete();
            //    _log.Info("Refunded {0} users' stakes.", stakes.Length);
            //}
        }
    }
}
