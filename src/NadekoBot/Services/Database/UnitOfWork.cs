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

        private IConfigRepository _configs;
        public IConfigRepository Configs => _configs ?? (_configs = new ConfigRepository(_context));

        private IDonatorsRepository _donators;
        public IDonatorsRepository Donators => _donators ?? (_donators = new DonatorsRepository(_context));

        public UnitOfWork(NadekoContext context)
        {
            
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
