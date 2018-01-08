using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;
using System.Collections.Concurrent;
using System.Linq;

namespace NadekoBot.Modules.Gambling.Services
{
    public class GamblingService : INService
    {
        private readonly DbService _db;
        private readonly CurrencyService _cs;
        private readonly Logger _log;

        public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new ConcurrentDictionary<(ulong, ulong), RollDuelGame>();

        public GamblingService(DbService db, CurrencyService cs)
        {
            _db = db;
            _cs = cs;
            _log = LogManager.GetCurrentClassLogger();

            using (var uow = _db.UnitOfWork)
            {
                //refund all of the currency users had at stake in gambling games
                //at the time bot was restarted

                var stakes = uow._context.Set<Stake>()
                    .ToArray();

                foreach (var s in stakes)
                {
                    _cs.AddAsync(s.UserId, "Stake-" + s.Source, s.Amount, uow, gamble: true)
                        .GetAwaiter()
                        .GetResult();
                }

                uow._context.Set<Stake>().RemoveRange(stakes);
                uow.Complete();
                _log.Info("Refunded {0} users' stakes.", stakes.Length);
            }
        }
    }
}
