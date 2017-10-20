using System.Threading.Tasks;
using NadekoBot.Core.Services;
using NadekoBot.Core.Modules.Gambling.Common;
using System.Threading;
using System.Linq;
using NadekoBot.Common;
using System.Collections.Generic;
using Discord;
using System;

namespace NadekoBot.Core.Modules.Gambling.Services
{
    public class CurrencyRaffleService : INService
    {
        public enum JoinErrorType {
            NotEnoughCurrency,
            AlreadyJoined
        }
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private readonly DbService _db;
        private readonly CurrencyService _cs;

        public Dictionary<ulong, CurrencyRaffleGame> Games { get; } = new Dictionary<ulong, CurrencyRaffleGame>();

        public CurrencyRaffleService(DbService db, CurrencyService cs)
        {
            _db = db;
            _cs = cs;
        }

        public async Task<(CurrencyRaffleGame, JoinErrorType?)> JoinOrCreateGame(ulong channelId, IUser user, int amount, Func<IUser, int, Task> onEnded)
        {
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                var newGame = false;
                if (!Games.TryGetValue(channelId, out var crg))
                {
                    newGame = true;
                    crg = new CurrencyRaffleGame(amount);
                }
                using (var uow = _db.UnitOfWork)
                {
                    //remove money, and stop the game if this 
                    // user created it and doesn't have the money
                    if (!await _cs.RemoveAsync(user.Id, "Currency Raffle Join", amount, uow).ConfigureAwait(false))
                    {
                        if(newGame)
                            Games.Remove(channelId);
                        return (null, JoinErrorType.NotEnoughCurrency);
                    }

                    if (!crg.AddUser(user))
                    {
                        await _cs.AddAsync(user.Id, "Curency Raffle Refund", amount, uow).ConfigureAwait(false);
                        return (null, JoinErrorType.AlreadyJoined);
                    }
                }
                if (newGame)
                {
                    var _t = new Timer(async state =>
                    {
                        await _locker.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            var users = crg.Users.ToArray();
                            var rng = new NadekoRandom();
                            var usr = users[rng.Next(0, users.Length)];

                            using (var uow = _db.UnitOfWork)
                            {
                                await _cs.AddAsync(usr.Id, "Currency Raffle Win",
                                    amount * users.Length, uow);
                            }
                            Games.Remove(channelId, out _);
                            var oe = onEnded(usr, users.Length * amount);
                        }
                        catch { }
                        finally { _locker.Release(); }

                    }, null, 30000, Timeout.Infinite);
                }
                return (crg, null);
            }
            finally
            {
                _locker.Release();
            }
        }
    }
}
