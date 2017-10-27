using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IPollsRepository : IRepository<Poll>
    {
        IEnumerable<Poll> GetAllPolls();
        void RemovePoll(int id);
    }
}
