using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Services;
using NadekoBot.Modules.Gambling.Common.Connect4;
using NLog;
using System.Collections.Concurrent;

namespace NadekoBot.Modules.Gambling.Services
{
    public class GamblingService : INService
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly Logger _log;

        public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new ConcurrentDictionary<(ulong, ulong), RollDuelGame>();
        public ConcurrentDictionary<ulong, Connect4Game> Connect4Games { get; } = new ConcurrentDictionary<ulong, Connect4Game>();

        public GamblingService(DbService db, ICurrencyService cs)
        {
            _db = db;
            _cs = cs;
            _log = LogManager.GetCurrentClassLogger();

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
