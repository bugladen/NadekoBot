using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Gambling.Common
{
    public class CurrencyRaffleGame
    {
        private readonly HashSet<(string, ulong)> _users = new HashSet<(string, ulong)>();
        private readonly int _amount;
        private readonly CurrencyService _cs;
        private readonly DbService _db;
        private bool running;

        public CurrencyRaffleGame(int amount, CurrencyService cs, DbService db)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException();

            _amount = amount;
            _cs = cs;
            _db = db;
        }

        public async Task<bool> AddUser(string username, ulong userId)
        {
            
        }

        public void ForceStop()
        {
            lock (_locker)
            {
                running = false;
            }
        }
    }
}
