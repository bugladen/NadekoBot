using NadekoBot.Services.Database.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        IQuoteRepository Quotes { get; }
        int Complete();
        Task<int> CompleteAsync();
    }
}
