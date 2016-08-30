using NadekoBot.Services.Database.Repositories;
using NadekoBot.Services.Database.Repositories.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database
{
    public class UnitOfWork : IUnitOfWork
    {
        private NadekoContext _context;

        private IQuoteRepository _quotes;
        public IQuoteRepository Quotes => _quotes ?? (_quotes = new QuoteRepository(_context));

        private IGuildConfigRepository _guildConfigs;
        public IGuildConfigRepository GuildConfigs => _guildConfigs ?? (_guildConfigs = new GuildConfigRepository(_context));

        private IDonatorsRepository _donators;
        public IDonatorsRepository Donators => _donators ?? (_donators = new DonatorsRepository(_context));

        private IClashOfClansRepository _clashOfClans;
        public IClashOfClansRepository ClashOfClans => _clashOfClans ?? (_clashOfClans = new ClashOfClansRepository(_context));

        private IReminderRepository _reminders;
        public IReminderRepository Reminders => _reminders ?? (_reminders = new ReminderRepository(_context));

        private ISelfAssignedRolesRepository _selfAssignedRoles;
        public ISelfAssignedRolesRepository SelfAssignedRoles => _selfAssignedRoles ?? (_selfAssignedRoles = new SelfAssignedRolesRepository(_context));

        private IBotConfigRepository _botConfig;
        public IBotConfigRepository BotConfig => _botConfig ?? (_botConfig = new BotConfigRepository(_context));

        private IRepeaterRepository _repeaters;
        public IRepeaterRepository Repeaters => _repeaters ?? (_repeaters = new RepeaterRepository(_context));

        private ICurrencyRepository _currency;
        public ICurrencyRepository Currency => _currency ?? (_currency = new CurrencyRepository(_context));

        public UnitOfWork(NadekoContext context)
        {
            _context = context;
        }

        public int Complete() =>
            _context.SaveChanges();

        public Task<int> CompleteAsync() => 
            _context.SaveChangesAsync();

        private bool disposed = false;

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
                if (disposing)
                    _context.Dispose();
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
