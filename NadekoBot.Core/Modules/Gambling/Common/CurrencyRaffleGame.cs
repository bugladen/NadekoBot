using Discord;
using NadekoBot.Common;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Gambling.Common
{
    public class CurrencyRaffleGame
    {
        private readonly HashSet<IUser> _users = new HashSet<IUser>();
        public IEnumerable<IUser> Users => _users;
        private readonly int _amount;

        public CurrencyRaffleGame(int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException();

            _amount = amount;
        }

        public bool AddUser(IUser usr)
        {
            if (!_users.Add(usr))
                return false;
            
            return true;
        }
    }
}
